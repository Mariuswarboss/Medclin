using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.Data.Context;
using Mediclin.Data.Models;
using Mediclin.UI.Models;
using Mediclin.UI.Services;
using System.Linq;

namespace Mediclin.UI.ViewModels.Doctor;

public class DashboardViewModel : BaseViewModel, IDisposable
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private readonly int _medicId;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _refreshTimer;
    private int _inAsteptareCount;
    private int _finalizateCount;
    private int _totalAziCount;
    private int _pacientiActivi;
    private int _activePatientsNow;
    private int _availableStaffCount;
    private int _totalStaffCount;
    private int _inRoomCount;
    private string _currentTimeText = DateTime.Now.ToString("HH:mm");
    private string _currentDateText = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("ro-RO"));
    private string _weatherText = "Meteo locală indisponibilă offline";
    private string _shiftSummary = "Se sincronizează";
    private string _clinicMessage = "Totul este stabil în clinică.";

    private ObservableCollection<ProgramareDisplay> _todayAppointments = new();
    private ObservableCollection<ProgramareDisplay> _waitingPatients = new();
    private ObservableCollection<DashboardTimelineItem> _medicalTimeline = new();

    public DashboardViewModel(ApplicationServices app, Utilizator utilizator, int medicId)
    {
        _app = app;
        _utilizator = utilizator;
        _medicId = medicId;
        UpdateStatusCommand = new AsyncRelayCommand(async p => await UpdatePrAsync(p));

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            CurrentTimeText = DateTime.Now.ToString("HH:mm");
            CurrentDateText = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("ro-RO"));
        };
        _clockTimer.Start();

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _refreshTimer.Tick += async (s, e) => await LoadAsync();
        _refreshTimer.Start();

        _ = LoadAsync();
    }

    public void Dispose()
    {
        _clockTimer?.Stop();
        _refreshTimer?.Stop();
    }

    public ObservableCollection<ProgramareDisplay> TodayAppointments
    {
        get => _todayAppointments;
        set => SetProperty(ref _todayAppointments, value);
    }

    public ObservableCollection<ProgramareDisplay> WaitingPatients
    {
        get => _waitingPatients;
        set => SetProperty(ref _waitingPatients, value);
    }

    public ObservableCollection<DashboardTimelineItem> MedicalTimeline
    {
        get => _medicalTimeline;
        set => SetProperty(ref _medicalTimeline, value);
    }

    public string WelcomeText => $"Bună ziua, Dr. {_utilizator.NumeComplet}!";
    public string TodayDateText => DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("ro-RO"));

    public int InAsteptareCount
    {
        get => _inAsteptareCount;
        set => SetProperty(ref _inAsteptareCount, value);
    }

    public int FinalizateCount
    {
        get => _finalizateCount;
        set => SetProperty(ref _finalizateCount, value);
    }

    public int TotalAziCount
    {
        get => _totalAziCount;
        set => SetProperty(ref _totalAziCount, value);
    }

    public int PacientiActivi
    {
        get => _pacientiActivi;
        set => SetProperty(ref _pacientiActivi, value);
    }

    public int ActivePatientsNow
    {
        get => _activePatientsNow;
        set => SetProperty(ref _activePatientsNow, value);
    }

    public int AvailableStaffCount
    {
        get => _availableStaffCount;
        set => SetProperty(ref _availableStaffCount, value);
    }

    public int TotalStaffCount
    {
        get => _totalStaffCount;
        set => SetProperty(ref _totalStaffCount, value);
    }

    public int InRoomCount
    {
        get => _inRoomCount;
        set => SetProperty(ref _inRoomCount, value);
    }

    public string CurrentTimeText
    {
        get => _currentTimeText;
        set => SetProperty(ref _currentTimeText, value);
    }

    public string CurrentDateText
    {
        get => _currentDateText;
        set => SetProperty(ref _currentDateText, value);
    }

    public string WeatherText
    {
        get => _weatherText;
        set => SetProperty(ref _weatherText, value);
    }

    public string ShiftSummary
    {
        get => _shiftSummary;
        set => SetProperty(ref _shiftSummary, value);
    }

    public string ClinicMessage
    {
        get => _clinicMessage;
        set => SetProperty(ref _clinicMessage, value);
    }

    public string StaffAvailabilityText => $"{AvailableStaffCount} / {TotalStaffCount} medici disponibili";
    public string LivePulseCaption => ActivePatientsNow == 0 ? "Flux liniștit" : "Flux activ în clinică";

    public ICommand UpdateStatusCommand { get; }

    public async Task LoadAsync()
    {
        if (_medicId <= 0) return;

        try
        {
            var db = new DatabaseContext();

            // Today's appointments for this doctor
            var rows = await db.QueryAsync(@"
                SELECT
                    pr.id,
                    pr.data_ora,
                    pr.durata_min,
                    pr.tip,
                    pr.status,
                    pr.motiv_vizita,
                    CONCAT(up.prenume, ' ', up.nume) AS pacient_nume,
                    up.prenume                        AS pacient_prenume,
                    up.nume                           AS pacient_nume_familie
                FROM programari pr
                JOIN pacienti    pa ON pa.id  = pr.pacient_id
                JOIN utilizatori up ON up.id  = pa.utilizator_id
                WHERE pr.medic_id = @mid
                AND   DATE(pr.data_ora) = CURDATE()
                ORDER BY pr.data_ora ASC",
                new Dictionary<string, object> { ["@mid"] = _medicId });

            var list = new ObservableCollection<ProgramareDisplay>();
            foreach (var r in rows ?? new List<Dictionary<string, object>>())
            {
                list.Add(new ProgramareDisplay
                {
                    Id          = Convert.ToInt32(r["id"]),
                    DataOra     = (DateTime)r["data_ora"],
                    Status      = r["status"]?.ToString() ?? "",
                    Tip         = r["tip"]?.ToString() ?? "",
                    MotivVizita = r["motiv_vizita"]?.ToString() ?? "",
                    MedicNume   = r["pacient_nume"]?.ToString() ?? "",
                    Initiale    = GetInitiale(
                                    r["pacient_prenume"]?.ToString() ?? "",
                                    r["pacient_nume_familie"]?.ToString() ?? "")
                });
            }
            TodayAppointments = list;

            // Counts
            InAsteptareCount = list.Count(p =>
                p.Status is "In_asteptare" or "Programata" or "Confirmata");
            FinalizateCount  = list.Count(p => p.Status == "Finalizata");
            TotalAziCount    = list.Count;
            InRoomCount      = list.Count(p => string.Equals(p.Status, "In_cabinet", StringComparison.OrdinalIgnoreCase));
            ActivePatientsNow = list.Count(p => p.Status is "In_asteptare" or "Programata" or "Confirmata" or "In_cabinet");

            // Waiting room (patients who arrived / in cabinet)
            WaitingPatients = new ObservableCollection<ProgramareDisplay>(
                list.Where(p =>
                    p.Status is "In_asteptare" or "In_cabinet" or
                                "Programata"  or "Confirmata"));

            var timelineList = new ObservableCollection<DashboardTimelineItem>();
            foreach (var appointment in list.OrderBy(p => p.DataOra).Take(8))
            {
                timelineList.Add(new DashboardTimelineItem(appointment));
            }
            MedicalTimeline = timelineList;

            var pacienti = await _app.Pacienti.GetAllAsync();
            PacientiActivi = pacienti.Count;

            var doctors = (await _app.Medici.GetAllAsync()).Where(m => m.Verificat).ToList();
            TotalStaffCount = doctors.Count;
            AvailableStaffCount = await CalculateAvailableStaffAsync(doctors);
            ShiftSummary = AvailableStaffCount == 0
                ? "Niciun medic nu este în tură chiar acum"
                : AvailableStaffCount == TotalStaffCount
                    ? "Echipă completă în tură"
                    : "Tura este în desfășurare";

            ClinicMessage = InAsteptareCount > 0
                ? $"{InAsteptareCount} pacienți așteaptă preluarea."
                : InRoomCount > 0
                    ? $"{InRoomCount} consultații sunt deja în cabinet."
                    : "Totul este stabil în clinică.";
        }
        catch (Exception ex)
        {
            ClinicMessage = $"Dashboard indisponibil temporar: {ex.Message}";
            await _app.JurnalRepo.LogAsync("LOAD_DOCTOR_DASHBOARD_ERROR", "DoctorDashboard", ex.Message, "Eroare", _utilizator.Id);
        }

        OnPropertyChanged(nameof(StaffAvailabilityText));
        OnPropertyChanged(nameof(LivePulseCaption));
    }

    private static string GetInitiale(string prenume, string nume)
    {
        var p = prenume.Length > 0 ? prenume[0].ToString() : "";
        var n = nume.Length    > 0 ? nume[0].ToString()    : "";
        return (p + n).ToUpper();
    }

    private async Task<int> CalculateAvailableStaffAsync(List<Medic> doctors)
    {
        if (doctors.Count == 0) return 0;
        var now = DateTime.Now;
        var dayName = GetRomanianDayName(now.DayOfWeek);
        var checks = doctors.Select(async doctor =>
        {
            var schedule = await _app.Medici.GetProgramAsync(doctor.Id);
            return schedule.Any(slot =>
                slot.Activ &&
                string.Equals(slot.ZiSaptamana, dayName, StringComparison.OrdinalIgnoreCase) &&
                now.TimeOfDay >= slot.OraStart &&
                now.TimeOfDay <= slot.OraSfarsit);
        });
        var results = await Task.WhenAll(checks);
        return results.Count(x => x);
    }

    private async Task UpdatePrAsync(object? parameter)
    {
        if (parameter is not ProgramareDisplay appointment)
        {
            return;
        }

        try
        {
            await _app.Programari.UpdateStatusAsync(appointment.Id, "In_cabinet");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ClinicMessage = $"Nu s-a putut porni consultația: {ex.Message}";
            await _app.JurnalRepo.LogAsync("START_CONSULTATION_ERROR", "DoctorDashboard", ex.Message, "Eroare", _utilizator.Id);
        }
    }

    private static string GetRomanianDayName(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Luni",
            DayOfWeek.Tuesday => "Marti",
            DayOfWeek.Wednesday => "Miercuri",
            DayOfWeek.Thursday => "Joi",
            DayOfWeek.Friday => "Vineri",
            DayOfWeek.Saturday => "Sambata",
            _ => "Duminica"
        };
    }
}

public class DashboardTimelineItem
{
    public DashboardTimelineItem(ProgramareDisplay appointment)
    {
        Appointment = appointment;
    }

    public ProgramareDisplay Appointment { get; }
    public string PatientName => string.IsNullOrWhiteSpace(Appointment.MedicNume) ? "Pacient" : Appointment.MedicNume;
    public string Initials => string.IsNullOrWhiteSpace(Appointment.Initiale) ? "P" : Appointment.Initiale;
    public string AppointmentTime => Appointment.DataOra.ToString("HH:mm");
    public string SecondaryLine => string.IsNullOrWhiteSpace(Appointment.MotivVizita) ? Appointment.Tip : Appointment.MotivVizita!;
    public string StatusLabel
    {
        get
        {
            if (Appointment.Status == "In_cabinet") return "In Room";
            if (Appointment.Status is "Programata" or "Confirmata" && Appointment.DataOra < DateTime.Now.AddMinutes(-10)) return "Delayed";
            if (Appointment.Status == "Finalizata") return "Completed";
            return "On Time";
        }
    }

    public string StatusTone
    {
        get
        {
            return StatusLabel switch
            {
                "In Room" => "#DDF3EC",
                "Delayed" => "#FDE8D8",
                "Completed" => "#E8EDF9",
                _ => "#EEF2F4"
            };
        }
    }

    public string StatusForeground
    {
        get
        {
            return StatusLabel switch
            {
                "In Room" => "#0F6E56",
                "Delayed" => "#C97721",
                "Completed" => "#44668C",
                _ => "#6C717A"
            };
        }
    }
}
