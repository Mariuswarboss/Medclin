using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class PatientDashboardViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private Pacient? _pacient;
    private readonly List<Medic> _allDoctors = new();
    private Medic? _selectedMedic;
    private Specialitate? _selectedSpecialitate;
    private DateTime? _selectedDate = DateTime.Today;
    private DateTime? _selectedSlot;
    private string _motivVizita = string.Empty;
    private string _selectedTip = "Control";
    private string _nextAppointmentText = "Nu există programări viitoare";
    private int _nextAppointmentDays;
    private int _activePrescriptions;
    private int _newLabResults;
    private bool _isBookingPanelOpen;
    private bool _isSlotsLoading;
    private string _bookingError = string.Empty;
    private string _bookingSuccess = string.Empty;

    public PatientDashboardViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        UpcomingAppointments = new ObservableCollection<Programare>();
        AvailableDoctors = new ObservableCollection<Medic>();
        Specialitati = new ObservableCollection<Specialitate>();
        AvailableSlots = new ObservableCollection<DateTime>();
        TipuriConsultatie = new ObservableCollection<string> { "Initiala", "Control", "Urgenta", "Telemedicina" };

        OpenBookingCommand = new RelayCommand(_ => OpenBooking());
        FilterDoctorsBySpecialtyCommand = new RelayCommand(FilterDoctorsBySpecialty);
        SelectDoctorCommand = new AsyncRelayCommand(async p => await SelectDoctorAsync(p as Medic));
        LoadSlotsCommand = new AsyncRelayCommand(async _ => await LoadSlotsAsync());
        SelectSlotCommand = new RelayCommand(p => SelectSlot(p));
        ConfirmBookingCommand = new AsyncRelayCommand(async _ => await ConfirmBookingAsync(), _ => !IsSlotsLoading);
        CancelBookingCommand = new RelayCommand(_ => CancelBooking());

        _ = LoadAsync();
    }

    public string WelcomeText => $"Bună ziua, {_utilizator.Prenume}!";
    public string TodayDate => DateTime.Now.ToString("dddd, dd MMMM yyyy", new CultureInfo("ro-RO"));
    public ObservableCollection<Programare> UpcomingAppointments { get; }
    public ObservableCollection<Medic> AvailableDoctors { get; private set; }
    public ObservableCollection<Specialitate> Specialitati { get; }
    public ObservableCollection<DateTime> AvailableSlots { get; private set; }
    public ObservableCollection<string> TipuriConsultatie { get; }

    public Medic? SelectedMedic
    {
        get => _selectedMedic;
        set
        {
            if (SetProperty(ref _selectedMedic, value))
            {
                SelectedSlot = null;
                _ = LoadSlotsAsync();
            }
        }
    }

    public Specialitate? SelectedSpecialitate
    {
        get => _selectedSpecialitate;
        set => SetProperty(ref _selectedSpecialitate, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                SelectedSlot = null;
                _ = LoadSlotsAsync();
            }
        }
    }

    public DateTime? SelectedSlot { get => _selectedSlot; set => SetProperty(ref _selectedSlot, value); }
    public string MotivVizita { get => _motivVizita; set => SetProperty(ref _motivVizita, value); }
    public string SelectedTip { get => _selectedTip; set => SetProperty(ref _selectedTip, value); }
    public int NextAppointmentDays { get => _nextAppointmentDays; private set => SetProperty(ref _nextAppointmentDays, value); }
    public string NextAppointmentText { get => _nextAppointmentText; private set => SetProperty(ref _nextAppointmentText, value); }
    public int ActivePrescriptions { get => _activePrescriptions; private set => SetProperty(ref _activePrescriptions, value); }
    public int NewLabResults { get => _newLabResults; private set => SetProperty(ref _newLabResults, value); }
    public bool IsBookingPanelOpen { get => _isBookingPanelOpen; set => SetProperty(ref _isBookingPanelOpen, value); }
    public bool IsSlotsLoading { get => _isSlotsLoading; set => SetProperty(ref _isSlotsLoading, value); }
    public string BookingError { get => _bookingError; set => SetProperty(ref _bookingError, value); }
    public string BookingSuccess { get => _bookingSuccess; set => SetProperty(ref _bookingSuccess, value); }

    public ICommand OpenBookingCommand { get; }
    public ICommand FilterDoctorsBySpecialtyCommand { get; }
    public ICommand SelectDoctorCommand { get; }
    public ICommand LoadSlotsCommand { get; }
    public ICommand SelectSlotCommand { get; }
    public ICommand ConfirmBookingCommand { get; }
    public ICommand CancelBookingCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            BookingError = string.Empty;
            _pacient = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (_pacient is null)
            {
                BookingError = "Pacient negăsit";
                return;
            }

            var programari = await _app.ProgramariRepo.GetByPacientAsync(_pacient.Id);
            var viitoare = programari
                .Where(p => p.DataOra >= DateTime.Now && !new[] { "Anulata", "Finalizata", "Neprezentata" }.Contains(p.Status))
                .OrderBy(p => p.DataOra)
                .Take(5)
                .ToList();

            UpcomingAppointments.Clear();
            foreach (var p in viitoare)
            {
                UpcomingAppointments.Add(p);
            }

            var next = viitoare.FirstOrDefault();
            if (next is not null)
            {
                NextAppointmentDays = Math.Max(0, (next.DataOra.Date - DateTime.Today).Days);
                NextAppointmentText = $"{next.DataOra:dd MMMM, HH:mm} · {next.MedicNume ?? "Medic"}";
            }
            else
            {
                NextAppointmentDays = 0;
                NextAppointmentText = "Nu există programări viitoare";
            }

            ActivePrescriptions = (await _app.ReteteRepo.GetActiveAsync(_pacient.Id)).Count;
            NewLabResults = (await _app.AnalizeRepo.GetByPacientAsync(_pacient.Id))
                .Count(a => a.CreatLa >= DateTime.Now.AddDays(-30));

            Specialitati.Clear();
            foreach (var s in await _app.Specialitati.GetAllAsync())
            {
                Specialitati.Add(s);
            }

            _allDoctors.Clear();
            _allDoctors.AddRange((await _app.Medici.GetAllAsync()).Where(m => m.Verificat));
            ApplyDoctorFilter();
        }
        catch (Exception ex)
        {
            BookingError = $"Eroare la încărcarea dashboardului: {ex.Message}";
            await _app.JurnalRepo.LogAsync("LOAD_PATIENT_DASHBOARD_ERROR", "PatientDashboard", ex.Message, "Eroare", _utilizator.Id);
        }
    }

    private void OpenBooking()
    {
        BookingError = string.Empty;
        BookingSuccess = string.Empty;
        IsBookingPanelOpen = true;
    }

    private void FilterDoctorsBySpecialty(object? parameter)
    {
        SelectedSpecialitate = parameter as Specialitate;
        ApplyDoctorFilter();
    }

    private void ApplyDoctorFilter()
    {
        var doctors = SelectedSpecialitate is null
            ? _allDoctors
            : _allDoctors.Where(m => m.SpecialitateId == SelectedSpecialitate.Id);

        AvailableDoctors = new ObservableCollection<Medic>(doctors.OrderBy(m => m.SpecialitateNume).ThenBy(m => m.Nume));
        OnPropertyChanged(nameof(AvailableDoctors));
    }

    private async Task SelectDoctorAsync(Medic? medic)
    {
        if (medic is null) return;
        SelectedMedic = medic;
        await LoadSlotsAsync();
    }

    private async Task LoadSlotsAsync()
    {
        if (SelectedMedic is null || SelectedDate is null)
        {
            AvailableSlots = new ObservableCollection<DateTime>();
            OnPropertyChanged(nameof(AvailableSlots));
            return;
        }

        try
        {
            IsSlotsLoading = true;
            var slots = await _app.Programari.GetSloturiDisponibileAsync(SelectedMedic.Id, SelectedDate.Value);
            AvailableSlots = new ObservableCollection<DateTime>(slots);
            OnPropertyChanged(nameof(AvailableSlots));
        }
        catch (Exception ex)
        {
            BookingError = $"Nu s-au putut încărca orele disponibile: {ex.Message}";
            await _app.JurnalRepo.LogAsync("LOAD_SLOTS_ERROR", "PatientDashboard", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsSlotsLoading = false;
        }
    }

    private void SelectSlot(object? parameter)
    {
        if (parameter is DateTime slot)
        {
            SelectedSlot = slot;
        }
    }

    private async Task ConfirmBookingAsync()
    {
        if (_pacient is null || SelectedMedic is null || SelectedDate is null || SelectedSlot is null)
        {
            BookingError = "Selectați medicul, data și ora";
            return;
        }

        try
        {
            BookingError = string.Empty;
            var (ok, error) = await _app.Programari.CreateProgramareAsync(
                _pacient.Id,
                SelectedMedic.Id,
                SelectedSlot.Value,
                SelectedTip,
                MotivVizita ?? string.Empty);
            if (!ok)
            {
                BookingError = error;
                return;
            }

            BookingSuccess = "Programare confirmată!";
            IsBookingPanelOpen = false;
            MotivVizita = string.Empty;
            SelectedSlot = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            BookingError = $"Programarea nu a putut fi salvată: {ex.Message}";
            await _app.JurnalRepo.LogAsync("CONFIRM_BOOKING_ERROR", "PatientDashboard", ex.Message, "Eroare", _utilizator.Id);
        }
    }

    private void CancelBooking()
    {
        BookingError = string.Empty;
        BookingSuccess = string.Empty;
        IsBookingPanelOpen = false;
    }
}
