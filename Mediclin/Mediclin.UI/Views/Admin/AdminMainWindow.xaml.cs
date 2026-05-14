using System.Windows;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Admin;

namespace Mediclin.UI.Views.Admin;

public partial class AdminMainWindow : Window
{
    public AdminMainWindow(Utilizator utilizator, IAuthService authService, ApplicationServices app)
    {
        InitializeComponent();
        DataContext = new AdminMainViewModel(utilizator, authService, app) { CloseAction = Close };
    }
}
