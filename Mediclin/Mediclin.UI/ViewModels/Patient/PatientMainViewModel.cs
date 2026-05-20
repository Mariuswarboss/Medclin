using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Admin;
using Mediclin.UI.Views.Patient;
using Mediclin.UI.Views.Shared;

namespace Mediclin.UI.ViewModels.Patient;

public class PatientMainViewModel : BaseViewModel
{
    private readonly Utilizator _utilizator;
    private readonly IAuthService _auth;
    private readonly ApplicationServices _app;
    private object? _currentView;
    private int _programariBadge;
    private int _reteteBadge;
    private int _notificationCount;
    private string _activeNav = "Dashboard";
    private bool _isSidebarCollapsed;
    private bool _isDarkTheme;
    private string _globalSearchQuery = string.Empty;

    public PatientMainViewModel(Utilizator utilizator, IAuthService auth, ApplicationServices app)
    {
        _utilizator = utilizator;
        _auth = auth;
        _app = app;
        NavigateCommand = new RelayCommand(s => Navigate(s?.ToString() ?? string.Empty));
        LogoutCommand = new RelayCommand(_ => Logout());
        ToggleSidebarCommand = new RelayCommand(_ => ToggleSidebar());
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        OpenNotificationsCommand = new RelayCommand(_ => OpenNotifications());
        GlobalSearchCommand = new AsyncRelayCommand(async _ => await RefreshGlobalSearchAsync());
        GlobalSearchResults = new ObservableCollection<string>();
        _isDarkTheme = ThemeService.IsDark;
        _ = LoadBadgesAsync();
        Navigate("PatientDashboard");
    }

    public string NumeComplet => _utilizator.NumeComplet;
    public string Prenume => _utilizator.Prenume;
    public string Initiale => string.Concat((_utilizator.NumeComplet ?? "P").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
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
        "Programari" => "My appointments",
        "Emr" => "Medical record",
        "Retete" => "Prescriptions",
        "Analize" => "Lab results",
        "Mesaje" => "Messages",
        "Notificari" => "Notifications",
        "Setari" => "Settings",
        _ => "Patient dashboard"
    };

    public int ProgramariBadge
    {
        get => _programariBadge;
        set
        {
            if (SetProperty(ref _programariBadge, value))
            {
                OnPropertyChanged(nameof(ProgramariMenuText));
            }
        }
    }

    public int ReteteBadge
    {
        get => _reteteBadge;
        set
        {
            if (SetProperty(ref _reteteBadge, value))
            {
                OnPropertyChanged(nameof(ReteteMenuText));
            }
        }
    }

    public int NotificationCount
    {
        get => _notificationCount;
        set
        {
            if (SetProperty(ref _notificationCount, value))
            {
                OnPropertyChanged(nameof(NotificationBadgeText));
            }
        }
    }

    public string ProgramariMenuText => $"Programarile mele  {ProgramariBadge}";
    public string ReteteMenuText => $"Retete active  {ReteteBadge}";
    public string NotificationBadgeText => NotificationCount > 99 ? "99+" : NotificationCount.ToString();

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

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ToggleSidebarCommand { get; }
    public ICommand ToggleThemeCommand { get; }
    public ICommand OpenNotificationsCommand { get; }
    public ICommand GlobalSearchCommand { get; }
    public Action? CloseAction { get; set; }

    private async Task LoadBadgesAsync()
    {
        try
        {
            var p = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (p is null)
            {
                return;
            }

            var prog = await _app.ProgramariRepo.GetByPacientAsync(p.Id);
            ProgramariBadge = prog.Count(x => x.DataOra >= DateTime.Today && x.Status is "Programata" or "Confirmata");
            var ret = await _app.ReteteRepo.GetActiveAsync(p.Id);
            ReteteBadge = ret.Count;
            NotificationCount = await _app.Notificari.GetUnreadCountAsync(_utilizator.Id);
        }
        catch
        {
            ProgramariBadge = 0;
            ReteteBadge = 0;
            NotificationCount = 0;
        }
    }

    private void Navigate(string section)
    {
        _ = LoadBadgesAsync();
        ActiveNav = section switch
        {
            "PatientDashboard" => "Dashboard",
            "PatientAppointments" => "Programari",
            "PatientEmr" => "Emr",
            "PatientPrescriptions" => "Retete",
            "PatientLabs" => "Analize",
            "PatientMessages" => "Mesaje",
            "PatientSettings" => "Setari",
            _ => ActiveNav
        };

        var previous = CurrentView;
        var next = section switch
        {
            "PatientDashboard" => new PatientDashboardView { DataContext = new PatientDashboardViewModel(_app, _utilizator) },
            "PatientAppointments" => new MyAppointmentsView { DataContext = new MyAppointmentsViewModel(_app, _utilizator) },
            "PatientEmr" => new MyEMRView { DataContext = new MyEMRViewModel(_app, _utilizator) },
            "PatientPrescriptions" => new PrescriptionsView { DataContext = new PrescriptionsViewModel(_app, _utilizator) },
            "PatientLabs" => new LabResultsView { DataContext = new LabResultsViewModel(_app, _utilizator) },
            "PatientMessages" => new MessagesView { DataContext = new MessagesViewModel(_app, _utilizator, false) },
            "PatientSettings" => new AccountSettingsView { DataContext = new AccountSettingsViewModel(_app, _utilizator) },
            _ => CurrentView
        };
        CurrentView = next;
        if (previous is IDisposable d && !ReferenceEquals(previous, next))
        {
            d.Dispose();
        }
    }

    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    private void OpenNotifications()
    {
        ActiveNav = "Notificari";
        var previous = CurrentView;
        CurrentView = new NotificationsView
        {
            DataContext = new NotificationsViewModel(_app, _utilizator, count => NotificationCount = count)
        };

        if (previous is IDisposable d)
        {
            d.Dispose();
        }
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

            var patient = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (patient is null)
            {
                GlobalSearchResults.Add("No patient profile is linked to this account.");
                return;
            }

            var doctors = await _app.Medici.GetAllAsync();
            foreach (var doctor in doctors.Where(d =>
                         Match(d.NumeCompletCuTitlu) || Match(d.SpecialitateNume) || Match(d.Email)).Take(4))
            {
                GlobalSearchResults.Add($"Doctor: {doctor.NumeCompletCuTitlu}  {doctor.SpecialitateNume}");
            }

            var appointments = await _app.ProgramariRepo.GetByPacientAsync(patient.Id);
            foreach (var appointment in appointments.Where(a =>
                         Match(a.MedicNume) || Match(a.SpecialitateNume) ||
                         Match(a.MotivVizita) || Match(a.Status) || Match(a.Tip)).Take(5))
            {
                GlobalSearchResults.Add($"Appointment: {appointment.DataOra:dd MMM HH:mm}  {appointment.MedicNume}  {appointment.Status}");
            }

            var prescriptions = await _app.ReteteRepo.GetByPacientAsync(patient.Id);
            foreach (var prescription in prescriptions.Where(r =>
                         Match(r.MedicNume) || Match(r.Status) || Match(r.Observatii) ||
                         r.Medicamente.Any(m => Match(m.Denumire) || Match(m.Dozaj) || Match(m.Instructiuni))).Take(5))
            {
                GlobalSearchResults.Add($"Prescription: {prescription.DataEmitere:dd MMM yyyy}  {prescription.Status}  {prescription.MedicNume}");
            }

            var labs = await _app.AnalizeRepo.GetByPacientAsync(patient.Id);
            foreach (var lab in labs.Where(l =>
                         Match(l.Laborator) || Match(l.Interpretare) || Match(l.PdfUrl)).Take(5))
            {
                GlobalSearchResults.Add($"Analysis: {lab.DataRecoltare:dd MMM yyyy}  abnormal values: {lab.Anormale}");
            }

            var conversations = await _app.MesajeRepo.GetConversatiiForPacientAsync(patient.Id, _utilizator.Id);
            foreach (var conversation in conversations.Where(c =>
                         Match(c.PartenerNume) || Match(c.UltimMesajScurt)).Take(5))
            {
                GlobalSearchResults.Add($"Message: {conversation.PartenerNume}  {conversation.UltimMesajScurt}");
            }

            if (GlobalSearchResults.Count == 0)
            {
                GlobalSearchResults.Add("No matching doctors, appointments, prescriptions, analyses, or messages.");
            }
        }
        catch
        {
            GlobalSearchResults.Clear();
            GlobalSearchResults.Add("Search is temporarily unavailable.");
        }
    }

    private void Logout()
    {
        _app.CurrentUserId = null;
        _auth.Logout();
        new LoginWindow(new LoginViewModel(_auth, _app)).Show();
        CloseAction?.Invoke();
    }
}
