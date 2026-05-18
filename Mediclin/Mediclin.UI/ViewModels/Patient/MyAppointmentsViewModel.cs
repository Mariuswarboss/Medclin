using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Mediclin.Data.Context;
using Mediclin.Data.Models;
using Mediclin.UI.Models;
using Mediclin.UI.Services;

using System.Linq;
using System.Windows;

namespace Mediclin.UI.ViewModels.Patient;

public class MyAppointmentsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private int _pacientId;
    private string _pacientNume = string.Empty;
    private string _activeFilter = "Toate";
    private readonly List<Medic> _allDoctors = new();
    private Specialitate? _selectedSpecialitate;
    private Medic? _selectedMedic;
    private DateTime? _selectedDate = DateTime.Today;
    private DateTime? _selectedSlot;
    private string _motivVizita = string.Empty;
    private string _slotsMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private string _bookingError = string.Empty;
    private string _bookingSuccess = string.Empty;
    private bool _isSlotsLoading;
    private bool _isBookingLoading;
    private bool _isLoading;
    private bool _isEmpty;

    public MyAppointmentsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        AllAppointments = new ObservableCollection<ProgramareDisplay>();
        FilteredAppointments = new ObservableCollection<ProgramareDisplay>();
        Specialitati = new ObservableCollection<Specialitate>();
        MediciFiltrati = new ObservableCollection<Medic>();
        AvailableSlots = new ObservableCollection<DateTime>();

        ProgramareNouaCommand = new RelayCommand(_ => { }); // booking is inline
        SetFiltruCommand = new RelayCommand(p =>
        {
            var f = p?.ToString() ?? "Toate";
            ApplyFilter(f);
        });
        SelectSpecialitateCommand = new RelayCommand(p => SelectedSpecialitate = p as Specialitate);
        SelectSlotCommand = new RelayCommand(p =>
        {
            if (p is DateTime dt)
            {
                SelectedSlot = dt;
            }
        });
        ConfirmBookingCommand = new AsyncRelayCommand(async _ => await ExecuteConfirmBookingAsync(), _ => !IsBookingLoading);
        CancelAppointmentCommand = new AsyncRelayCommand(async p =>
        {
            if (p is ProgramareDisplay row)
            {
                await CancelAsync(row.Id);
            }
        });

        _ = LoadInitialAsync();
    }

    public ObservableCollection<ProgramareDisplay> AllAppointments { get; private set; }
    public ObservableCollection<ProgramareDisplay> FilteredAppointments { get; private set; }
    public ObservableCollection<Specialitate> Specialitati { get; }
    public ObservableCollection<Medic> MediciFiltrati { get; }
    
    private ObservableCollection<DateTime> _availableSlots = new();
    public ObservableCollection<DateTime> AvailableSlots
    {
        get => _availableSlots;
        set => SetProperty(ref _availableSlots, value);
    }

    public string ActiveFilter
    {
        get => _activeFilter;
        set => SetProperty(ref _activeFilter, value);
    }

    public Specialitate? SelectedSpecialitate
    {
        get => _selectedSpecialitate;
        set
        {
            if (SetProperty(ref _selectedSpecialitate, value))
            {
                ApplyDoctorFilter();
            }
        }
    }

    public Medic? SelectedMedic
    {
        get => _selectedMedic;
        set
        {
            if (SetProperty(ref _selectedMedic, value))
            {
                AvailableSlots = new ObservableCollection<DateTime>();
                SelectedSlot   = null;
                SlotsMessage   = "";
                if (value != null && SelectedDate.HasValue)
                    _ = Task.Run(async () =>
                        await Application.Current.Dispatcher.InvokeAsync(
                            LoadSlotsAsync));
            }
        }
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                AvailableSlots = new ObservableCollection<DateTime>();
                SelectedSlot   = null;
                SlotsMessage   = "";
                if (SelectedMedic != null && value.HasValue)
                    _ = Task.Run(async () =>
                        await Application.Current.Dispatcher.InvokeAsync(
                            LoadSlotsAsync));
            }
        }
    }

    public DateTime? SelectedSlot { get => _selectedSlot; set => SetProperty(ref _selectedSlot, value); }
    public string SelectedTip { get; set; } = "Control";

    public string MotivVizita
    {
        get => _motivVizita;
        set => SetProperty(ref _motivVizita, value);
    }

    public string SlotsMessage
    {
        get => _slotsMessage;
        set => SetProperty(ref _slotsMessage, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public string BookingError
    {
        get => _bookingError;
        set => SetProperty(ref _bookingError, value);
    }

    public string BookingSuccess
    {
        get => _bookingSuccess;
        set => SetProperty(ref _bookingSuccess, value);
    }

    public bool IsSlotsLoading
    {
        get => _isSlotsLoading;
        set => SetProperty(ref _isSlotsLoading, value);
    }

    public bool IsBookingLoading
    {
        get => _isBookingLoading;
        set
        {
            if (SetProperty(ref _isBookingLoading, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }

    public ICommand ProgramareNouaCommand { get; }
    public ICommand SetFiltruCommand { get; }
    public ICommand SelectSpecialitateCommand { get; }
    public ICommand SelectSlotCommand { get; }
    public ICommand ConfirmBookingCommand { get; }
    public ICommand CancelAppointmentCommand { get; }

    private async Task LoadInitialAsync()
    {
        try
        {
            var pacient = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (pacient is null)
            {
                ErrorMessage = "Profilul de pacient nu a fost găsit. Contactați recepția.";
                return;
            }
            _pacientId = pacient.Id;
            _pacientNume = _utilizator.NumeComplet;

            Specialitati.Clear();
            foreach (var s in await _app.Specialitati.GetAllAsync())
            {
                Specialitati.Add(s);
            }

            _allDoctors.Clear();
            _allDoctors.AddRange(await _app.Medici.GetAllAsync());
            SelectedSpecialitate ??= Specialitati.FirstOrDefault();
            ApplyDoctorFilter();
            SelectedMedic ??= MediciFiltrati.FirstOrDefault();
            
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nu s-au putut încărca datele inițiale: {ex.Message}";
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            AllAppointments = new ObservableCollection<ProgramareDisplay>();

            var db = new DatabaseContext();
            var rows = await db.QueryAsync(@"
                SELECT
                    pr.id,
                    pr.data_ora,
                    pr.durata_min,
                    pr.tip,
                    pr.status,
                    pr.motiv_vizita,
                    CONCAT(um.prenume, ' ', um.nume) AS medic_nume,
                    m.titlu                          AS medic_titlu,
                    s.nume                           AS specialitate_nume
                FROM programari pr
                JOIN medici      m  ON m.id  = pr.medic_id
                JOIN utilizatori um ON um.id = m.utilizator_id
                JOIN specialitati s ON s.id  = m.specialitate_id
                WHERE pr.pacient_id = @pid
                ORDER BY pr.data_ora DESC",
                new Dictionary<string, object> { ["@pid"] = _pacientId });

            foreach (var r in rows ?? new List<Dictionary<string, object>>())
            {
                AllAppointments.Add(new ProgramareDisplay
                {
                    Id               = Convert.ToInt32(r["id"]),
                    DataOra          = (DateTime)r["data_ora"],
                    DurataMin        = Convert.ToInt32(r["durata_min"]),
                    Tip              = r["tip"]?.ToString() ?? "",
                    Status           = r["status"]?.ToString() ?? "",
                    MotivVizita      = r["motiv_vizita"]?.ToString() ?? "",
                    MedicNume        = r["medic_nume"]?.ToString() ?? "",
                    MedicTitlu       = r["medic_titlu"]?.ToString() ?? "",
                    SpecialitateNume = r["specialitate_nume"]?.ToString() ?? ""
                });
            }

            ApplyFilter(ActiveFilter);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter(string filter)
    {
        ActiveFilter = filter;
        var now = DateTime.Now;

        var filtered = filter switch
        {
            "Viitoare"   => AllAppointments.Where(p =>
                                p.DataOra > now &&
                                p.Status is not ("Anulata" or "Neprezentata"
                                                 or "Finalizata")),
            "Finalizate" => AllAppointments.Where(p =>
                                p.Status == "Finalizata"),
            "Anulate"    => AllAppointments.Where(p =>
                                p.Status is "Anulata" or "Neprezentata"),
            _            => AllAppointments.AsEnumerable()
        };

        var isAscending = filter == "Toate" || filter == "Viitoare";
        var ordered = isAscending ? filtered.OrderBy(p => p.DataOra) : filtered.OrderByDescending(p => p.DataOra);

        FilteredAppointments = new ObservableCollection<ProgramareDisplay>(ordered);

        IsEmpty = FilteredAppointments.Count == 0;
        OnPropertyChanged(nameof(FilteredAppointments));
    }

    private void ApplyDoctorFilter()
    {
        MediciFiltrati.Clear();
        var doctors = SelectedSpecialitate is null
            ? _allDoctors
            : _allDoctors.Where(m => m.SpecialitateId == SelectedSpecialitate.Id);

        foreach (var medic in doctors.OrderBy(m => m.NumeComplet))
        {
            MediciFiltrati.Add(medic);
        }

        if (SelectedMedic is not null && !MediciFiltrati.Contains(SelectedMedic))
        {
            SelectedMedic = MediciFiltrati.FirstOrDefault();
        }
        else if (SelectedMedic is null && MediciFiltrati.Count > 0)
        {
            SelectedMedic = MediciFiltrati[0];
        }
    }

    private async Task LoadSlotsAsync()
    {
        try
        {
            IsSlotsLoading = true;
            AvailableSlots = new ObservableCollection<DateTime>();
            SlotsMessage   = "";

            if (SelectedMedic == null || !SelectedDate.HasValue)
            {
                SlotsMessage = "Selectați medicul și data.";
                return;
            }

            // Correct Romanian day names matching DB ENUM exactly
            string ziRomana = SelectedDate.Value.DayOfWeek switch
            {
                DayOfWeek.Monday    => "Luni",
                DayOfWeek.Tuesday   => "Marti",
                DayOfWeek.Wednesday => "Miercuri",
                DayOfWeek.Thursday  => "Joi",
                DayOfWeek.Friday    => "Vineri",
                DayOfWeek.Saturday  => "Sambata",
                DayOfWeek.Sunday    => "Duminica",
                _                   => ""
            };

            var db = new DatabaseContext();

            // Step 1: Get doctor schedule for this day
            var programRows = await db.QueryAsync(@"
                SELECT ora_start, ora_sfarsit, activ
                FROM   program_medici
                WHERE  medic_id      = @mid
                AND    zi_saptamana  = @zi
                AND    activ         = 1
                LIMIT  1",
                new Dictionary<string, object>
                {
                    ["@mid"] = SelectedMedic.Id,
                    ["@zi"]  = ziRomana
                });

            if (programRows == null || programRows.Count == 0)
            {
                SlotsMessage = $"Medicul nu lucrează {ziRomana}.";
                return;
            }

            // Step 2: Parse schedule times safely
            TimeSpan oraStart, oraSfarsit;
            try
            {
                var startRaw = programRows[0]["ora_start"];
                var endRaw   = programRows[0]["ora_sfarsit"];

                // MySql.Data returns TIME as TimeSpan directly
                oraStart   = startRaw is TimeSpan ts1 ? ts1
                             : TimeSpan.Parse(startRaw.ToString()!);
                oraSfarsit = endRaw   is TimeSpan ts2 ? ts2
                             : TimeSpan.Parse(endRaw.ToString()!);
            }
            catch
            {
                SlotsMessage = "Eroare la citirea programului medicului.";
                return;
            }

            int durata = SelectedMedic.DurataConsultatie > 0
                             ? SelectedMedic.DurataConsultatie : 30;

            // Step 3: Get already booked slots for this doctor+date
            var bookedRows = await db.QueryAsync(@"
                SELECT data_ora, durata_min
                FROM   programari
                WHERE  medic_id = @mid
                AND    DATE(data_ora) = @data
                AND    status NOT IN ('Anulata','Neprezentata')",
                new Dictionary<string, object>
                {
                    ["@mid"]  = SelectedMedic.Id,
                    ["@data"] = SelectedDate.Value.ToString("yyyy-MM-dd")
                });

            var taken = (bookedRows ?? new List<Dictionary<string, object>>())
                .Select(b =>
                {
                    var start = b["data_ora"] is DateTime dt ? dt
                                : DateTime.Parse(b["data_ora"].ToString()!);
                    var dur   = Convert.ToInt32(b["durata_min"]);
                    return (Start: start, End: start.AddMinutes(dur));
                }).ToList();

            // Step 4: Generate slots
            var slots   = new List<DateTime>();
            var current = SelectedDate.Value.Date + oraStart;
            var endTime = SelectedDate.Value.Date + oraSfarsit;
            var now     = DateTime.Now;

            while (current.AddMinutes(durata) <= endTime)
            {
                // Skip past times (only if today)
                if (SelectedDate.Value.Date == DateTime.Today &&
                    current <= now)
                {
                    current = current.AddMinutes(durata);
                    continue;
                }

                bool isTaken = taken.Any(t =>
                    current <  t.End &&
                    current.AddMinutes(durata) > t.Start);

                if (!isTaken)
                    slots.Add(current);

                current = current.AddMinutes(durata);
            }

            if (slots.Count == 0)
                SlotsMessage = "Nu există ore disponibile pentru această zi.";
            else
                AvailableSlots = new ObservableCollection<DateTime>(slots);
        }
        catch (Exception ex)
        {
            SlotsMessage = $"Eroare: {ex.Message}";
        }
        finally
        {
            IsSlotsLoading = false;
        }
    }

    private async Task ExecuteConfirmBookingAsync()
    {
        try
        {
            // Validation
            if (_selectedMedic == null)
            { BookingError = "Selectați un medic."; return; }
            if (!SelectedDate.HasValue)
            { BookingError = "Selectați o dată."; return; }
            if (!SelectedSlot.HasValue)
            { BookingError = "Selectați o oră disponibilă."; return; }

            IsBookingLoading = true;
            BookingError = "";
            BookingSuccess = "";

            var db = new DatabaseContext();

            // Double-check slot is still available
            var conflict = await db.ScalarAsync(@"
                SELECT COUNT(*) FROM programari
                WHERE medic_id = @mid
                AND status NOT IN ('Anulata','Neprezentata')
                AND data_ora < @end
                AND DATE_ADD(data_ora, INTERVAL durata_min MINUTE) > @start",
                new Dictionary<string, object>
                {
                    ["@mid"]   = _selectedMedic.Id,
                    ["@start"] = SelectedSlot.Value,
                    ["@end"]   = SelectedSlot.Value.AddMinutes(
                                     _selectedMedic.DurataConsultatie)
                });

            if (Convert.ToInt32(conflict) > 0)
            {
                BookingError = "Această oră a fost rezervată între timp. " +
                               "Alegeți altă oră.";
                await LoadSlotsAsync(); // refresh slots
                return;
            }

            // INSERT programare
            var newId = await db.ExecuteInsertAsync(@"
                INSERT INTO programari
                    (pacient_id, medic_id, data_ora, durata_min,
                     tip, status, motiv_vizita, creat_la, actualizat_la)
                VALUES
                    (@pid, @mid, @data, @dur,
                     @tip, 'Confirmata', @motiv, NOW(), NOW())",
                new Dictionary<string, object>
                {
                    ["@pid"]   = _pacientId,
                    ["@mid"]   = _selectedMedic.Id,
                    ["@data"]  = SelectedSlot.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["@dur"]   = _selectedMedic.DurataConsultatie,
                    ["@tip"]   = SelectedTip ?? "Control",
                    ["@motiv"] = MotivVizita ?? ""
                });

            int programareId = Convert.ToInt32(newId);

            // Notify the doctor
            await db.ExecuteAsync(@"
                INSERT INTO notificari
                    (utilizator_id, titlu, mesaj, tip, citita, creat_la)
                VALUES
                    (@uid,
                     'Programare nouă',
                     @msg,
                     'Programare', 0, NOW())",
                new Dictionary<string, object>
                {
                    ["@uid"] = _selectedMedic.UtilizatorId,
                    ["@msg"] = $"Pacientul {_pacientNume} a făcut o programare " +
                               $"pe {SelectedSlot.Value:dd MMM yyyy} " +
                               $"la ora {SelectedSlot.Value:HH:mm}."
                });

            // Notify the patient
            await db.ExecuteAsync(@"
                INSERT INTO notificari
                    (utilizator_id, titlu, mesaj, tip, citita, creat_la)
                VALUES
                    (@uid,
                     'Programare confirmată',
                     @msg,
                     'Programare', 0, NOW())",
                new Dictionary<string, object>
                {
                    ["@uid"] = _utilizator.Id,
                    ["@msg"] = $"Programarea ta la {_selectedMedic.NumeCompletCuTitlu} " +
                               $"pe {SelectedSlot.Value:dd MMM yyyy HH:mm} " +
                               $"a fost confirmată."
                });

            // Log to jurnal_activitate
            await db.ExecuteAsync(@"
                INSERT INTO jurnal_activitate
                    (utilizator_id, actiune, modul, severitate, creat_la)
                VALUES (@uid, 'CREATE_PROGRAMARE', 'Programari', 'Info', NOW())",
                new Dictionary<string, object> { ["@uid"] = _utilizator.Id });

            // Success
            BookingSuccess = $"✓ Programare confirmată pentru " +
                             $"{SelectedSlot.Value:dd MMM yyyy} la " +
                             $"{SelectedSlot.Value:HH:mm} cu " +
                             $"{_selectedMedic.NumeCompletCuTitlu}";

            // Reset form
            SelectedSlot   = null;
            MotivVizita    = "";
            AvailableSlots = new ObservableCollection<DateTime>();

            // Refresh the appointments list
            await LoadAsync();

            // Auto-hide success after 4 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(4000);
                App.Current.Dispatcher.Invoke(() => BookingSuccess = "");
            });
        }
        catch (Exception ex)
        {
            BookingError = $"Eroare la salvarea programării: {ex.Message}";
        }
        finally
        {
            IsBookingLoading = false;
        }
    }

    private async Task CancelAsync(int id)
    {
        try
        {
            await _app.ProgramariRepo.CancelAsync(id, "Anulată de pacient din aplicație.");
            await LoadAsync();
            ErrorMessage = "Programarea a fost anulată.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Anulare eșuată: {ex.Message}";
        }
    }
}


