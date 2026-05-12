using System.Windows;
using Mediclin.UI.ViewModels.Auth;

namespace Mediclin.UI.Views.Auth;

public partial class RegisterWindow : Window
{
    public RegisterWindow(RegisterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseAction = Close;
    }
}
