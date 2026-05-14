using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Mediclin.Data.Context;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public class AdminDashboardViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private string _sanatate = "Se verifică...";
    private Mediclin.Business.DTOs.RaportDto _statistici = new();

    public AdminDashboardViewModel(ApplicationServices app)
    {
        _app = app;
        _ = LoadAsync();
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        t.Tick += async (_, _) => await LoadAsync();
        t.Start();
    }

    public string SanatateSistem
    {
        get => _sanatate;
        set => SetProperty(ref _sanatate, value);
    }

    public Mediclin.Business.DTOs.RaportDto Statistici
    {
        get => _statistici;
        set => SetProperty(ref _statistici, value);
    }

    public ObservableCollection<Dictionary<string, object>> Activitate { get; } = new();

    private async Task LoadAsync()
    {
        try
        {
            var ok = await ConnectionFactory.TestConnectionAsync();
            SanatateSistem = ok
                ? "Toate sistemele funcționează"
                : "Eroare conexiune bază de date";
            Statistici = await _app.Rapoarte.GetStatisticiAdminAsync();
            var rows = await _app.JurnalRepo.GetRecentAsync(10);
            Activitate.Clear();
            foreach (var r in rows)
            {
                Activitate.Add(r);
            }
        }
        catch (Exception ex)
        {
            SanatateSistem = "Eroare: " + ex.Message;
        }
    }
}
