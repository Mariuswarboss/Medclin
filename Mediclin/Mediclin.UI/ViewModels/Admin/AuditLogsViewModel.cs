using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels;

namespace Mediclin.UI.ViewModels.Admin;

public class AuditLogsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private int _page = 1;
    private string? _severitate;
    private string _cautare = string.Empty;

    public AuditLogsViewModel(ApplicationServices app)
    {
        _app = app;
        Rows = new ObservableCollection<Dictionary<string, object>>();
        PreviousCommand = new RelayCommand(_ => ShiftPage(-1), _ => Page > 1);
        NextCommand = new RelayCommand(_ => ShiftPage(1), _ => Page < TotalPages);
        _ = LoadAsync();
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        t.Tick += async (_, _) => await LoadAsync();
        t.Start();
    }

    public ObservableCollection<Dictionary<string, object>> Rows { get; }

    public int Page
    {
        get => _page;
        set => SetProperty(ref _page, value);
    }

    public int TotalPages { get; private set; } = 1;

    public string? SeveritateFiltru
    {
        get => _severitate;
        set
        {
            if (SetProperty(ref _severitate, value))
            {
                Page = 1;
                _ = LoadAsync();
            }
        }
    }

    public string Cautare
    {
        get => _cautare;
        set => SetProperty(ref _cautare, value);
    }

    public RelayCommand PreviousCommand { get; }
    public RelayCommand NextCommand { get; }

    private void ShiftPage(int d)
    {
        Page = Math.Max(1, Page + d);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var (rows, total) = await _app.JurnalRepo.GetPagedAsync(Page, 50, SeveritateFiltru, Cautare);
            TotalPages = Math.Max(1, (int)Math.Ceiling(total / 50.0));
            OnPropertyChanged(nameof(TotalPages));
            Rows.Clear();
            foreach (var r in rows)
            {
                Rows.Add(r);
            }
        }
        catch
        {
            // ignorat
        }
    }
}
