using System.Collections.ObjectModel;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class PrescriptionsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    public PrescriptionsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync(app, utilizator));
        RequestRenewalCommand = new AsyncRelayCommand(async p => await RequestRenewalAsync(p as RetetaDisplay));
        _ = LoadAsync(app, utilizator);
    }

    public ObservableCollection<RetetaDisplay> Retete { get; } = new();
    public ICommand RefreshCommand { get; }
    public ICommand RequestRenewalCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool HasRetete => Retete.Count > 0;

    private async Task LoadAsync(ApplicationServices app, Utilizator utilizator)
    {
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            Retete.Clear();

            var p = await app.Pacienti.GetByUtilizatorIdAsync(utilizator.Id);
            if (p is null)
            {
                StatusMessage = "Nu s-a gasit profilul de pacient.";
                OnPropertyChanged(nameof(HasRetete));
                return;
            }

            var retete = await app.ReteteRepo.GetActiveAsync(p.Id);
            foreach (var r in retete ?? new List<Reteta>())
            {
                var medicamente = await app.ReteteRepo.GetMedicamenteAsync(r.Id);
                Retete.Add(new RetetaDisplay(r, medicamente ?? new List<Medicament>()));
            }

            StatusMessage = Retete.Count == 0 ? "Nu exista retete active." : string.Empty;
            OnPropertyChanged(nameof(HasRetete));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Retetele nu au putut fi incarcate: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RequestRenewalAsync(RetetaDisplay? reteta)
    {
        if (reteta is null)
        {
            StatusMessage = "Alege o reteta pentru reinnoire.";
            return;
        }

        try
        {
            var pacient = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (pacient is null)
            {
                StatusMessage = "Nu s-a gasit profilul de pacient.";
                return;
            }

            if (!pacient.MedicDeFamilieId.HasValue)
            {
                StatusMessage = "Alege mai intai medicul de familie din fisa medicala.";
                return;
            }

            var medic = await _app.Medici.GetByIdAsync(pacient.MedicDeFamilieId.Value);
            if (medic?.UtilizatorId is null)
            {
                StatusMessage = "Medicul de familie nu are cont valid pentru notificare.";
                return;
            }

            await _app.NotificariRepo.CreateAsync(
                medic.UtilizatorId,
                "Solicitare reinnoire reteta",
                $"{pacient.NumeComplet} solicita reinnoirea retetei: {reteta.MedicamenteText}.",
                "Reteta",
                $"/retete/{reteta.Id}/reinnoire");

            StatusMessage = $"Solicitarea de reinnoire a fost trimisa catre {medic.NumeCompletCuTitlu}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Solicitarea nu a putut fi trimisa: {ex.Message}";
        }
    }
}
