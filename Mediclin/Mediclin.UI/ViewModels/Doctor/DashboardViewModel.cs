using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using System.Linq;

namespace Mediclin.UI.ViewModels.Doctor;

public class DashboardViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private readonly int _medicId;
    private readonly DispatcherTimer _clockTimer;
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

    public DashboardViewModel(ApplicationServices app, Utilizator utilizator, int medicId)
    {
        _app = app;
        _utilizator = utilizator;
        _medicId = medicId;
        TodayAppointments = new ObservableCollection<Programare>();
        WaitingPatients = new ObservableCollection<Programare>();
        MedicalTimeline = new ObservableCollection<DashboardTimelineItem>();
        UpdateStatusCommand = new AsyncRelayCommand(async p => await UpdatePrAsync(p));

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            CurrentTimeText = DateTime.Now.ToString("HH:mm");
            CurrentDateText = DateTime.Now.ToString("dddd, d MMMM yyyy", new CultureInfo("ro-RO"));
        };
        _clockTimer.Start();

        _ = LoadAsync();
    }

    public ObservableCollection<Programare> TodayAppointments { get; }
    public ObservableCollection<Programare> WaitingPatients { get; }
    public ObservableCollection<DashboardTimelineItem> MedicalTimeline { get; }

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
        if (_medicId <= 0)
        {
            return;
        }

        try
        {
            var list = await _app.ProgramariRepo.GetTodayAsync(_medicId);
            TodayAppointments.Clear();
            foreach (var appointment in list.OrderBy(x => x.DataOra))
            {
                TodayAppointments.Add(appointment);
            }

            WaitingPatients.Clear();
            foreach (var waiting in list.Where(p => p.Status is "In_asteptare" or "Programata" or "Confirmata").OrderBy(p => p.DataOra))
            {
                WaitingPatients.Add(waiting);
            }

            MedicalTimeline.Clear();
            foreach (var appointment in list.OrderBy(p => p.DataOra).Take(8))
            {
                MedicalTimeline.Add(new DashboardTimelineItem(appointment));
            }

            TotalAziCount = list.Count;
            FinalizateCount = list.Count(p => string.Equals(p.Status, "Finalizata", StringComparison.OrdinalIgnoreCase));
            InAsteptareCount = WaitingPatients.Count;
            InRoomCount = list.Count(p => string.Equals(p.Status, "In_cabinet", StringComparison.OrdinalIgnoreCase));
            ActivePatientsNow = list.Count(p => p.Status is "In_asteptare" or "Programata" or "Confirmata" or "In_cabinet");

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

    private async Task<int> CalculateAvailableStaffAsync(List<Medic> doctors)
    {
        if (doctors.Count == 0)
        {
            return 0;
        }

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
        if (parameter is not Programare appointment)
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
    public DashboardTimelineItem(Programare appointment)
    {
        Appointment = appointment;
    }

    public Programare Appointment { get; }
    public string PatientName => Appointment.PacientNume ?? "Pacient";
    public string Initials => string.Concat((Appointment.PacientNume ?? "P").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();
    public string AppointmentTime => Appointment.DataOra.ToString("HH:mm");
    public string SecondaryLine => string.IsNullOrWhiteSpace(Appointment.MotivVizita) ? Appointment.Tip : Appointment.MotivVizita!;
    public string StatusLabel
    {
        get
        {
            if (Appointment.Status == "In_cabinet")
            {
                return "In Room";
            }

            if (Appointment.Status is "Programata" or "Confirmata" && Appointment.DataOra < DateTime.Now.AddMinutes(-10))
            {
                return "Delayed";
            }

            if (Appointment.Status == "Finalizata")
            {
                return "Completed";
            }

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
