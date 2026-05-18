using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Context;
using Mediclin.UI.Models;

namespace Mediclin.UI.ViewModels.Doctor;

public class AppointmentsViewModel : BaseViewModel
{
    private readonly int _medicId;
    private int _year  = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;
    private HashSet<int> _daysWithAppts = new();

    // ── Collections ───────────────────────────────────────────
    private ObservableCollection<ProgramareDisplay> _dayAppts = new();
    private ObservableCollection<CalendarDayItem>   _calDays  = new();

    public ObservableCollection<ProgramareDisplay> DayAppts
    {
        get => _dayAppts;
        private set { _dayAppts = value; OnPropertyChanged(); }
    }

    public ObservableCollection<CalendarDayItem> CalDays
    {
        get => _calDays;
        private set { _calDays = value; OnPropertyChanged(); }
    }

    // ── State ─────────────────────────────────────────────────
    private DateTime? _selDate;
    private string _dayTitle    = "Selectează o zi din calendar";
    private string _error       = "";
    private bool   _loading     = false;

    public DateTime? SelDate
    {
        get => _selDate;
        set
        {
            if (SetProperty(ref _selDate, value))
            {
                RebuildCalendar();
                if (value.HasValue) _ = LoadDayAsync(value.Value);
            }
        }
    }

    public string DayTitle { get => _dayTitle; private set => SetProperty(ref _dayTitle, value); }
    public string Error    { get => _error;    private set => SetProperty(ref _error, value); }
    public bool   Loading  { get => _loading;  private set => SetProperty(ref _loading, value); }

    public string MonthYear =>
        new DateTime(_year, _month, 1)
            .ToString("MMMM yyyy", new System.Globalization.CultureInfo("ro-RO"));

    // ── Commands ──────────────────────────────────────────────
    public ICommand PrevMonthCmd { get; }
    public ICommand NextMonthCmd { get; }
    public ICommand SelectDayCmd { get; }
    public ICommand RefreshCmd   { get; }

    // ── Constructor ───────────────────────────────────────────
    public AppointmentsViewModel(int medicId, int medicUtilizatorId, string medicNume)
    {
        _medicId = medicId;

        PrevMonthCmd = new RelayCommand(_ => ShiftMonth(-1));
        NextMonthCmd = new RelayCommand(_ => ShiftMonth(+1));
        SelectDayCmd = new RelayCommand(p =>
        {
            if (p is CalendarDayItem d && d.IsCurrentMonth)
                SelDate = d.Date;
        });
        RefreshCmd = new AsyncRelayCommand(async _ =>
        {
            if (_selDate.HasValue) await LoadDayAsync(_selDate.Value);
        });
    }

    // ── Init ──────────────────────────────────────────────────
    public async Task LoadAsync()
    {
        try
        {
            // Set today as default selected date
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _selDate = DateTime.Today;
                OnPropertyChanged(nameof(SelDate));
                DayTitle = $"Programări — {DateTime.Today:dd.MM.yyyy}";
                RebuildCalendar();
            });

            await LoadDaysMarkedAsync();
            await LoadDayAsync(DateTime.Today);
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                Error = $"Eroare: {ex.Message}");
        }
    }

    // ── Private methods ───────────────────────────────────────
    private void ShiftMonth(int delta)
    {
        var d = new DateTime(_year, _month, 1).AddMonths(delta);
        _year  = d.Year;
        _month = d.Month;
        OnPropertyChanged(nameof(MonthYear));
        RebuildCalendar();
        _ = LoadDaysMarkedAsync();
    }

    private void RebuildCalendar()
    {
        var first  = new DateTime(_year, _month, 1);
        var offset = ((int)first.DayOfWeek + 6) % 7;
        var start  = first.AddDays(-offset);

        var days = Enumerable.Range(0, 42).Select(i =>
        {
            var d = start.AddDays(i);
            return new CalendarDayItem
            {
                Date            = d,
                IsCurrentMonth  = d.Month == _month,
                IsToday         = d.Date  == DateTime.Today,
                HasAppointments = _daysWithAppts.Contains(d.Day) && d.Month == _month,
                IsSelected      = d.Date  == _selDate?.Date
            };
        }).ToList();

        CalDays = new ObservableCollection<CalendarDayItem>(days);
    }

    private async Task LoadDaysMarkedAsync()
    {
        try
        {
            var db   = new DatabaseContext();
            var rows = await db.QueryAsync(@"
                SELECT DISTINCT DAY(data_ora) AS zi
                FROM   programari
                WHERE  medic_id = @mid
                AND    MONTH(data_ora) = @m
                AND    YEAR(data_ora)  = @y
                AND    status NOT IN ('Anulata','Neprezentata')",
                new Dictionary<string, object>
                {
                    ["@mid"] = _medicId,
                    ["@m"]   = _month,
                    ["@y"]   = _year
                });

            var marked = (rows ?? new()).Select(r => Convert.ToInt32(r["zi"])).ToHashSet();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _daysWithAppts = marked;
                RebuildCalendar();
            });
        }
        catch { /* non-critical */ }
    }

    private async Task LoadDayAsync(DateTime date)
    {
        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Loading  = true;
                Error    = "";
                DayTitle = $"Programări — {date:dd MMMM yyyy}";
            });

            var db   = new DatabaseContext();
            var rows = await db.QueryAsync(@"
                SELECT pr.id,
                       pr.data_ora,
                       pr.durata_min,
                       pr.tip,
                       pr.status,
                       pr.motiv_vizita,
                       CONCAT(up.prenume, ' ', up.nume) AS pnume
                FROM   programari  pr
                JOIN   pacienti    pa ON pa.id = pr.pacient_id
                JOIN   utilizatori up ON up.id = pa.utilizator_id
                WHERE  pr.medic_id       = @mid
                AND    DATE(pr.data_ora) = @d
                ORDER  BY pr.data_ora",
                new Dictionary<string, object>
                {
                    ["@mid"] = _medicId,
                    ["@d"]   = date.ToString("yyyy-MM-dd")
                });

            var list = (rows ?? new()).Select(r => new ProgramareDisplay
            {
                Id          = Convert.ToInt32(r["id"]),
                DataOra     = (DateTime)r["data_ora"],
                DurataMin   = Convert.ToInt32(r["durata_min"]),
                Tip         = r["tip"]?.ToString()          ?? "",
                Status      = r["status"]?.ToString()       ?? "",
                MotivVizita = r["motiv_vizita"]?.ToString() ?? "",
                PacientNume = r["pnume"]?.ToString()        ?? ""
            }).ToList();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                DayAppts = new ObservableCollection<ProgramareDisplay>(list);
                Loading  = false;
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Loading = false;
                Error   = $"Eroare la programări: {ex.Message}";
            });
        }
    }
}
