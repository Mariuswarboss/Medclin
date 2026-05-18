using System.Collections.ObjectModel;
using System.Linq;
using Mediclin.Business.DTOs;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public class FinancialViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private RaportDto _raport = new();

    public FinancialViewModel(ApplicationServices app)
    {
        _app = app;
        Zilnic = new ObservableCollection<ConsultatiiPerZiDto>();
        _ = LoadAsync();
    }

    public RaportDto Raport
    {
        get => _raport;
        set
        {
            if (!SetProperty(ref _raport, value))
            {
                return;
            }

            OnPropertyChanged(nameof(VenitLunarText));
            OnPropertyChanged(nameof(ConsultatiiText));
            OnPropertyChanged(nameof(TarifMediuText));
        }
    }

    public ObservableCollection<ConsultatiiPerZiDto> Zilnic { get; }

    public string VenitLunarText => $"{Raport.VenitEstimat:N0} MDL";
    public string ConsultatiiText => Raport.TotalConsultatii.ToString("N0");
    public string TarifMediuText
    {
        get
        {
            if (Raport.TotalConsultatii <= 0)
            {
                return "—";
            }

            return $"{(Raport.VenitEstimat / Raport.TotalConsultatii):N0} MDL";
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            var acum = DateTime.Now;
            Raport = await _app.Rapoarte.GetRaportLunarAsync(acum.Year, acum.Month);
            Zilnic.Clear();
            foreach (var z in Raport.ConsultatiiPerZi.OrderByDescending(x => x.Data))
            {
                Zilnic.Add(z);
            }
        }
        catch
        {
            // ignorat
        }
    }
}
