using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Doctor;

public class AppointmentsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private readonly int _medicIdDefault;
    private readonly Func<Task>? _onParentRefresh;
    private DateTime _selectedDate = DateTime.Today;
    private int _selectedYear;
    private int _selectedMonth;
    private ObservableCollection<SlotItem> _availableSlots = new();
    private SlotItem? _selectedSlot;
    private Pacient? _selectedPacient;
    private Medic? _selectedMedic;
    private string _selectedTip = "Initiala";
    private string _motivVizita = string.Empty;
    private string _errorMessage = string.Empty;
    private ObservableCollection<Programare> _selectedDateAppointments = new();

    public AppointmentsViewModel(ApplicationServices app, Utilizator utilizator, int medicIdDefault, Func<Task>? onParentRefresh = null)
    {
        _app = app;
        _utilizator = utilizator;
        _medicIdDefault = medicIdDefault;
        _onParentRefresh = onParentRefresh;
        _selectedYear = _selectedDate.Year;
        _selectedMonth = _selectedDate.Month;

        AllPacients = new ObservableCollection<Pacient>();
        AllMedici = new ObservableCollection<Medic>();
        CalendarDays = new ObservableCollection<CalendarDayItem>();
        DaysWithAppointments = new List<int>();
        TipuriConsultatie = new List<string> { "Initiala", "Control", "Urgenta", "Telemedicina" };

        PreviousMonthCommand = new RelayCommand(_ => ShiftMonth(-1));
        NextMonthCommand = new RelayCommand(_ => ShiftMonth(1));
        SelectDayCommand = new RelayCommand(p =>
        {
            if (p is CalendarDayItem d)
            {
                SelectedDate = d.Date;
                _ = ReloadDayAsync();
            }
        });
        SelectSlotCommand = new RelayCommand(p =>
        {
            if (p is SlotItem s)
            {
                SelectedSlot = s;
            }
        });
        CreateProgramareCommand = new RelayCommand(_ => _ = CreateAsync());

        _ = InitAsync();
    }

    public ObservableCollection<Pacient> AllPacients { get; }
    public ObservableCollection<Medic> AllMedici { get; }
    public ObservableCollection<CalendarDayItem> CalendarDays { get; }
    public List<int> DaysWithAppointments { get; private set; }
    public List<string> TipuriConsultatie { get; }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                _selectedYear = value.Year;
                _selectedMonth = value.Month;
                OnPropertyChanged(nameof(MonthYearText));
                _ = ReloadDayAsync();
                _ = RebuildCalendarAsync();
            }
        }
    }

    public string MonthYearText => new DateTime(_selectedYear, _selectedMonth, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("ro-RO"));

    public ObservableCollection<Programare> SelectedDateAppointments
    {
        get => _selectedDateAppointments;
        set => SetProperty(ref _selectedDateAppointments, value);
    }

    public ObservableCollection<SlotItem> AvailableSlots
    {
        get => _availableSlots;
        set => SetProperty(ref _availableSlots, value);
    }

    public SlotItem? SelectedSlot
    {
        get => _selectedSlot;
        set => SetProperty(ref _selectedSlot, value);
    }

    public Pacient? SelectedPacient
    {
        get => _selectedPacient;
        set => SetProperty(ref _selectedPacient, value);
    }

    public Medic? SelectedMedic
    {
        get => _selectedMedic;
        set
        {
            if (SetProperty(ref _selectedMedic, value))
            {
                _ = ReloadSlotsAsync();
            }
        }
    }

    public string SelectedTip
    {
        get => _selectedTip;
        set => SetProperty(ref _selectedTip, value);
    }

    public string MotiVizita
    {
        get => _motivVizita;
        set => SetProperty(ref _motivVizita, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand SelectDayCommand { get; }
    public ICommand SelectSlotCommand { get; }
    public ICommand CreateProgramareCommand { get; }

    private async Task InitAsync()
    {
        try
        {
            foreach (var p in await _app.Pacienti.GetAllAsync())
            {
                AllPacients.Add(p);
            }

            foreach (var m in await _app.Medici.GetAllAsync())
            {
                AllMedici.Add(m);
            }

            SelectedMedic = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id)
                ?? AllMedici.FirstOrDefault();
            await RebuildCalendarAsync();
            await ReloadDayAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task RebuildCalendarAsync()
    {
        try
        {
            var mid = SelectedMedic?.Id ?? _medicIdDefault;
            if (mid <= 0)
            {
                return;
            }

            DaysWithAppointments = await _app.ProgramariRepo.GetDistinctDaysWithAppointmentsAsync(mid, _selectedYear, _selectedMonth);
            var first = new DateTime(_selectedYear, _selectedMonth, 1);
            var startOffset = ((int)first.DayOfWeek + 6) % 7;
            var gridStart = first.AddDays(-startOffset);
            CalendarDays.Clear();
            for (var i = 0; i < 42; i++)
            {
                var d = gridStart.AddDays(i);
                CalendarDays.Add(new CalendarDayItem
                {
                    Date = d,
                    IsCurrentMonth = d.Month == _selectedMonth,
                    IsToday = d.Date == DateTime.Today,
                    HasAppointments = DaysWithAppointments.Contains(d.Day) && d.Month == _selectedMonth,
                    IsSelected = d.Date == SelectedDate.Date
                });
            }
        }
        catch
        {
            // ignorat
        }
    }

    private void ShiftMonth(int delta)
    {
        var d = new DateTime(_selectedYear, _selectedMonth, 1).AddMonths(delta);
        _selectedYear = d.Year;
        _selectedMonth = d.Month;
        OnPropertyChanged(nameof(MonthYearText));
        _ = RebuildCalendarAsync();
    }

    private async Task ReloadDayAsync()
    {
        try
        {
            var mid = SelectedMedic?.Id ?? _medicIdDefault;
            if (mid <= 0)
            {
                return;
            }

            var list = await _app.ProgramariRepo.GetByMedicDayAsync(mid, SelectedDate);
            SelectedDateAppointments = new ObservableCollection<Programare>(list);
            await ReloadSlotsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task ReloadSlotsAsync()
    {
        try
        {
            var mid = SelectedMedic?.Id ?? _medicIdDefault;
            if (mid <= 0)
            {
                return;
            }

            var slots = await _app.Programari.GetSloturiDisponibileAsync(mid, SelectedDate);
            var col = new ObservableCollection<SlotItem>();
            foreach (var s in slots)
            {
                col.Add(new SlotItem(s));
            }

            AvailableSlots = col;
        }
        catch
        {
            AvailableSlots = new ObservableCollection<SlotItem>();
        }
    }

    private async Task CreateAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (SelectedPacient is null)
            {
                ErrorMessage = "Selectați un pacient.";
                return;
            }

            var mid = SelectedMedic?.Id ?? _medicIdDefault;
            if (mid <= 0)
            {
                ErrorMessage = "Medic invalid.";
                return;
            }

            if (SelectedSlot is null)
            {
                ErrorMessage = "Selectați un interval orar.";
                return;
            }

            var (ok, err) = await _app.Programari.CreateProgramareAsync(
                SelectedPacient.Id,
                mid,
                SelectedSlot.At,
                SelectedTip,
                MotiVizita);
            if (!ok)
            {
                ErrorMessage = err;
                return;
            }

            MessageBox.Show("Programarea a fost creată.", "MediClin", MessageBoxButton.OK, MessageBoxImage.Information);
            await ReloadDayAsync();
            await RebuildCalendarAsync();
            if (_onParentRefresh is not null)
            {
                await _onParentRefresh();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
