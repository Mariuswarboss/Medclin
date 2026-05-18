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

        _ = InitializeAsync();
    }

    public Utilizator UtilizatorCurent => _utilizator;
    public string MedicNume => _utilizator.NumeComplet;

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
        set => SetProperty(ref _unreadCount, value);
    }

    public string ActiveNav
    {
        get => _activeNav;
        set => SetProperty(ref _activeNav, value);
    }

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToPacientiCommand { get; }
    public ICommand NavigateToProgramariCommand { get; }
    public ICommand NavigateToEmrCommand { get; }
    public ICommand NavigateToRapoarteCommand { get; }
    public ICommand NavigateToMesajeCommand { get; }
    public ICommand NavigateToSetariCommand { get; }
    public ICommand LogoutCommand { get; }

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
        }
        catch
        {
            PacientiCount = 0;
            AziCount = 0;
        }
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
}
