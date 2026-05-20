using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Admin;

namespace Mediclin.UI.ViewModels.Admin;

public class AdminMainViewModel : BaseViewModel
{
    private readonly Utilizator _utilizator;
    private readonly IAuthService _auth;
    private readonly ApplicationServices _app;
    private object? _currentView;
    private string _activeNav = "Dashboard";

    public AdminMainViewModel(Utilizator utilizator, IAuthService auth, ApplicationServices app)
    {
        _utilizator = utilizator;
        _auth = auth;
        _app = app;
        NavigateCommand = new RelayCommand(s => Navigate(s?.ToString() ?? string.Empty));
        LogoutCommand = new RelayCommand(_ => Logout());
        Navigate("AdminDashboard");
    }

    public string NumeComplet => _utilizator.NumeComplet;
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string ActiveNav
    {
        get => _activeNav;
        set => SetProperty(ref _activeNav, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public Action? CloseAction { get; set; }

    private void Navigate(string section)
    {
        ActiveNav = section switch
        {
            "AdminDashboard" => "Dashboard",
            "AdminUsers" => "Users",
            "AdminDoctors" => "Doctors",
            "AdminFinancial" => "Financial",
            "AdminLogs" => "Logs",
            "AdminSettings" => "Settings",
            _ => ActiveNav
        };

        CurrentView = section switch
        {
            "AdminDashboard" => new AdminDashboardView { DataContext = new AdminDashboardViewModel(_app) },
            "AdminUsers" => new UsersManagementView { DataContext = new UsersViewModel(_app) },
            "AdminDoctors" => new DoctorsVerificationView { DataContext = new DoctorsVerificationViewModel(_app) },
            "AdminFinancial" => new FinancialView { DataContext = new FinancialViewModel(_app) },
            "AdminLogs" => new AuditLogsView { DataContext = new AuditLogsViewModel(_app) },
            "AdminSettings" => new GlobalSettingsView { DataContext = new GlobalSettingsViewModel(_app) },
            _ => CurrentView
        };
    }

    private void Logout()
    {
        _app.CurrentUserId = null;
        _auth.Logout();
        new LoginWindow(new LoginViewModel(_auth, _app)).Show();
        CloseAction?.Invoke();
    }
}
