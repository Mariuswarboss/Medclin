using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Dialogs;
using Mediclin.UI.Views.Dialogs;
using System.Linq;

namespace Mediclin.UI.ViewModels.Doctor;

public class EMRViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private int _medicId;
    private int _toastVersion;
    private Pacient? _currentPacient;
    private Consultatie? _currentConsultatie;
    private DiagnosisOption? _selectedDiagnosisOption;
    private string _simptome = string.Empty;
    private string _diagnosticCod = string.Empty;
    private string _diagnosticText = string.Empty;
    private string _recomandari = string.Empty;
    private string _notePrivate = string.Empty;
    private string _tensiune = string.Empty;
    private string _puls = string.Empty;
    private string _temperatura = string.Empty;
    private string _greutate = string.Empty;
    private string _inaltime = string.Empty;
    private bool _isSaving;
    private bool _showSaveToast;
    private bool _isSaveToastSuccess;
    private string _saveToastMessage = string.Empty;
    private string _saveToastIcon = "✓";
    private bool _isLoading;

    public EMRViewModel(ApplicationServices app, Utilizator utilizator, Pacient? pacient, int medicId = 0)
    {
        _app = app;
        _utilizator = utilizator;
        _currentPacient = pacient;
        _medicId = medicId;

        IstoricConsltatii = new ObservableCollection<Consultatie>();
        Alergii = new ObservableCollection<Alergie>();
        BoliCronice = new ObservableCollection<BolaCronica>();
        RetetActive = new ObservableCollection<Reteta>();
        DiagnosisOptions = new ObservableCollection<DiagnosisOption>
        {
            new("I10", "Hipertensiune esențială"),
            new("J06.9", "Infecție acută căi respiratorii superioare"),
            new("E11.9", "Diabet zaharat tip 2 fără complicații"),
            new("M54.5", "Lombalgie"),
            new("R51", "Cefalee"),
            new("K21.9", "Boală de reflux gastro-esofagian")
        };

        SaveConsultatieCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => CurrentPacient is not null && _medicId > 0 && !IsSaving);
        EmiteRetetaCommand = new AsyncRelayCommand(async _ => await EmiteAsync(), _ => CurrentPacient is not null && CurrentConsultatie is not null && CurrentConsultatie.Id > 0 && !IsSaving);
        LoadPatientCommand = new AsyncRelayCommand(async _ => await ReloadAsync());

        _ = InitializeAsync();
    }

    public Pacient? CurrentPacient
    {
        get => _currentPacient;
        set
        {
            if (SetProperty(ref _currentPacient, value))
            {
                OnPropertyChanged(nameof(HasPatientSelected));
                OnPropertyChanged(nameof(CurrentPatientInitials));
                OnPropertyChanged(nameof(CurrentPatientMeta));
            }
        }
    }

    public ObservableCollection<Consultatie> IstoricConsltatii { get; }
    public ObservableCollection<Alergie> Alergii { get; }
    public ObservableCollection<BolaCronica> BoliCronice { get; }
    public ObservableCollection<Reteta> RetetActive { get; }
    public ObservableCollection<DiagnosisOption> DiagnosisOptions { get; }

    public Consultatie? CurrentConsultatie
    {
        get => _currentConsultatie;
        set => SetProperty(ref _currentConsultatie, value);
    }

    public DiagnosisOption? SelectedDiagnosisOption
    {
        get => _selectedDiagnosisOption;
        set
        {
            if (SetProperty(ref _selectedDiagnosisOption, value) && value is not null)
            {
                DiagnosticCod = value.Code;
                if (string.IsNullOrWhiteSpace(DiagnosticText))
                {
                    DiagnosticText = value.Label;
                }
            }
        }
    }

    public string Simptome
    {
        get => _simptome;
        set => SetProperty(ref _simptome, value);
    }

    public string DiagnosticCod
    {
        get => _diagnosticCod;
        set => SetProperty(ref _diagnosticCod, value);
    }

    public string DiagnosticText
    {
        get => _diagnosticText;
        set => SetProperty(ref _diagnosticText, value);
    }

    public string Recomandari
    {
        get => _recomandari;
        set => SetProperty(ref _recomandari, value);
    }

    public string NotePrvate
    {
        get => _notePrivate;
        set => SetProperty(ref _notePrivate, value);
    }

    public string Tensiune
    {
        get => _tensiune;
        set => SetProperty(ref _tensiune, value);
    }

    public string Puls
    {
        get => _puls;
        set => SetProperty(ref _puls, value);
    }

    public string Temperatura
    {
        get => _temperatura;
        set => SetProperty(ref _temperatura, value);
    }

    public string Greutate
    {
        get => _greutate;
        set => SetProperty(ref _greutate, value);
    }

    public string Inaltime
    {
        get => _inaltime;
        set => SetProperty(ref _inaltime, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool ShowSaveToast
    {
        get => _showSaveToast;
        set => SetProperty(ref _showSaveToast, value);
    }

    public bool IsSaveToastSuccess
    {
        get => _isSaveToastSuccess;
        set => SetProperty(ref _isSaveToastSuccess, value);
    }

    public string SaveToastMessage
    {
        get => _saveToastMessage;
        set => SetProperty(ref _saveToastMessage, value);
    }

    public string SaveToastIcon
    {
        get => _saveToastIcon;
        set => SetProperty(ref _saveToastIcon, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasPatientSelected => CurrentPacient is not null;
    public string CurrentPatientInitials => string.Concat((CurrentPacient?.NumeComplet ?? "P").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();
    public string CurrentPatientMeta
    {
        get
        {
            if (CurrentPacient is null)
            {
                return "Selectați un pacient pentru a începe consultația.";
            }

            var details = new List<string>();
            if (CurrentPacient.GrupaSanguina is not null)
            {
                details.Add($"Grupa {CurrentPacient.GrupaSanguina}");
            }

            if (CurrentPacient.Sex is not null)
            {
                details.Add(CurrentPacient.Sex);
            }

            if (CurrentPacient.DataNasterii.HasValue)
            {
                details.Add($"{Math.Max(0, DateTime.Today.Year - CurrentPacient.DataNasterii.Value.Year)} ani");
            }

            return string.Join(" · ", details);
        }
    }

    public ICommand SaveConsultatieCommand { get; }
    public ICommand EmiteRetetaCommand { get; }
    public ICommand LoadPatientCommand { get; }

    private async Task InitializeAsync()
    {
        if (_medicId <= 0)
        {
            _medicId = (await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id))?.Id ?? 0;
        }

        await ReloadAsync();
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task ReloadAsync()
    {
        if (CurrentPacient is null || _medicId <= 0)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var patient = await _app.Pacienti.GetByIdAsync(CurrentPacient.Id) ?? CurrentPacient;
            CurrentPacient = patient;

            Alergii.Clear();
            foreach (var allergy in await _app.Pacienti.GetAlergiiAsync(patient.Id))
            {
                Alergii.Add(allergy);
            }

            BoliCronice.Clear();
            foreach (var condition in await _app.Pacienti.GetBolicroniceAsync(patient.Id))
            {
                BoliCronice.Add(condition);
            }

            IstoricConsltatii.Clear();
            foreach (var consultatie in await _app.Consultatii.GetIstoricAsync(patient.Id))
            {
                IstoricConsltatii.Add(consultatie);
            }

            RetetActive.Clear();
            foreach (var prescription in await _app.ReteteRepo.GetActiveAsync(patient.Id))
            {
                RetetActive.Add(prescription);
            }

            var appointmentsToday = await _app.ProgramariRepo.GetByMedicDayAsync(_medicId, DateTime.Today);
            var activeAppointment = appointmentsToday.FirstOrDefault(x => x.PacientId == patient.Id && x.Status is "In_cabinet" or "In_asteptare" or "Programata");
            if (activeAppointment is not null)
            {
                CurrentConsultatie = await _app.ConsultatiiRepo.GetByProgramareAsync(activeAppointment.Id)
                    ?? await _app.Consultatii.StartConsultatieAsync(activeAppointment.Id, patient.Id, _medicId);
            }
            else
            {
                CurrentConsultatie = IstoricConsltatii.FirstOrDefault();
            }

            if (CurrentConsultatie is not null)
            {
                BindConsultatie(CurrentConsultatie);
            }
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Save Failed: {ex.Message}");
            await _app.JurnalRepo.LogAsync("LOAD_DOCTOR_EMR_ERROR", "DoctorEMR", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BindConsultatie(Consultatie consultatie)
    {
        Simptome = consultatie.Simptome ?? string.Empty;
        DiagnosticCod = consultatie.DiagnosticCod ?? string.Empty;
        DiagnosticText = consultatie.DiagnosticText ?? string.Empty;
        Recomandari = consultatie.Recomandari ?? string.Empty;
        NotePrvate = consultatie.NotePrivate ?? string.Empty;
        Tensiune = consultatie.TensiuneArteriala ?? string.Empty;
        Puls = consultatie.Puls?.ToString() ?? string.Empty;
        Temperatura = consultatie.Temperatura?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        Greutate = consultatie.Greutate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        Inaltime = consultatie.Inaltime?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        SelectedDiagnosisOption = DiagnosisOptions.FirstOrDefault(option => option.Code == consultatie.DiagnosticCod);
    }

    private async Task SaveAsync()
    {
        if (CurrentPacient is null || CurrentConsultatie is null)
        {
            return;
        }

        try
        {
            IsSaving = true;
            CurrentConsultatie.Simptome = Simptome;
            CurrentConsultatie.DiagnosticCod = DiagnosticCod;
            CurrentConsultatie.DiagnosticText = DiagnosticText;
            CurrentConsultatie.Recomandari = Recomandari;
            CurrentConsultatie.NotePrivate = NotePrvate;
            CurrentConsultatie.TensiuneArteriala = string.IsNullOrWhiteSpace(Tensiune) ? null : Tensiune;
            CurrentConsultatie.Puls = int.TryParse(Puls, out var pulse) ? pulse : null;
            CurrentConsultatie.Temperatura = decimal.TryParse(Temperatura, NumberStyles.Any, CultureInfo.InvariantCulture, out var temperature) ? temperature : null;
            CurrentConsultatie.Greutate = decimal.TryParse(Greutate, NumberStyles.Any, CultureInfo.InvariantCulture, out var weight) ? weight : null;
            CurrentConsultatie.Inaltime = decimal.TryParse(Inaltime, NumberStyles.Any, CultureInfo.InvariantCulture, out var height) ? height : null;

            var (ok, error) = await _app.Consultatii.SaveConsultatieAsync(CurrentConsultatie);
            if (!ok)
            {
                await ShowToastAsync(false, $"Save Failed: {error}");
                return;
            }

            await ShowToastAsync(true, "Record Saved Successfully");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Save Failed: {ex.Message}");
            await _app.JurnalRepo.LogAsync("SAVE_DOCTOR_EMR_ERROR", "DoctorEMR", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsSaving = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task EmiteAsync()
    {
        if (CurrentPacient is null || CurrentConsultatie is null || CurrentConsultatie.Id <= 0)
        {
            return;
        }

        var dialog = new PrescriptionDialog
        {
            Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow
        };
        var viewModel = new PrescriptionDialogViewModel(dialog);
        dialog.DataContext = viewModel;
        if (dialog.ShowDialog() == true && viewModel.Medicamente.Count > 0)
        {
            var (ok, error, _) = await _app.Retete.EmiteRetetaAsync(
                CurrentConsultatie.Id,
                CurrentPacient.Id,
                _medicId,
                viewModel.Medicamente.ToList(),
                viewModel.Observatii);

            if (!ok)
            {
                await ShowToastAsync(false, $"Save Failed: {error}");
                return;
            }

            await ShowToastAsync(true, "Rețeta a fost emisă");
            await ReloadAsync();
        }
    }

    private async Task ShowToastAsync(bool success, string message)
    {
        var version = ++_toastVersion;
        IsSaveToastSuccess = success;
        SaveToastMessage = message;
        SaveToastIcon = success ? "✓" : "!";
        ShowSaveToast = true;

        await Task.Delay(2800);
        if (version == _toastVersion)
        {
            ShowSaveToast = false;
        }
    }
}

public class DiagnosisOption
{
    public DiagnosisOption(string code, string label)
    {
        Code = code;
        Label = label;
    }

    public string Code { get; }
    public string Label { get; }
    public string DisplayText => $"{Code} · {Label}";
}
