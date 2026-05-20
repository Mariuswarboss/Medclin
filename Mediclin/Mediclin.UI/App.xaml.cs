using System.Windows;
using Mediclin.Business.Services;
using Mediclin.Data.Context;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Auth;
using Mediclin.UI.Views.Auth;

namespace Mediclin.UI;

public partial class App : Application
{
    public static ApplicationServices Services { get; private set; } = null!;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var dbConnected = await ConnectionFactory.TestConnectionAsync();
        if (!dbConnected)
        {
            var detalii = string.IsNullOrWhiteSpace(ConnectionFactory.LastErrorMessage)
                ? "Nu au fost disponibile detalii tehnice."
                : ConnectionFactory.LastErrorMessage;

            MessageBox.Show(
                $"Nu s-a putut conecta la baza de date mediclin_db.\n\nDetalii: {detalii}\n\nVerificați că MySQL rulează și setările din appsettings.json.",
                "Eroare conexiune",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Current.Shutdown();
            return;
        }

        Services = new ApplicationServices();

        // Aplică tema (Light/Dark) salvată de utilizator
        ThemeService.ApplySaved();

        await EnsureProgramMediciAsync();

        var authService = new AuthService(Services.Utilizatori);
        var loginVm = new LoginViewModel(authService, Services);
        new LoginWindow(loginVm).Show();
    }

    private static async Task EnsureProgramMediciAsync()
    {
        try
        {
            var db = new DatabaseContext();
            await db.ExecuteAsync(@"
                INSERT IGNORE INTO program_medici
                    (medic_id, zi_saptamana, ora_start, ora_sfarsit, activ)
                SELECT m.id, z.zi, '09:00:00', '17:00:00', 1
                FROM   medici m
                CROSS  JOIN (
                    SELECT 'Luni'     AS zi UNION ALL
                    SELECT 'Marti'         UNION ALL
                    SELECT 'Miercuri'      UNION ALL
                    SELECT 'Joi'           UNION ALL
                    SELECT 'Vineri'
                ) z
                WHERE  m.id NOT IN (
                    SELECT DISTINCT medic_id FROM program_medici
                )",
                null);
        }
        catch { }
    }
}
