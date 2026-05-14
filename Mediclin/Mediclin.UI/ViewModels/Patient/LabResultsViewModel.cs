using System.Collections.ObjectModel;
using System.Linq;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class LabResultsViewModel : BaseViewModel
{
    public LabResultsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _ = LoadAsync(app, utilizator);
    }

    public ObservableCollection<RezultatAnaliza> Rezultate { get; } = new();
    public ObservableCollection<ValoareAnaliza> UltimeleValori { get; } = new();

    private async Task LoadAsync(ApplicationServices app, Utilizator utilizator)
    {
        try
        {
            var p = await app.Pacienti.GetByUtilizatorIdAsync(utilizator.Id);
            if (p is null)
            {
                return;
            }

            var list = await app.AnalizeRepo.GetByPacientAsync(p.Id);
            foreach (var r in list)
            {
                Rezultate.Add(r);
            }

            var first = list.FirstOrDefault();
            if (first is not null)
            {
                foreach (var v in await app.AnalizeRepo.GetValoriAsync(first.Id))
                {
                    UltimeleValori.Add(v);
                }
            }
        }
        catch
        {
            // ignorat
        }
    }
}
