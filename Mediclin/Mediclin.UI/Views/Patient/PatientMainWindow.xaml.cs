using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Patient;

namespace Mediclin.UI.Views.Patient;

public partial class PatientMainWindow : Window
{
    private readonly DispatcherTimer _inactivityTimer;

    public PatientMainWindow(Utilizator utilizator, IAuthService authService, ApplicationServices app)
    {
        InitializeComponent();
        DataContext = new PatientMainViewModel(utilizator, authService, app) { CloseAction = Close };

        _inactivityTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _inactivityTimer.Tick += (_, _) =>
        {
            _inactivityTimer.Stop();
            if (DataContext is PatientMainViewModel vm && vm.LogoutCommand.CanExecute(null))
            {
                vm.LogoutCommand.Execute(null);
            }
        };
        _inactivityTimer.Start();

        PreviewMouseMove += ResetInactivityTimer;
        PreviewMouseDown += ResetInactivityTimer;
        PreviewKeyDown += ResetInactivityTimer;
        Closing += (_, _) => _inactivityTimer.Stop();
    }

    private void ResetInactivityTimer(object sender, InputEventArgs e)
    {
        _inactivityTimer.Stop();
        _inactivityTimer.Start();
    }
}
