using System.Windows;
using Mediclin.Business.Services;
using Mediclin.Data.Context;
using Mediclin.Data.Repositories;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;

namespace Mediclin.UI;

public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var dbConnected = await ConnectionFactory.TestConnectionAsync();
        if (!dbConnected)
        {
            MessageBox.Show(
                "Nu s-a putut conecta la baza de date mediclin_db.\nVerificati ca MySQL ruleaza si setarile din appsettings.json.",
                "Eroare conexiune",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Current.Shutdown();
            return;
        }

        var db = new DatabaseContext();
        var utilizatorRepo = new UtilizatorRepository(db);
        var authService = new AuthService(utilizatorRepo);
        var loginVm = new LoginViewModel(authService);
        var loginWindow = new LoginWindow(loginVm);
        loginWindow.Show();
    }
}
