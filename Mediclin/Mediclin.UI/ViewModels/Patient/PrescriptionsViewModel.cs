using System.Collections.ObjectModel;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class PrescriptionsViewModel : BaseViewModel
{
    public PrescriptionsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _ = LoadAsync(app, utilizator);
    }

    public ObservableCollection<Reteta> Retete { get; } = new();

    private async Task LoadAsync(ApplicationServices app, Utilizator utilizator)
    {
        try
        {
            var p = await app.Pacienti.GetByUtilizatorIdAsync(utilizator.Id);
            if (p is null)
            {
                return;
            }

            foreach (var r in await app.Retete.GetRetetePacientAsync(p.Id))
            {
                Retete.Add(r);
            }
        }
        catch
        {
            // ignorat
        }
    }
}
