using System.Windows;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Doctor;

namespace Mediclin.UI.Views.Doctor;

public partial class DoctorMainWindow : Window
{
    public DoctorMainWindow(Utilizator utilizator, IAuthService authService, ApplicationServices app)
    {
        InitializeComponent();
        DataContext = new DoctorMainViewModel(utilizator, authService, app) { CloseAction = Close };
    }
}
