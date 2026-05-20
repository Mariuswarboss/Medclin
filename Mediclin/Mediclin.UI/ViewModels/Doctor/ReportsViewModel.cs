using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;
using Mediclin.Business.DTOs;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Microsoft.Win32;

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
    private int _cardConsultatiiDirecte;
    private decimal _cardVenit;
    private DateTime _periodStart = DateTime.Today;
    private DateTime _periodEnd = DateTime.Today.AddDays(1).AddTicks(-1);

    public ReportsViewModel(ApplicationServices app, int medicId)
    {
        _app = app;
        _medicId = medicId;
        Perioduri = new List<string> { "Azi", "Saptamana", "Luna" };
        ChartBars = new ObservableCollection<RaportBarItem>();
        RecentProgramari = new ObservableCollection<Programare>();
        LoadRaportCommand = new RelayCommand(_ => _ = LoadAsync());
        ExportPdfCommand = new AsyncRelayCommand(_ => ExportPdfAsync());
        ExportCsvCommand = new AsyncRelayCommand(_ => ExportCsvAsync());
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

    public int CardConsultatiiDirecte
    {
        get => _cardConsultatiiDirecte;
        set => SetProperty(ref _cardConsultatiiDirecte, value);
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

            _periodStart = from;
            _periodEnd = to;
            CardTotal = CurrentRaport.TotalProgramari > 0 ? CurrentRaport.TotalProgramari : CurrentRaport.TotalConsultatii;
            CardFinalizate = CurrentRaport.ProgramariFinalizate > 0 ? CurrentRaport.ProgramariFinalizate : CurrentRaport.TotalConsultatii;
            CardNeprezentate = CurrentRaport.ProgramariNeprezentate;
            CardVenit = CurrentRaport.VenitEstimat;
            CardConsultatiiDirecte = Math.Max(0, CurrentRaport.TotalConsultatii - CurrentRaport.ProgramariFinalizate);

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

    private async Task ExportPdfAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Salveaza raportul financiar",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"MediClin_Raport_Financiar_{SelectedPeriod}_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
            AddExtension = true,
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await SimplePdfExportService.SaveTextPdfAsync(dialog.FileName, BuildExportLines());
            StatusMessage = $"PDF salvat: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export PDF esuat: {ex.Message}";
        }
    }

    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Salveaza raportul CSV",
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"MediClin_Raport_Financiar_{SelectedPeriod}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, BuildCsv(), new UTF8Encoding(true));
            StatusMessage = $"CSV salvat: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export CSV esuat: {ex.Message}";
        }
    }

    private List<string> BuildExportLines()
    {
        var lines = new List<string>
        {
            "MediClin - Raport financiar medic",
            $"Perioada: {SelectedPeriod} ({_periodStart:dd.MM.yyyy} - {_periodEnd:dd.MM.yyyy})",
            $"Generat la: {DateTime.Now:dd.MM.yyyy HH:mm}",
            string.Empty,
            $"Total programari: {CardTotal}",
            $"Programari finalizate: {CardFinalizate}",
            $"Fise salvate direct: {CardConsultatiiDirecte}",
            $"Venit estimat: {CardVenit:N2} lei",
            $"Neprezentari: {CardNeprezentate}",
            $"Rata neprezentare: {CurrentRaport.RataNeprezentare:N2}%",
            string.Empty,
            "Activitate pe zile:"
        };

        foreach (var item in ChartBars)
        {
            lines.Add($"- {item.Label}: {item.Count} consultatii");
        }

        lines.Add(string.Empty);
        lines.Add("Programari recente:");
        foreach (var programare in RecentProgramari)
        {
            SimplePdfExportService.AddWrapped(
                lines,
                $"- {programare.DataOra:dd.MM.yyyy HH:mm} | {programare.PacientNume ?? "Pacient"} | {programare.Tip} | {programare.Status}");
        }

        return lines;
    }

    private string BuildCsv()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Sectiune,Indicator,Valoare");
        csv.AppendLine($"Raport,Perioada,{EscapeCsv(SelectedPeriod)}");
        csv.AppendLine($"Raport,Start,{_periodStart:yyyy-MM-dd}");
        csv.AppendLine($"Raport,Sfarsit,{_periodEnd:yyyy-MM-dd}");
        csv.AppendLine($"Metrici,Total programari,{CardTotal}");
        csv.AppendLine($"Metrici,Programari finalizate,{CardFinalizate}");
        csv.AppendLine($"Metrici,Fise salvate direct,{CardConsultatiiDirecte}");
        csv.AppendLine($"Metrici,Venit estimat,{CardVenit.ToString(CultureInfo.InvariantCulture)}");
        csv.AppendLine($"Metrici,Neprezentari,{CardNeprezentate}");
        csv.AppendLine($"Metrici,Rata neprezentare,{CurrentRaport.RataNeprezentare.ToString(CultureInfo.InvariantCulture)}");
        csv.AppendLine();
        csv.AppendLine("Data,Pacient,Tip,Status,Durata minute,Motiv");

        foreach (var programare in RecentProgramari)
        {
            csv.AppendLine(string.Join(',',
                programare.DataOra.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                EscapeCsv(programare.PacientNume ?? "Pacient"),
                EscapeCsv(programare.Tip),
                EscapeCsv(programare.Status),
                programare.DurataMin,
                EscapeCsv(programare.MotivVizita ?? string.Empty)));
        }

        return csv.ToString();
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

    private static string EscapeCsv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
