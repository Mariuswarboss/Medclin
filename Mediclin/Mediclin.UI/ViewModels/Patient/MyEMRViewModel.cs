using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class MyEMRViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private Pacient? _currentPacient;
    private bool _hasMedicPrincipal;
    private string? _medicPrincipalNume;
    private int? _medicPrincipalUtilizatorId;
    private bool _isLoading;
    private bool _isRequestingEmr;
    private string _requestEmrMessage = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isAssigningMedic;
    private Medic? _selectedMedicToAssign;
    private ConsultatieDisplay? _latestConsultatie;

    public MyEMRViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        Alergii = new ObservableCollection<Alergie>();
        IstoricConsltatii = new ObservableCollection<ConsultatieDisplay>();
        BolicCronice = new ObservableCollection<BolaCronica>();
        RetetActive = new ObservableCollection<RetetaDisplay>();
        AvailableMedici = new ObservableCollection<Medic>();
        RequestEMRFromDoctorCommand = new AsyncRelayCommand(async _ => await RequestEMRFromDoctorAsync(), _ => !IsRequestingEMR);
        DownloadPDFCommand = new RelayCommand(_ => RequestEMRMessage = "Exportul PDF va fi disponibil dupa actualizarea fisei medicale.");
        AssignMedicCommand = new AsyncRelayCommand(async _ => await AssignMedicAsync(), _ => SelectedMedicToAssign is not null && !IsAssigningMedic);
        SelectConsultatieCommand = new RelayCommand(p => SelectConsultatie(p as ConsultatieDisplay));
        _ = LoadAsync();
    }

    public Pacient? CurrentPacient
    {
        get => _currentPacient;
        private set
        {
            if (SetProperty(ref _currentPacient, value))
            {
                OnPropertyChanged(nameof(Initiale));
                OnPropertyChanged(nameof(NumeComplet));
                OnPropertyChanged(nameof(MetaPacient));
            }
        }
    }

    public ObservableCollection<Alergie> Alergii { get; private set; }
    public ObservableCollection<ConsultatieDisplay> IstoricConsltatii { get; private set; }
    public ObservableCollection<BolaCronica> BolicCronice { get; private set; }
    public ObservableCollection<RetetaDisplay> RetetActive { get; private set; }
    public ObservableCollection<Medic> AvailableMedici { get; private set; }

    public Medic? SelectedMedicToAssign
    {
        get => _selectedMedicToAssign;
        set
        {
            if (SetProperty(ref _selectedMedicToAssign, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasMedicPrincipal { get => _hasMedicPrincipal; private set => SetProperty(ref _hasMedicPrincipal, value); }
    public string? MedicPrincipalNume { get => _medicPrincipalNume; private set => SetProperty(ref _medicPrincipalNume, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsRequestingEMR { get => _isRequestingEmr; private set => SetProperty(ref _isRequestingEmr, value); }
    public bool IsAssigningMedic { get => _isAssigningMedic; private set => SetProperty(ref _isAssigningMedic, value); }
    public string RequestEMRMessage { get => _requestEmrMessage; private set => SetProperty(ref _requestEmrMessage, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public ConsultatieDisplay? LatestConsultatie
    {
        get => _latestConsultatie;
        private set
        {
            if (_latestConsultatie == value)
            {
                return;
            }

            if (_latestConsultatie is not null)
            {
                _latestConsultatie.IsSelected = false;
            }

            if (SetProperty(ref _latestConsultatie, value))
            {
                if (_latestConsultatie is not null)
                {
                    _latestConsultatie.IsSelected = true;
                }

                OnPropertyChanged(nameof(HasConsultatie));
            }
        }
    }

    public bool HasConsultatie => LatestConsultatie is not null;
    public bool HasAlergii => Alergii.Count > 0;
    public bool HasBoliCronice => BolicCronice.Count > 0;
    public bool HasReteteActive => RetetActive.Count > 0;
    public string Initiale => $"{(string.IsNullOrWhiteSpace(CurrentPacient?.Prenume) ? string.Empty : CurrentPacient!.Prenume![0])}{(string.IsNullOrWhiteSpace(CurrentPacient?.Nume) ? string.Empty : CurrentPacient!.Nume![0])}";
    public string NumeComplet => CurrentPacient?.NumeComplet ?? string.Empty;
    public string MetaPacient
    {
        get
        {
            if (CurrentPacient is null) return string.Empty;
            var parts = new[]
            {
                CurrentPacient.DataNasterii.HasValue ? $"{DateTime.Today.Year - CurrentPacient.DataNasterii.Value.Year} ani" : null,
                CurrentPacient.Sex,
                CurrentPacient.GrupaSanguina
            }.Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join(" · ", parts);
        }
    }

    public ICommand RequestEMRFromDoctorCommand { get; }
    public ICommand DownloadPDFCommand { get; }
    public ICommand AssignMedicCommand { get; }
    public ICommand SelectConsultatieCommand { get; }

    public async Task LoadAsync()
    {
        Alergii = new ObservableCollection<Alergie>();
        IstoricConsltatii = new ObservableCollection<ConsultatieDisplay>();
        BolicCronice = new ObservableCollection<BolaCronica>();
        RetetActive = new ObservableCollection<RetetaDisplay>();
        LatestConsultatie = null;
        NotifyMedicalCollectionsChanged();

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var pacient = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (pacient is null || pacient.Id <= 0)
            {
                ErrorMessage = "Nu s-au putut incarca datele medicale.";
                return;
            }

            CurrentPacient = pacient;

            if (pacient.MedicDeFamilieId.HasValue)
            {
                var medic = await _app.Medici.GetByIdAsync(pacient.MedicDeFamilieId.Value);
                MedicPrincipalNume = medic?.NumeCompletCuTitlu;
                _medicPrincipalUtilizatorId = medic?.UtilizatorId;
                HasMedicPrincipal = medic is not null;
            }
            else
            {
                var medici = await _app.Medici.GetAllAsync();
                AvailableMedici = new ObservableCollection<Medic>(medici);
                OnPropertyChanged(nameof(AvailableMedici));
            }

            var alergii = await _app.Pacienti.GetAlergiiAsync(pacient.Id);
            Alergii = new ObservableCollection<Alergie>(alergii ?? new List<Alergie>());

            var boli = await _app.Pacienti.GetBolicroniceAsync(pacient.Id);
            BolicCronice = new ObservableCollection<BolaCronica>(boli ?? new List<BolaCronica>());

            var consultatii = await _app.ConsultatiiRepo.GetByPacientAsync(pacient.Id);
            IstoricConsltatii = new ObservableCollection<ConsultatieDisplay>((consultatii ?? new List<Consultatie>()).Select(c => new ConsultatieDisplay(c)));
            LatestConsultatie = IstoricConsltatii.FirstOrDefault();

            var retete = await _app.ReteteRepo.GetActiveAsync(pacient.Id);
            var retetaList = new List<RetetaDisplay>();
            foreach (var reteta in retete ?? new List<Reteta>())
            {
                var medicamente = await _app.ReteteRepo.GetMedicamenteAsync(reteta.Id);
                retetaList.Add(new RetetaDisplay(reteta, medicamente ?? new List<Medicament>()));
            }

            RetetActive = new ObservableCollection<RetetaDisplay>(retetaList);
            NotifyMedicalCollectionsChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la incarcarea fisei: {ex.Message}";
            await _app.JurnalRepo.LogAsync("LOAD_EMR_ERROR", "MyEMRViewModel", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectConsultatie(ConsultatieDisplay? consultatie)
    {
        if (consultatie != null)
        {
            LatestConsultatie = consultatie;
        }
    }

    private void NotifyMedicalCollectionsChanged()
    {
        OnPropertyChanged(nameof(Alergii));
        OnPropertyChanged(nameof(IstoricConsltatii));
        OnPropertyChanged(nameof(BolicCronice));
        OnPropertyChanged(nameof(RetetActive));
        OnPropertyChanged(nameof(HasAlergii));
        OnPropertyChanged(nameof(HasBoliCronice));
        OnPropertyChanged(nameof(HasReteteActive));
        OnPropertyChanged(nameof(HasConsultatie));
    }

    private async Task RequestEMRFromDoctorAsync()
    {
        if (!HasMedicPrincipal || !_medicPrincipalUtilizatorId.HasValue || CurrentPacient is null)
        {
            RequestEMRMessage = "Nu ai un medic de familie asociat. Contacteaza clinica pentru a-ti asigna un medic.";
            return;
        }

        try
        {
            IsRequestingEMR = true;
            await _app.NotificariRepo.CreateAsync(
                _medicPrincipalUtilizatorId.Value,
                "Solicitare fisa medicala",
                $"Pacientul {CurrentPacient.NumeComplet} solicita vizualizarea/actualizarea fisei medicale.",
                "Mesaj",
                $"/emr/{CurrentPacient.Id}");
            RequestEMRMessage = $"Solicitare trimisa catre {MedicPrincipalNume}. Vei fi notificat cand fisa este actualizata.";
        }
        catch (Exception ex)
        {
            RequestEMRMessage = $"Solicitarea nu a putut fi trimisa: {ex.Message}";
            await _app.JurnalRepo.LogAsync("REQUEST_EMR_ERROR", "MyEMRViewModel", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsRequestingEMR = false;
        }
    }

    private async Task AssignMedicAsync()
    {
        if (SelectedMedicToAssign is null || CurrentPacient is null) return;

        try
        {
            IsAssigningMedic = true;
            CurrentPacient.MedicDeFamilieId = SelectedMedicToAssign.Id;
            await _app.Pacienti.UpdateAsync(CurrentPacient);

            MedicPrincipalNume = SelectedMedicToAssign.NumeCompletCuTitlu;
            _medicPrincipalUtilizatorId = SelectedMedicToAssign.UtilizatorId;
            HasMedicPrincipal = true;

            await _app.JurnalRepo.LogAsync("ASSIGN_MEDIC", "MyEMRViewModel", $"Pacientul {CurrentPacient.Id} si-a ales medicul {SelectedMedicToAssign.Id}", "Info", _utilizator.Id);
            RequestEMRMessage = "Medicul de familie a fost setat cu succes.";
        }
        catch (Exception ex)
        {
            RequestEMRMessage = $"Eroare la setarea medicului: {ex.Message}";
        }
        finally
        {
            IsAssigningMedic = false;
        }
    }
}

public class ConsultatieDisplay : INotifyPropertyChanged
{
    private bool _isSelected;

    public ConsultatieDisplay(Consultatie consultatie)
    {
        DataText = consultatie.DataConsultatie.ToString("dd.MM.yyyy HH:mm");
        MedicNume = consultatie.MedicNume ?? "Medic";
        MedicSubtitlu = string.IsNullOrWhiteSpace(consultatie.Specialitate) ? MedicNume : $"{MedicNume} · {consultatie.Specialitate}";
        Simptome = string.IsNullOrWhiteSpace(consultatie.Simptome) ? "Nu sunt simptome transmise." : consultatie.Simptome!;
        DiagnosticCod = consultatie.DiagnosticCod ?? string.Empty;
        Diagnostic = string.IsNullOrWhiteSpace(consultatie.DiagnosticText) ? "Fara diagnostic completat" : consultatie.DiagnosticText!;
        Recomandari = string.IsNullOrWhiteSpace(consultatie.Recomandari) ? "Nu sunt recomandari transmise." : consultatie.Recomandari!;
        Tensiune = string.IsNullOrWhiteSpace(consultatie.TensiuneArteriala) ? "-" : consultatie.TensiuneArteriala!;
        Puls = consultatie.Puls?.ToString(CultureInfo.InvariantCulture) ?? "-";
        Temperatura = consultatie.Temperatura?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-";
        Greutate = consultatie.Greutate?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-";
        Inaltime = consultatie.Inaltime?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-";
    }

    public string DataText { get; }
    public string MedicNume { get; }
    public string MedicSubtitlu { get; }
    public string Simptome { get; }
    public string DiagnosticCod { get; }
    public string Diagnostic { get; }
    public string Recomandari { get; }
    public string Tensiune { get; }
    public string Puls { get; }
    public string Temperatura { get; }
    public string Greutate { get; }
    public string Inaltime { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public string DiagnosticCuCod => string.IsNullOrWhiteSpace(DiagnosticCod) ? Diagnostic : $"{DiagnosticCod} · {Diagnostic}";
}

public class RetetaDisplay
{
    public RetetaDisplay(Reteta reteta, IEnumerable<Medicament> medicamente)
    {
        Id = reteta.Id;
        MedicNume = reteta.MedicNume ?? "Medic";
        DataText = reteta.DataEmitere.ToString("dd.MM.yyyy");
        ExpiraText = reteta.DataExpirare.HasValue ? reteta.DataExpirare.Value.ToString("dd.MM.yyyy") : "fara expirare";
        Observatii = reteta.Observatii ?? string.Empty;
        Medicamente = new ObservableCollection<MedicamentDisplay>(medicamente.Select(m => new MedicamentDisplay(m)));
        MedicamenteText = Medicamente.Count == 0 ? "Fara medicamente completate" : string.Join(", ", Medicamente.Select(m => m.NumeScurt));
        Status = reteta.Status;
    }

    public int Id { get; }
    public string MedicNume { get; }
    public string DataText { get; }
    public string ExpiraText { get; }
    public string MedicamenteText { get; }
    public string Observatii { get; }
    public string Status { get; }
    public ObservableCollection<MedicamentDisplay> Medicamente { get; }
}

public class MedicamentDisplay
{
    public MedicamentDisplay(Medicament medicament)
    {
        NumeScurt = string.Join(" ", new[] { medicament.Denumire, medicament.Concentratie, medicament.Forma }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        CantitateText = $"Cant. {medicament.Cantitate}";
        DozajText = string.IsNullOrWhiteSpace(medicament.Dozaj) ? "Dozaj necompletat" : medicament.Dozaj!;
        FrecventaText = medicament.Frecventa ?? string.Empty;
        DurataText = medicament.DurataZile.HasValue ? $"{medicament.DurataZile.Value} zile" : string.Empty;
        Instructiuni = medicament.Instructiuni ?? string.Empty;
    }

    public string NumeScurt { get; }
    public string CantitateText { get; }
    public string DozajText { get; }
    public string FrecventaText { get; }
    public string DurataText { get; }
    public string Instructiuni { get; }
}
