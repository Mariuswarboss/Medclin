using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mediclin.Business.DTOs;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Doctor;

public class ReportsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly int _medicId;
    private string _selectedPeriod = "Azi";
    private RaportDto _current = new();
    private string _statusMessage = string.Empty;
    private int _cardTotal;
    private int _cardFinalizate;
    private int _cardNeprezentate;
    private decimal _cardVenit;

    public ReportsViewModel(ApplicationServices app, int medicId)
    {
        _app = app;
        _medicId = medicId;
        Perioduri = new List<string> { "Azi", "Saptamana", "Luna" };
        ChartBars = new ObservableCollection<RaportBarItem>();
        RecentProgramari = new ObservableCollection<Programare>();
        LoadRaportCommand = new RelayCommand(_ => _ = LoadAsync());
        ExportPdfCommand = new RelayCommand(_ =>
        {
            MessageBox.Show("Export PDF va fi disponibil în curând.", "MediClin");
            StatusMessage = "Export PDF planificat.";
        });
        ExportCsvCommand = new RelayCommand(_ =>
        {
            MessageBox.Show("Export CSV va fi disponibil în curând.", "MediClin");
            StatusMessage = "Export CSV planificat.";
        });
        _ = LoadAsync();
    }

    public List<string> Perioduri { get; }

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public RaportDto CurrentRaport
    {
        get => _current;
        set => SetProperty(ref _current, value);
    }

    public int CardTotal
    {
        get => _cardTotal;
        set => SetProperty(ref _cardTotal, value);
    }

    public int CardFinalizate
    {
        get => _cardFinalizate;
        set => SetProperty(ref _cardFinalizate, value);
    }

    public int CardNeprezentate
    {
        get => _cardNeprezentate;
        set => SetProperty(ref _cardNeprezentate, value);
    }

    public decimal CardVenit
    {
        get => _cardVenit;
        set => SetProperty(ref _cardVenit, value);
    }

    public ObservableCollection<RaportBarItem> ChartBars { get; }
    public ObservableCollection<Programare> RecentProgramari { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand LoadRaportCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportCsvCommand { get; }

    private async Task LoadAsync()
    {
        if (_medicId <= 0)
        {
            return;
        }

        try
        {
            DateTime from;
            DateTime to;

            switch (SelectedPeriod)
            {
                case "Saptamana":
                    from = DateTime.Today.AddDays(-7);
                    to = DateTime.Today.AddDays(1).AddTicks(-1);
                    CurrentRaport = await _app.Rapoarte.GetStatisticiMedicAsync(_medicId, from, to);
                    break;
                case "Luna":
                    CurrentRaport = await _app.Rapoarte.GetRaportLunarAsync(_medicId, DateTime.Today.Year, DateTime.Today.Month);
                    from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                    break;
                default:
                    from = DateTime.Today;
                    to = DateTime.Today.AddDays(1).AddTicks(-1);
                    CurrentRaport = await _app.Rapoarte.GetRaportZilnicAsync(_medicId, DateTime.Today);
                    break;
            }

            CardTotal = CurrentRaport.TotalProgramari > 0 ? CurrentRaport.TotalProgramari : CurrentRaport.TotalConsultatii;
            CardFinalizate = CurrentRaport.ProgramariFinalizate > 0 ? CurrentRaport.ProgramariFinalizate : CurrentRaport.TotalConsultatii;
            CardNeprezentate = CurrentRaport.ProgramariNeprezentate;
            CardVenit = CurrentRaport.VenitEstimat;

            BuildChart();
            await LoadRecentProgramariAsync(from, to);
            StatusMessage = "Raport actualizat.";
        }
        catch (Exception ex)
        {
            CurrentRaport = new RaportDto();
            ChartBars.Clear();
            StatusMessage = ex.Message;
        }
    }

    private void BuildChart()
    {
        ChartBars.Clear();
        var items = CurrentRaport.ConsultatiiPerZi;
        if (items.Count > 0)
        {
            var max = items.Max(i => i.Total);
            if (max <= 0)
            {
                max = 1;
            }

            foreach (var day in items)
            {
                var h = 140.0 * day.Total / max;
                ChartBars.Add(new RaportBarItem
                {
                    Label = day.Data.ToString("dd.MM", CultureInfo.InvariantCulture),
                    Count = day.Total,
                    BarHeight = Math.Max(4, h)
                });
            }

            return;
        }

        var total = CurrentRaport.TotalProgramari > 0 ? CurrentRaport.TotalProgramari : CurrentRaport.TotalConsultatii;
        var barH = total > 0 ? Math.Min(140, 40 + total * 8.0) : 4;
        ChartBars.Add(new RaportBarItem
        {
            Label = SelectedPeriod == "Azi" ? "Azi" : "Total",
            Count = total,
            BarHeight = barH
        });
    }

    private async Task LoadRecentProgramariAsync(DateTime from, DateTime to)
    {
        RecentProgramari.Clear();
        var list = await _app.ProgramariRepo.GetByMedicAsync(_medicId, from, to);
        foreach (var p in list.OrderByDescending(x => x.DataOra).Take(50))
        {
            RecentProgramari.Add(p);
        }
    }
}
