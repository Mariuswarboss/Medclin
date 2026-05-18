using System;
using System.Windows;
using System.Windows.Input;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Admin;
using Mediclin.UI.Views.Patient;

namespace Mediclin.UI.ViewModels.Patient;

public class PatientMainViewModel : BaseViewModel
{
    private readonly Utilizator _utilizator;
    private readonly IAuthService _auth;
    private readonly ApplicationServices _app;
    private object? _currentView;
    private int _programariBadge;
    private int _reteteBadge;

    public PatientMainViewModel(Utilizator utilizator, IAuthService auth, ApplicationServices app)
    {
        _utilizator = utilizator;
        _auth = auth;
        _app = app;
        NavigateCommand = new RelayCommand(s => Navigate(s?.ToString() ?? string.Empty));
        LogoutCommand = new RelayCommand(_ => Logout());
        _ = LoadBadgesAsync();
        Navigate("PatientDashboard");
    }

    public string NumeComplet => _utilizator.NumeComplet;
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public int ProgramariBadge
    {
        get => _programariBadge;
        set => SetProperty(ref _programariBadge, value);
    }

    public int ReteteBadge
    {
        get => _reteteBadge;
        set => SetProperty(ref _reteteBadge, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
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
            ProgramariBadge = prog.Count(x => x.DataOra >= DateTime.Today && x.Status != "Anulata");
            var ret = await _app.ReteteRepo.GetActiveAsync(p.Id);
            ReteteBadge = ret.Count;
        }
        catch
        {
            ProgramariBadge = 0;
            ReteteBadge = 0;
        }
    }

    private void Navigate(string section)
    {
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

    private void Logout()
    {
        _app.CurrentUserId = null;
        _auth.Logout();
        new LoginWindow(new LoginViewModel(_auth, _app)).Show();
        CloseAction?.Invoke();
    }
}
