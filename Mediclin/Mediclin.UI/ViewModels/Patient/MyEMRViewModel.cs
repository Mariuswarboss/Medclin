using System.Collections.ObjectModel;
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

    public MyEMRViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        Alergii = new ObservableCollection<Alergie>();
        IstoricConsltatii = new ObservableCollection<ConsultatieDisplay>();
        BolicCronice = new ObservableCollection<BolaCronica>();
        RetetActive = new ObservableCollection<RetetaDisplay>();
        RequestEMRFromDoctorCommand = new AsyncRelayCommand(async _ => await RequestEMRFromDoctorAsync(), _ => !IsRequestingEMR);
        DownloadPDFCommand = new RelayCommand(_ => RequestEMRMessage = "Exportul PDF va fi disponibil după actualizarea fișei medicale.");
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

    public bool HasMedicPrincipal { get => _hasMedicPrincipal; private set => SetProperty(ref _hasMedicPrincipal, value); }
    public string? MedicPrincipalNume { get => _medicPrincipalNume; private set => SetProperty(ref _medicPrincipalNume, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public bool IsRequestingEMR { get => _isRequestingEmr; private set => SetProperty(ref _isRequestingEmr, value); }
    public string RequestEMRMessage { get => _requestEmrMessage; private set => SetProperty(ref _requestEmrMessage, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
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

    public async Task LoadAsync()
    {
        Alergii = new ObservableCollection<Alergie>();
        IstoricConsltatii = new ObservableCollection<ConsultatieDisplay>();
        BolicCronice = new ObservableCollection<BolaCronica>();
        RetetActive = new ObservableCollection<RetetaDisplay>();
        OnPropertyChanged(nameof(Alergii));
        OnPropertyChanged(nameof(IstoricConsltatii));
        OnPropertyChanged(nameof(BolicCronice));
        OnPropertyChanged(nameof(RetetActive));

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var pacient = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (pacient is null || pacient.Id <= 0)
            {
                ErrorMessage = "Nu s-au putut încărca datele medicale.";
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

            var alergii = await _app.Pacienti.GetAlergiiAsync(pacient.Id);
            Alergii = new ObservableCollection<Alergie>(alergii ?? new List<Alergie>());
            OnPropertyChanged(nameof(Alergii));

            var boli = await _app.Pacienti.GetBolicroniceAsync(pacient.Id);
            BolicCronice = new ObservableCollection<BolaCronica>(boli ?? new List<BolaCronica>());
            OnPropertyChanged(nameof(BolicCronice));

            var consultatii = await _app.ConsultatiiRepo.GetByPacientAsync(pacient.Id);
            IstoricConsltatii = new ObservableCollection<ConsultatieDisplay>((consultatii ?? new List<Consultatie>()).Select(c => new ConsultatieDisplay(c)));
            OnPropertyChanged(nameof(IstoricConsltatii));

            var retete = await _app.ReteteRepo.GetActiveAsync(pacient.Id);
            var retetaList = new List<RetetaDisplay>();
            foreach (var reteta in retete ?? new List<Reteta>())
            {
                var medicamente = await _app.ReteteRepo.GetMedicamenteAsync(reteta.Id);
                retetaList.Add(new RetetaDisplay(reteta, medicamente ?? new List<Medicament>()));
            }

            RetetActive = new ObservableCollection<RetetaDisplay>(retetaList);
            OnPropertyChanged(nameof(RetetActive));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare la încărcarea fișei: {ex.Message}";
            await _app.JurnalRepo.LogAsync("LOAD_EMR_ERROR", "MyEMRViewModel", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RequestEMRFromDoctorAsync()
    {
        if (!HasMedicPrincipal || !_medicPrincipalUtilizatorId.HasValue || CurrentPacient is null)
        {
            RequestEMRMessage = "Nu ai un medic de familie asociat. Contactează clinica pentru a-ți asigna un medic.";
            return;
        }

        try
        {
            IsRequestingEMR = true;
            await _app.NotificariRepo.CreateAsync(
                _medicPrincipalUtilizatorId.Value,
                "Solicitare fișă medicală",
                $"Pacientul {CurrentPacient.NumeComplet} solicită vizualizarea/actualizarea fișei medicale.",
                "Mesaj",
                $"/emr/{CurrentPacient.Id}");
            RequestEMRMessage = $"Solicitare trimisă către {MedicPrincipalNume}. Vei fi notificat când fișa este actualizată.";
        }
        catch (Exception ex)
        {
            RequestEMRMessage = $"Solicitarea nu a putut fi trimisă: {ex.Message}";
            await _app.JurnalRepo.LogAsync("REQUEST_EMR_ERROR", "MyEMRViewModel", ex.Message, "Eroare", _utilizator.Id);
        }
        finally
        {
            IsRequestingEMR = false;
        }
    }
}

public class ConsultatieDisplay
{
    public ConsultatieDisplay(Consultatie consultatie)
    {
        DataText = consultatie.DataConsultatie.ToString("dd.MM.yyyy HH:mm");
        MedicNume = consultatie.MedicNume ?? "Medic";
        Diagnostic = string.IsNullOrWhiteSpace(consultatie.DiagnosticText) ? "Fără diagnostic completat" : consultatie.DiagnosticText!;
        Recomandari = consultatie.Recomandari ?? string.Empty;
    }

    public string DataText { get; }
    public string MedicNume { get; }
    public string Diagnostic { get; }
    public string Recomandari { get; }
}

public class RetetaDisplay
{
    public RetetaDisplay(Reteta reteta, IEnumerable<Medicament> medicamente)
    {
        MedicNume = reteta.MedicNume ?? "Medic";
        DataText = reteta.DataEmitere.ToString("dd.MM.yyyy");
        ExpiraText = reteta.DataExpirare.HasValue ? reteta.DataExpirare.Value.ToString("dd.MM.yyyy") : "fără expirare";
        MedicamenteText = string.Join(", ", medicamente.Select(m => m.Denumire).Where(x => !string.IsNullOrWhiteSpace(x)));
        Status = reteta.Status;
    }

    public string MedicNume { get; }
    public string DataText { get; }
    public string ExpiraText { get; }
    public string MedicamenteText { get; }
    public string Status { get; }
}
