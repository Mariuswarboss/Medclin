using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels;

namespace Mediclin.UI.ViewModels.Admin;

public sealed class AuditLogRow
{
    public string Moment { get; init; } = string.Empty;
    public string Severitate { get; init; } = string.Empty;
    public string Utilizator { get; init; } = string.Empty;
    public string Actiune { get; init; } = string.Empty;
    public string Modul { get; init; } = string.Empty;
    public string Ip { get; init; } = string.Empty;
}

public class AuditLogsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private string? _severitate;
    private string _cautare = string.Empty;

    public AuditLogsViewModel(ApplicationServices app)
    {
        _app = app;
        Rows = new ObservableCollection<AuditLogRow>();
        SetSeverityCommand = new RelayCommand(p => SetSeverity(p as string));
        _ = LoadAsync();
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        t.Tick += async (_, _) => await LoadAsync();
        t.Start();
    }

    public ObservableCollection<AuditLogRow> Rows { get; }

    public string? SeveritateFiltru
    {
        get => _severitate;
        set
        {
            if (SetProperty(ref _severitate, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public string Cautare
    {
        get => _cautare;
        set
        {
            if (SetProperty(ref _cautare, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public RelayCommand SetSeverityCommand { get; }

    private void SetSeverity(string? severity)
    {
        SeveritateFiltru = severity;
    }

    private async Task LoadAsync()
    {
        try
        {
            var rows = await _app.JurnalRepo.GetRecentAsync(100);
            var filtered = rows.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SeveritateFiltru))
            {
                filtered = filtered.Where(r => Read(r, "severitate").Equals(SeveritateFiltru, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(Cautare))
            {
                var q = Cautare.Trim();
                filtered = filtered.Where(r =>
                    Read(r, "actiune").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    Read(r, "modul").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    Read(r, "utilizator_nume").Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            Rows.Clear();
            foreach (var r in filtered)
            {
                Rows.Add(new AuditLogRow
                {
                    Moment = ReadDate(r, "creat_la"),
                    Severitate = Read(r, "severitate"),
                    Utilizator = Read(r, "utilizator_nume"),
                    Actiune = Read(r, "actiune"),
                    Modul = Read(r, "modul"),
                    Ip = Read(r, "ip_adresa")
                });
            }
        }
        catch
        {
            // ignorat
        }
    }

    private static string Read(Dictionary<string, object> row, string key)
    {
        return row.TryGetValue(key, out var value) && value != DBNull.Value ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static string ReadDate(Dictionary<string, object> row, string key)
    {
        return row.TryGetValue(key, out var value) && value != DBNull.Value && DateTime.TryParse(value.ToString(), out var date)
            ? date.ToString("dd.MM.yyyy HH:mm")
            : string.Empty;
    }
}
