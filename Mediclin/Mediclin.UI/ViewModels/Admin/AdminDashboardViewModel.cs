using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using Mediclin.Business.DTOs;
using Mediclin.Data.Context;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public sealed class AdminKpiItem
{
    public AdminKpiItem(string titlu, string valoare, string subtitlu)
    {
        Titlu = titlu;
        Valoare = valoare;
        Subtitlu = subtitlu;
    }

    public string Titlu { get; }
    public string Valoare { get; }
    public string Subtitlu { get; }
}

public sealed class AdminJournalLine
{
    public string Moment { get; init; } = string.Empty;
    public string Severitate { get; init; } = string.Empty;
    public string Utilizator { get; init; } = string.Empty;
    public string Actiune { get; init; } = string.Empty;
    public string Modul { get; init; } = string.Empty;
}

public class AdminDashboardViewModel : BaseViewModel
{
    private static readonly CultureInfo Ro = new("ro-RO");
    private readonly ApplicationServices _app;
    private readonly DispatcherTimer _timer;
    private string _sanatate = "Se încarcă…";
    private RaportDto _statistici = new();
    private int _mediciInAsteptare;

    public AdminDashboardViewModel(ApplicationServices app)
    {
        _app = app;
        KpiItems = new ObservableCollection<AdminKpiItem>();
        JournalLines = new ObservableCollection<AdminJournalLine>();
        _ = LoadAsync();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        _timer.Tick += async (_, _) => await LoadAsync();
        _timer.Start();
    }

    public string SanatateSistem
    {
        get => _sanatate;
        set => SetProperty(ref _sanatate, value);
    }

    public RaportDto Statistici
    {
        get => _statistici;
        set => SetProperty(ref _statistici, value);
    }

    public int MediciInAsteptare
    {
        get => _mediciInAsteptare;
        set => SetProperty(ref _mediciInAsteptare, value);
    }

    public ObservableCollection<AdminKpiItem> KpiItems { get; }
    public ObservableCollection<AdminJournalLine> JournalLines { get; }

    private async Task LoadAsync()
    {
        try
        {
            var ok = await ConnectionFactory.TestConnectionAsync();
            SanatateSistem = ok
                ? "Conexiunea la baza de date este activă. Monitorizare utilizatori, medici, programări și jurnal."
                : "Problemă la conexiunea cu baza de date.";

            Statistici = await _app.Rapoarte.GetStatisticiAdminAsync();
            var medici = await _app.Medici.GetAllAsync();
            MediciInAsteptare = medici.Count(m => !m.Verificat);

            KpiItems.Clear();
            KpiItems.Add(new AdminKpiItem("Utilizatori activi", Statistici.UtilizatoriActivi.ToString("N0", Ro), "conturi cu acces activ"));
            KpiItems.Add(new AdminKpiItem("Medici verificați", Statistici.MediciVerificati.ToString("N0", Ro), "profiluri medic aprobate"));
            KpiItems.Add(new AdminKpiItem("Programări astăzi", Statistici.ProgramariAstazi.ToString("N0", Ro), "în calendarul zilei curente"));
            KpiItems.Add(new AdminKpiItem("Medici în așteptare", MediciInAsteptare.ToString("N0", Ro), "necesită verificare în secțiunea Medici"));

            var rows = await _app.JurnalRepo.GetRecentAsync(12);
            JournalLines.Clear();
            foreach (var r in rows)
            {
                var creat = r.TryGetValue("creat_la", out var cl) && cl != DBNull.Value
                    ? Convert.ToDateTime(cl).ToString("dd.MM.yyyy HH:mm", Ro)
                    : "—";
                JournalLines.Add(new AdminJournalLine
                {
                    Moment = creat,
                    Severitate = r.TryGetValue("severitate", out var sev) && sev != DBNull.Value ? sev.ToString() ?? "—" : "—",
                    Utilizator = r.TryGetValue("utilizator_nume", out var un) && un != DBNull.Value ? un.ToString() ?? "—" : "—",
                    Actiune = r.TryGetValue("actiune", out var a) && a != DBNull.Value ? a.ToString() ?? "—" : "—",
                    Modul = r.TryGetValue("modul", out var m) && m != DBNull.Value ? m.ToString() ?? "—" : "—"
                });
            }
        }
        catch (Exception ex)
        {
            SanatateSistem = "Eroare la încărcare: " + ex.Message;
        }
    }
}
