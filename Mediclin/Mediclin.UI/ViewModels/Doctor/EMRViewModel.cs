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
    private string _alergieSubstanta = string.Empty;
    private string _alergieSeveritate = "Moderata";
    private string _alergieObservatii = string.Empty;
    private string _analizaLaborator = "MediClin Lab";
    private string _analizaTest = string.Empty;
    private string _analizaValoare = string.Empty;
    private string _analizaUnitate = string.Empty;
    private string _analizaStatus = "Normal";
    private string _analizaInterpretare = string.Empty;
    private bool _isSaving;
    private bool _isTrimiting;
    private bool _showSaveToast;
    private bool _isSaveToastSuccess;
    private string _saveToastMessage = string.Empty;
    private string _saveToastIcon = "✓";
    private bool _isLoading;
    private string _searchQuery = string.Empty;
    private bool _isSearching;
    private bool _isSearchPanelVisible;

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
        AlergieSeveritateOptions = new ObservableCollection<string>
        {
            "Usoara",
            "Moderata",
            "Severa",
            "Anafilaxie"
        };
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
        SaveDraftCommand = new AsyncRelayCommand(async _ => await SaveDraftAsync(), _ => CurrentPacient is not null && _medicId > 0 && !IsSaving && !IsTrimiting);
        TrimiteRezultatCommand = new AsyncRelayCommand(async _ => await TrimiteRezultatAsync(), _ => CurrentPacient is not null && _medicId > 0 && !IsSaving && !IsTrimiting);
        EmiteRetetaCommand = new AsyncRelayCommand(async _ => await EmiteAsync(), _ => CurrentPacient is not null && CurrentConsultatie is not null && CurrentConsultatie.Id > 0 && !IsSaving);
        LoadPatientCommand = new AsyncRelayCommand(async _ => await ReloadAsync());
        StartConsultatieNouaCommand = new AsyncRelayCommand(async _ => await StartNouaAsync(), _ => CurrentPacient is not null && _medicId > 0 && !IsSaving);
        SearchCommand = new AsyncRelayCommand(async _ => await ExecuteSearchAsync());
        SelectPatientCommand = new AsyncRelayCommand(async p => { if (p is Pacient pac) await SelectPatientAsync(pac); });
        OpenSearchCommand = new RelayCommand(_ => IsSearchPanelVisible = true);
        AddAlergieCommand = new AsyncRelayCommand(async _ => await AddAlergieAsync(), _ => CurrentPacient is not null && !string.IsNullOrWhiteSpace(AlergieSubstanta));
        TrimiteAnalizaCommand = new AsyncRelayCommand(async _ => await TrimiteAnalizaAsync(), _ => CurrentPacient is not null && !string.IsNullOrWhiteSpace(AnalizaTest) && !string.IsNullOrWhiteSpace(AnalizaValoare));

        SearchResults = new ObservableCollection<Pacient>();
        IsSearchPanelVisible = CurrentPacient is null;
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
    public ObservableCollection<string> AlergieSeveritateOptions { get; }
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

    public string AlergieSubstanta
    {
        get => _alergieSubstanta;
        set
        {
            if (SetProperty(ref _alergieSubstanta, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string AlergieSeveritate
    {
        get => _alergieSeveritate;
        set => SetProperty(ref _alergieSeveritate, value);
    }

    public string AlergieObservatii
    {
        get => _alergieObservatii;
        set => SetProperty(ref _alergieObservatii, value);
    }

    public string AnalizaLaborator
    {
        get => _analizaLaborator;
        set => SetProperty(ref _analizaLaborator, value);
    }

    public string AnalizaTest
    {
        get => _analizaTest;
        set
        {
            if (SetProperty(ref _analizaTest, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string AnalizaValoare
    {
        get => _analizaValoare;
        set
        {
            if (SetProperty(ref _analizaValoare, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string AnalizaUnitate
    {
        get => _analizaUnitate;
        set => SetProperty(ref _analizaUnitate, value);
    }

    public string AnalizaStatus
    {
        get => _analizaStatus;
        set => SetProperty(ref _analizaStatus, value);
    }

    public string AnalizaInterpretare
    {
        get => _analizaInterpretare;
        set => SetProperty(ref _analizaInterpretare, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
    }

    public bool IsTrimiting
    {
        get => _isTrimiting;
        set
        {
            if (SetProperty(ref _isTrimiting, value))
                CommandManager.InvalidateRequerySuggested();
        }
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

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value) && value.Length >= 2)
                _ = ExecuteSearchAsync();
            else if (value.Length < 2)
                SearchResults.Clear();
        }
    }

    public bool IsSearching
    {
        get => _isSearching;
        set => SetProperty(ref _isSearching, value);
    }

    public bool IsSearchPanelVisible
    {
        get => _isSearchPanelVisible;
        set => SetProperty(ref _isSearchPanelVisible, value);
    }

    public ObservableCollection<Pacient> SearchResults { get; }

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
    public ICommand SaveDraftCommand { get; }
    public ICommand TrimiteRezultatCommand { get; }
    public ICommand EmiteRetetaCommand { get; }
    public ICommand LoadPatientCommand { get; }
    public ICommand StartConsultatieNouaCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand SelectPatientCommand { get; }
    public ICommand OpenSearchCommand { get; }
    public ICommand AddAlergieCommand { get; }
    public ICommand TrimiteAnalizaCommand { get; }

    // ── Search ──────────────────────────────────────────────────
    private async Task ExecuteSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery) || _searchQuery.Length < 2)
        {
            SearchResults.Clear();
            return;
        }

        try
        {
            IsSearching = true;
            var results = await _app.Pacienti.SearchByNameAsync(_searchQuery);
            SearchResults.Clear();
            foreach (var p in results)
                SearchResults.Add(p);
        }
        catch
        {
            SearchResults.Clear();
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task SelectPatientAsync(Pacient pacient)
    {
        CurrentPacient = pacient;
        IsSearchPanelVisible = false;
        SearchQuery = string.Empty;
        SearchResults.Clear();

        // Reset form fields
        Simptome = string.Empty;
        DiagnosticCod = string.Empty;
        DiagnosticText = string.Empty;
        Recomandari = string.Empty;
        NotePrvate = string.Empty;
        Tensiune = string.Empty;
        Puls = string.Empty;
        Temperatura = string.Empty;
        Greutate = string.Empty;
        Inaltime = string.Empty;
        SelectedDiagnosisOption = null;
        CurrentConsultatie = null;

        OnPropertyChanged(nameof(HasPatientSelected));
        OnPropertyChanged(nameof(CurrentPatientInitials));
        OnPropertyChanged(nameof(CurrentPatientMeta));
        OnPropertyChanged(nameof(CanStartNew));
        CommandManager.InvalidateRequerySuggested();

        await ReloadAsync();
    }

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
                // Fără programare azi - afişăm ultima consultație din istoric (read-only)
                CurrentConsultatie = IstoricConsltatii.FirstOrDefault();
            }

            OnPropertyChanged(nameof(CanStartNew));

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

    public bool CanStartNew => CurrentPacient is not null && _medicId > 0;

    private async Task StartNouaAsync()
    {
        if (CurrentPacient is null || _medicId <= 0)
        {
            return;
        }

        try
        {
            IsSaving = true;
            CurrentConsultatie = await _app.Consultatii.StartConsultatieFaraProgramareAsync(CurrentPacient.Id, _medicId);
            // Resetam campurile pentru o noua consultatie
            Simptome = string.Empty;
            DiagnosticCod = string.Empty;
            DiagnosticText = string.Empty;
            Recomandari = string.Empty;
            NotePrvate = string.Empty;
            Tensiune = string.Empty;
            Puls = string.Empty;
            Temperatura = string.Empty;
            Greutate = string.Empty;
            Inaltime = string.Empty;
            SelectedDiagnosisOption = null;
            OnPropertyChanged(nameof(CanStartNew));
            CommandManager.InvalidateRequerySuggested();
            await ShowToastAsync(true, "Consultație nouă inițiată!");
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Eroare: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task SaveAsync()
    {
        if (CurrentPacient is null || CurrentConsultatie is null) return;

        try
        {
            IsSaving = true;
            BuildConsultatieFromFields(CurrentConsultatie);

            var (ok, error) = await _app.Consultatii.SaveDraftAsync(CurrentConsultatie);
            if (!ok)
            {
                await ShowToastAsync(false, $"Eroare: {error}");
                return;
            }

            await ShowToastAsync(true, "Fișă salvată cu succes");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Eroare la salvare: {ex.Message}");
            await _app.JurnalRepo.LogAsync("SAVE_DOCTOR_EMR_ERROR", "DoctorEMR", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsSaving = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task SaveDraftAsync()
    {
        if (CurrentPacient is null) return;

        try
        {
            IsSaving = true;

            // Dacă nu există consultație activă, creăm una nouă
            if (CurrentConsultatie is null || CurrentConsultatie.Id <= 0)
            {
                CurrentConsultatie = await _app.Consultatii.StartConsultatieFaraProgramareAsync(
                    CurrentPacient.Id, _medicId);
            }

            BuildConsultatieFromFields(CurrentConsultatie);

            var (ok, error) = await _app.Consultatii.SaveDraftAsync(CurrentConsultatie);
            if (!ok)
            {
                await ShowToastAsync(false, $"Eroare: {error}");
                return;
            }

            await ShowToastAsync(true, "Draft salvat ✓");
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Eroare la salvare draft: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task TrimiteRezultatAsync()
    {
        if (CurrentPacient is null) return;

        try
        {
            IsTrimiting = true;

            if (CurrentConsultatie is null || CurrentConsultatie.Id <= 0)
            {
                CurrentConsultatie = await _app.Consultatii.StartConsultatieFaraProgramareAsync(
                    CurrentPacient.Id, _medicId);
            }

            BuildConsultatieFromFields(CurrentConsultatie);

            var (ok, error) = await _app.Consultatii.TrimiteRezultatAsync(
                CurrentConsultatie,
                CurrentPacient.UtilizatorId,
                _app.NotificariRepo);

            if (!ok)
            {
                await ShowToastAsync(false, $"Eroare la trimitere: {error}");
                return;
            }

            await ShowToastAsync(true, "Fișa a fost trimisă pacientului ✓");
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Eroare: {ex.Message}");
        }
        finally
        {
            IsTrimiting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>Copiază valorile din câmpuri în obiectul Consultatie.</summary>
    private void BuildConsultatieFromFields(Consultatie c)
    {
        c.Simptome           = Simptome;
        c.DiagnosticCod      = DiagnosticCod;
        c.DiagnosticText     = DiagnosticText;
        c.Recomandari        = Recomandari;
        c.NotePrivate        = NotePrvate;
        c.TensiuneArteriala  = string.IsNullOrWhiteSpace(Tensiune) ? null : Tensiune;
        c.Puls               = int.TryParse(Puls, out var p)    ? p    : null;
        c.Temperatura        = decimal.TryParse(Temperatura, NumberStyles.Any, CultureInfo.InvariantCulture, out var t) ? t : null;
        c.Greutate           = decimal.TryParse(Greutate,    NumberStyles.Any, CultureInfo.InvariantCulture, out var g) ? g : null;
        c.Inaltime           = decimal.TryParse(Inaltime,    NumberStyles.Any, CultureInfo.InvariantCulture, out var h) ? h : null;
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

    private async Task AddAlergieAsync()
    {
        if (CurrentPacient is null || string.IsNullOrWhiteSpace(AlergieSubstanta))
        {
            return;
        }

        try
        {
            var alergie = new Alergie
            {
                PacientId = CurrentPacient.Id,
                Substanta = AlergieSubstanta.Trim(),
                Severitate = NormalizeAlergieSeveritate(AlergieSeveritate),
                Observatii = string.IsNullOrWhiteSpace(AlergieObservatii) ? null : AlergieObservatii.Trim()
            };

            await _app.Pacienti.AddAlergieAsync(alergie);
            Alergii.Add(alergie);
            AlergieSubstanta = string.Empty;
            AlergieSeveritate = "Moderata";
            AlergieObservatii = string.Empty;
            await ShowToastAsync(true, "Alergia a fost adaugata.");
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Alergia nu a putut fi adaugata: {GetDetailedMessage(ex)}");
        }
    }

    private static string NormalizeAlergieSeveritate(string? severitate)
    {
        var normalized = (severitate ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "usoara" or "ușoară" or "usoară" => "Usoara",
            "medie" or "mediu" or "moderata" or "moderată" => "Moderata",
            "severa" or "severă" or "grava" or "gravă" => "Severa",
            "anafilaxie" => "Anafilaxie",
            _ => "Moderata"
        };
    }

    private static string GetDetailedMessage(Exception ex)
    {
        return ex.InnerException is null ? ex.Message : ex.InnerException.Message;
    }

    private async Task TrimiteAnalizaAsync()
    {
        if (CurrentPacient is null || string.IsNullOrWhiteSpace(AnalizaTest) || string.IsNullOrWhiteSpace(AnalizaValoare))
        {
            return;
        }

        try
        {
            var rezultat = new RezultatAnaliza
            {
                PacientId = CurrentPacient.Id,
                ConsultatieId = CurrentConsultatie?.Id > 0 ? CurrentConsultatie.Id : null,
                DataRecoltare = DateTime.Today,
                DataRezultat = DateTime.Today,
                Laborator = string.IsNullOrWhiteSpace(AnalizaLaborator) ? "MediClin Lab" : AnalizaLaborator.Trim(),
                Interpretare = string.IsNullOrWhiteSpace(AnalizaInterpretare) ? null : AnalizaInterpretare.Trim()
            };

            var rezultatId = await _app.AnalizeRepo.CreateAsync(rezultat);
            await _app.AnalizeRepo.AddValoareAsync(new ValoareAnaliza
            {
                RezultatId = rezultatId,
                TestNume = AnalizaTest.Trim(),
                Valoare = AnalizaValoare.Trim(),
                Unitate = string.IsNullOrWhiteSpace(AnalizaUnitate) ? null : AnalizaUnitate.Trim(),
                Status = string.IsNullOrWhiteSpace(AnalizaStatus) ? "Normal" : AnalizaStatus.Trim()
            });

            await _app.NotificariRepo.CreateAsync(
                CurrentPacient.UtilizatorId,
                "Rezultat analiza disponibil",
                $"Dr. {_utilizator.NumeComplet} a transmis rezultatul pentru {AnalizaTest.Trim()}.",
                "Analiza",
                "/analize");

            AnalizaTest = string.Empty;
            AnalizaValoare = string.Empty;
            AnalizaUnitate = string.Empty;
            AnalizaInterpretare = string.Empty;
            await ShowToastAsync(true, "Rezultatul analizei a fost transmis pacientului.");
        }
        catch (Exception ex)
        {
            await ShowToastAsync(false, $"Analiza nu a putut fi trimisa: {ex.Message}");
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
