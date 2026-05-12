using System.Windows;
using Mediclin.UI.ViewModels.Auth;

namespace Mediclin.UI.Views.Auth;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;
    }
}
