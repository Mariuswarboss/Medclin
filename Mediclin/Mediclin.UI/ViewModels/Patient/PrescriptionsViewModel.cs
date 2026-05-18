using System.Collections.ObjectModel;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class PrescriptionsViewModel : BaseViewModel
{
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    public PrescriptionsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadAsync(app, utilizator));
        RequestRenewalCommand = new RelayCommand(_ => StatusMessage = "Solicitarea de reinnoire a fost pregatita. Contacteaza medicul din Mesaje pentru confirmare.");
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
}
