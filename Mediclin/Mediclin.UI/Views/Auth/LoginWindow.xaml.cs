using System.Windows;
using Mediclin.Business.Services;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Auth;

namespace Mediclin.UI.Views.Auth;

public partial class LoginWindow : Window
{
    public LoginWindow(IAuthService authService, ApplicationServices app)
        : this(new LoginViewModel(authService, app))
    {
    }

    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;
    }
}
