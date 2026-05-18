using System.Threading.Tasks;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Doctor;

public class SettingsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private Medic? _medic;
    private int _durataConsultatie = 30;
    private decimal _tarifConsultatie = 300;
    private string _mesaj = string.Empty;
    private string _prenume = string.Empty;
    private string _nume = string.Empty;
    private string _telefon = string.Empty;
    private string _email = string.Empty;
    private string _avatarUrl = string.Empty;

    public SettingsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        Mesaj = $"Profil: {utilizator.NumeComplet} ({utilizator.Email})";
        SalveazaCommand = new RelayCommand(_ => _ = SalveazaAsync());
        _ = IncarcaAsync();
    }

    public int DurataConsultatie
    {
        get => _durataConsultatie;
        set => SetProperty(ref _durataConsultatie, value);
    }

    public decimal TarifConsultatie
    {
        get => _tarifConsultatie;
        set => SetProperty(ref _tarifConsultatie, value);
    }

    public string Mesaj
    {
        get => _mesaj;
        set => SetProperty(ref _mesaj, value);
    }

    public string Prenume
    {
        get => _prenume;
        set => SetProperty(ref _prenume, value);
    }

    public string Nume
    {
        get => _nume;
        set => SetProperty(ref _nume, value);
    }

    public string Telefon
    {
        get => _telefon;
        set => SetProperty(ref _telefon, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string AvatarUrl
    {
        get => _avatarUrl;
        set => SetProperty(ref _avatarUrl, value);
    }

    public ICommand SalveazaCommand { get; }

    private async Task IncarcaAsync()
    {
        try
        {
            _medic = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
            if (_medic is not null)
            {
                DurataConsultatie = _medic.DurataConsultatie;
                TarifConsultatie = _medic.TarifConsultatie;
            }

            var u = await _app.Utilizatori.GetByIdAsync(_utilizator.Id);
            if (u is not null)
            {
                Prenume = u.Prenume;
                Nume = u.Nume;
                Telefon = u.Telefon ?? string.Empty;
                Email = u.Email;
                AvatarUrl = u.AvatarUrl ?? string.Empty;
            }
        }
        catch
        {
            // ignorat
        }
    }

    private async Task SalveazaAsync()
    {
        try
        {
            var u = await _app.Utilizatori.GetByIdAsync(_utilizator.Id);
            if (u is not null)
            {
                u.Prenume = Prenume;
                u.Nume = Nume;
                u.Telefon = string.IsNullOrWhiteSpace(Telefon) ? null : Telefon;
                u.Email = Email;
                u.AvatarUrl = string.IsNullOrWhiteSpace(AvatarUrl) ? null : AvatarUrl;
                await _app.Utilizatori.UpdateAsync(u);
            }

            if (_medic is not null)
            {
                _medic.DurataConsultatie = DurataConsultatie;
                _medic.TarifConsultatie = TarifConsultatie;
                await _app.Medici.UpdateAsync(_medic);
            }
            
            Mesaj = "Setările au fost salvate cu succes.";
        }
        catch (System.Exception ex)
        {
            Mesaj = $"Eroare la salvare: {ex.Message}";
        }
    }
}
