using System.Windows;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Patient;

namespace Mediclin.UI.Views.Patient;

public partial class PatientMainWindow : Window
{
    public PatientMainWindow(Utilizator utilizator, IAuthService authService, ApplicationServices app)
    {
        InitializeComponent();
        DataContext = new PatientMainViewModel(utilizator, authService, app) { CloseAction = Close };
    }
}
