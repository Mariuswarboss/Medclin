using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Doctor;
using Mediclin.UI.Views.Shared;

namespace Mediclin.UI.ViewModels.Doctor;

public class DoctorMainViewModel : BaseViewModel
{
    private readonly Utilizator _utilizator;
    private readonly IAuthService _auth;
    private readonly ApplicationServices _app;
    private object? _currentView;
    private Medic? _medic;
    private int _medicId;
    private int _specialitateId;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _pacientiCount;
    private int _aziCount;
    private int _unreadCount;
    private string _activeNav = "Dashboard";
    private bool _isSidebarCollapsed;
    private bool _isDarkTheme;
    private string _globalSearchQuery = string.Empty;
    private string _workStatusText = "Status program indisponibil";

    public DoctorMainViewModel(Utilizator utilizator, IAuthService auth, ApplicationServices app)
    {
        _utilizator = utilizator;
        _auth = auth;
        _app = app;

        NavigateToDashboardCommand = new AsyncRelayCommand(async _ => await NavigateDashboardAsync());
        NavigateToPacientiCommand = new RelayCommand(_ => NavigatePacienti());
        NavigateToProgramariCommand = new AsyncRelayCommand(async _ => await NavigateProgramariAsync());
        NavigateToEmrCommand = new RelayCommand(_ => NavigateEmr());
        NavigateToRapoarteCommand = new AsyncRelayCommand(async _ => await NavigateRapoarteAsync());
        NavigateToMesajeCommand = new RelayCommand(_ => NavigateMesaje());
        NavigateToSetariCommand = new RelayCommand(_ => NavigateSetari());
        LogoutCommand = new RelayCommand(_ => Logout());
        ToggleSidebarCommand = new RelayCommand(_ => ToggleSidebar());
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        OpenNotificationsCommand = new RelayCommand(_ => OpenNotifications());
        GlobalSearchCommand = new AsyncRelayCommand(async _ => await RefreshGlobalSearchAsync());
        GlobalSearchResults = new ObservableCollection<string>();
        _isDarkTheme = ThemeService.IsDark;

        _ = InitializeAsync();
    }

    public Utilizator UtilizatorCurent => _utilizator;
    public string MedicNume => _utilizator.NumeComplet;
    public string UserInitials => string.Concat((_utilizator.NumeComplet ?? "M").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public int PacientiCount
    {
        get => _pacientiCount;
        set => SetProperty(ref _pacientiCount, value);
    }

    public int AziCount
    {
        get => _aziCount;
        set => SetProperty(ref _aziCount, value);
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (SetProperty(ref _unreadCount, value))
            {
                OnPropertyChanged(nameof(NotificationBadgeText));
            }
        }
    }

    public string ActiveNav
    {
        get => _activeNav;
        set
        {
            if (SetProperty(ref _activeNav, value))
            {
                OnPropertyChanged(nameof(SectionTitle));
            }
        }
    }

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (SetProperty(ref _isSidebarCollapsed, value))
            {
                OnPropertyChanged(nameof(SidebarWidth));
                OnPropertyChanged(nameof(SidebarGridWidth));
                OnPropertyChanged(nameof(SidebarTextVisibility));
                OnPropertyChanged(nameof(SidebarToggleText));
            }
        }
    }

    public int SidebarWidth => IsSidebarCollapsed ? 128 : 292;
    public GridLength SidebarGridWidth => new(SidebarWidth);
    public Visibility SidebarTextVisibility => IsSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
    public string SidebarToggleText => IsSidebarCollapsed ? ">" : "<";
    public string SectionTitle => ActiveNav switch
    {
        "Pacienti" => "Patient management",
        "Programari" => "Appointments",
        "Emr" => "Medical records",
        "Rapoarte" => "Financial analytics",
        "Mesaje" => "Messages",
        "Notificari" => "Notifications",
        "Setari" => "Settings",
        _ => "Doctor dashboard"
    };

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ThemeService.Apply(value);
                OnPropertyChanged(nameof(ThemeButtonText));
            }
        }
    }

    public string ThemeButtonText => IsDarkTheme ? "Light" : "Dark";
    public string NotificationBadgeText => UnreadCount > 99 ? "99+" : UnreadCount.ToString();

    public string WorkStatusText
    {
        get => _workStatusText;
        set => SetProperty(ref _workStatusText, value);
    }

    public string GlobalSearchQuery
    {
        get => _globalSearchQuery;
        set
        {
            if (SetProperty(ref _globalSearchQuery, value))
            {
                _ = RefreshGlobalSearchAsync();
            }
        }
    }

    public ObservableCollection<string> GlobalSearchResults { get; }

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToPacientiCommand { get; }
    public ICommand NavigateToProgramariCommand { get; }
    public ICommand NavigateToEmrCommand { get; }
    public ICommand NavigateToRapoarteCommand { get; }
    public ICommand NavigateToMesajeCommand { get; }
    public ICommand NavigateToSetariCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ToggleSidebarCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand OpenNotificationsCommand { get; }
    public ICommand GlobalSearchCommand { get; }

    public Action? CloseAction { get; set; }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            _medic = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
            _medicId = _medic?.Id ?? 0;
            if (_medic is null)
            {
                ErrorMessage = "Contul de medic nu este încă configurat în sistem.";
                NavigateSetari();
                return;
            }

            await LoadCountsAsync();
            await NavigateDashboardAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Nu s-a putut încărca modulul medic: {ex.Message}";
            await _app.JurnalRepo.LogAsync("LOAD_DOCTOR_MAIN_ERROR", "DoctorMain", ex.Message, "Eroare", _utilizator.Id);
            NavigateSetari();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCountsAsync()
    {
        try
        {
            if (_medicId <= 0)
            {
                PacientiCount = 0;
                AziCount = 0;
                return;
            }

            var pacienti = await _app.Pacienti.GetAllAsync();
            PacientiCount = pacienti.Count;
            var azi = await _app.ProgramariRepo.GetTodayAsync(_medicId);
            AziCount = azi.Count;
            UnreadCount = await _app.Notificari.GetUnreadCountAsync(_utilizator.Id);
            WorkStatusText = await GetCurrentWorkStatusAsync();
        }
        catch
        {
            PacientiCount = 0;
            AziCount = 0;
            WorkStatusText = "Status program indisponibil";
        }
    }

    private async Task<string> GetCurrentWorkStatusAsync()
    {
        if (_medicId <= 0)
        {
            return "Status program indisponibil";
        }

        var now = DateTime.Now;
        var dayName = GetRomanianDayName(now.DayOfWeek);
        var program = await _app.Medici.GetProgramAsync(_medicId);
        var currentSlot = program.FirstOrDefault(slot =>
            slot.Activ &&
            string.Equals(slot.ZiSaptamana, dayName, StringComparison.OrdinalIgnoreCase) &&
            now.TimeOfDay >= slot.OraStart &&
            now.TimeOfDay <= slot.OraSfarsit);

        if (currentSlot is not null)
        {
            return $"La lucru acum, pana la {currentSlot.OraSfarsit:hh\\:mm}";
        }

        var nextSlotToday = program
            .Where(slot => slot.Activ &&
                           string.Equals(slot.ZiSaptamana, dayName, StringComparison.OrdinalIgnoreCase) &&
                           slot.OraStart > now.TimeOfDay)
            .OrderBy(slot => slot.OraStart)
            .FirstOrDefault();

        return nextSlotToday is null
            ? "In afara programului"
            : $"Incepe la {nextSlotToday.OraStart:hh\\:mm}";
    }

    public async Task RefreshCountsPublicAsync() => await LoadCountsAsync();

    private void NavigateTo(UserControl view)
    {
        if (CurrentView is IDisposable disposable)
        {
            disposable.Dispose();
        }

        CurrentView = view;
    }

    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    private void OpenNotifications()
    {
        ActiveNav = "Notificari";
        var view = new NotificationsView
        {
            DataContext = new NotificationsViewModel(_app, _utilizator, count => UnreadCount = count)
        };
        NavigateTo(view);
    }

    private async Task RefreshGlobalSearchAsync()
    {
        var term = (GlobalSearchQuery ?? string.Empty).Trim();
        GlobalSearchResults.Clear();
        if (term.Length < 2)
        {
            return;
        }

        try
        {
            bool Match(string? value) => !string.IsNullOrWhiteSpace(value) &&
                value.Contains(term, StringComparison.OrdinalIgnoreCase);

            var patients = await _app.Pacienti.GetAllAsync();
            foreach (var patient in patients.Where(p =>
                         Match(p.NumeComplet) || Match(p.Email) || Match(p.Telefon) || Match(p.SpecialitateNume)).Take(5))
            {
                GlobalSearchResults.Add($"Patient: {patient.NumeComplet}  {patient.Email}");
            }

            var doctors = await _app.Medici.GetAllAsync();
            foreach (var doctor in doctors.Where(d =>
                         Match(d.NumeCompletCuTitlu) || Match(d.SpecialitateNume) || Match(d.Email)).Take(4))
            {
                GlobalSearchResults.Add($"Doctor: {doctor.NumeCompletCuTitlu}  {doctor.SpecialitateNume}");
            }

            if (_medicId > 0)
            {
                var appointments = await _app.ProgramariRepo.GetByMedicAsync(_medicId, DateTime.Today.AddMonths(-2), DateTime.Today.AddMonths(3));
                foreach (var appointment in appointments.Where(a =>
                             Match(a.PacientNume) || Match(a.MedicNume) || Match(a.SpecialitateNume) ||
                             Match(a.MotivVizita) || Match(a.Status) || Match(a.Tip)).Take(5))
                {
                    GlobalSearchResults.Add($"Appointment: {appointment.DataOra:dd MMM HH:mm}  {appointment.PacientNume}  {appointment.Status}");
                }

                var records = await _app.ConsultatiiRepo.GetByMedicAsync(_medicId);
                foreach (var record in records.Where(r =>
                             Match(r.DiagnosticText) || Match(r.DiagnosticCod) || Match(r.Simptome) || Match(r.Recomandari)).Take(4))
                {
                    GlobalSearchResults.Add($"Record: {record.DataConsultatie:dd MMM yyyy}  {record.DiagnosticText ?? record.DiagnosticCod}");
                }
            }

            if (GlobalSearchResults.Count == 0)
            {
                GlobalSearchResults.Add("No matching patients, doctors, appointments, records, prescriptions, analyses, or messages.");
            }
        }
        catch
        {
            GlobalSearchResults.Clear();
            GlobalSearchResults.Add("Search is temporarily unavailable.");
        }
    }

    private async Task NavigateDashboardAsync()
    {
        ActiveNav = "Dashboard";
        if (_medicId <= 0)
        {
            _medic = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
            _medicId = _medic?.Id ?? 0;
            _specialitateId = _medic?.SpecialitateId ?? 0;
        }

        var vm = new DashboardViewModel(_app, _utilizator, _medicId);
        var view = new DashboardView { DataContext = vm };
        NavigateTo(view);
    }

    private void NavigatePacienti()
    {
        ActiveNav = "Pacienti";
        var vm = new PatientsViewModel(_app, OpenEmrCuPacient, _medicId, _specialitateId);
        var view = new PatientsView { DataContext = vm };
        NavigateTo(view);
    }

    private async Task NavigateProgramariAsync()
    {
        ActiveNav = "Programari";
        try
        {
            if (_medicId <= 0)
            {
                _medic = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
                _medicId = _medic?.Id ?? 0;
            }

            var vm = new AppointmentsViewModel(
                _medicId,
                _utilizator.Id,
                _utilizator.NumeComplet);

            await vm.LoadAsync();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var view = new AppointmentsView();
                view.DataContext = vm;
                NavigateTo(view);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[NAV ERROR] Programari: {ex}");
        }
    }

    private void NavigateEmr()
    {
        ActiveNav = "Emr";
        var vm = new EMRViewModel(_app, _utilizator, null, _medicId);
        var view = new EMRView { DataContext = vm };
        NavigateTo(view);
    }

    private void OpenEmrCuPacient(Pacient p)
    {
        ActiveNav = "Emr";
        var vm = new EMRViewModel(_app, _utilizator, p, _medicId);
        var view = new EMRView { DataContext = vm };
        NavigateTo(view);
    }

    private async Task NavigateRapoarteAsync()
    {
        ActiveNav = "Rapoarte";
        if (_medicId <= 0)
        {
            _medic = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
            _medicId = _medic?.Id ?? 0;
        }

        var vm = new ReportsViewModel(_app, _medicId);
        var view = new ReportsView { DataContext = vm };
        NavigateTo(view);
    }

    private void NavigateMesaje()
    {
        ActiveNav = "Mesaje";
        var view = new MessagesView { DataContext = new Mediclin.UI.ViewModels.Patient.MessagesViewModel(_app, _utilizator, true) };
        NavigateTo(view);
    }

    private void NavigateSetari()
    {
        ActiveNav = "Setari";
        var view = new SettingsView { DataContext = new SettingsViewModel(_app, _utilizator) };
        NavigateTo(view);
    }

    private void Logout()
    {
        _app.CurrentUserId = null;
        _auth.Logout();
        var loginVm = new LoginViewModel(_auth, _app);
        new LoginWindow(loginVm).Show();
        CloseAction?.Invoke();
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
