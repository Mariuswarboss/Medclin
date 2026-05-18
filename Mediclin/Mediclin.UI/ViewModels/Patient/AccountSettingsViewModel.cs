using System.Windows.Input;
using Mediclin.Business.DTOs;
using Mediclin.Business.Helpers;
using Mediclin.Business.Validators;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class AccountSettingsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private string _sectiune = "Profil";
    private string _prenume = string.Empty;
    private string _nume = string.Empty;
    private string _telefon = string.Empty;
    private string _email = string.Empty;
    private string _oras = string.Empty;
    private string _adresa = string.Empty;
    private string _parolaVeche = string.Empty;
    private string _parolaNoua = string.Empty;
    private string _parolaConfirm = string.Empty;
    private string _mesaj = string.Empty;
    private string _avatarUrl = string.Empty;

    public AccountSettingsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        _prenume = utilizator.Prenume;
        _nume = utilizator.Nume;
        _telefon = utilizator.Telefon ?? string.Empty;
        _email = utilizator.Email;
        _avatarUrl = utilizator.AvatarUrl ?? string.Empty;
        SalveazaProfilCommand = new RelayCommand(_ => _ = SalveazaProfilAsync());
        SchimbaParolaCommand = new RelayCommand(_ => _ = SchimbaParolaAsync());
        SalveazaNotificariCommand = new RelayCommand(_ => _ = SalveazaNotificariAsync());
        SchimbaSectiuneCommand = new RelayCommand(p =>
        {
            if (p is string s)
            {
                Sectiune = s;
                Mesaj = string.Empty;
            }
        });
        _ = IncarcaOrasAsync();
    }

    public string Sectiune
    {
        get => _sectiune;
        set => SetProperty(ref _sectiune, value);
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

    public string Oras
    {
        get => _oras;
        set => SetProperty(ref _oras, value);
    }

    public string Adresa
    {
        get => _adresa;
        set => SetProperty(ref _adresa, value);
    }

    public string ParolaVeche
    {
        get => _parolaVeche;
        set => SetProperty(ref _parolaVeche, value);
    }

    public string ParolaNoua
    {
        get => _parolaNoua;
        set => SetProperty(ref _parolaNoua, value);
    }

    public string ParolaConfirm
    {
        get => _parolaConfirm;
        set => SetProperty(ref _parolaConfirm, value);
    }

    public string Mesaj
    {
        get => _mesaj;
        set => SetProperty(ref _mesaj, value);
    }

    public string AvatarUrl
    {
        get => _avatarUrl;
        set => SetProperty(ref _avatarUrl, value);
    }

    public ICommand SalveazaProfilCommand { get; }
    public ICommand SchimbaParolaCommand { get; }
    public ICommand SalveazaNotificariCommand { get; }
    public ICommand SchimbaSectiuneCommand { get; }

    private async Task IncarcaOrasAsync()
    {
        try
        {
            var p = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (p is not null)
            {
                Oras = p.Oras ?? string.Empty;
                Adresa = p.Adresa ?? string.Empty;
            }
        }
        catch
        {
            // ignorat
        }
    }

    private async Task SalveazaProfilAsync()
    {
        try
        {
            var u = await _app.Utilizatori.GetByIdAsync(_utilizator.Id);
            if (u is null)
            {
                return;
            }

            u.Prenume = Prenume;
            u.Nume = Nume;
            u.Telefon = string.IsNullOrWhiteSpace(Telefon) ? null : Telefon;
            u.Email = Email;
            u.AvatarUrl = string.IsNullOrWhiteSpace(AvatarUrl) ? null : AvatarUrl;
            await _app.Utilizatori.UpdateAsync(u);

            var p = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (p is not null)
            {
                p.Oras = string.IsNullOrWhiteSpace(Oras) ? null : Oras;
                p.Adresa = string.IsNullOrWhiteSpace(Adresa) ? null : Adresa;
                await _app.Pacienti.UpdateAsync(p);
            }

            Mesaj = "Profilul a fost salvat.";
        }
        catch (Exception ex)
        {
            Mesaj = ex.Message;
        }
    }

    private async Task SchimbaParolaAsync()
    {
        try
        {
            var u = await _app.Utilizatori.GetByEmailAsync(_utilizator.Email);
            if (u is null || !PasswordHelper.VerifyPassword(ParolaVeche, u.ParolaHash))
            {
                Mesaj = "Parola curentă este incorectă.";
                return;
            }

            var dto = new RegisterDto
            {
                Parola = ParolaNoua,
                ConfirmareParola = ParolaConfirm,
                Email = Email,
                Prenume = Prenume,
                Nume = Nume,
                Telefon = Telefon,
                Rol = "pacient"
            };
            var errs = UserValidator.ValidateRegisterDto(dto);
            if (errs.Count > 0)
            {
                Mesaj = string.Join("; ", errs);
                return;
            }

            await _app.Utilizatori.UpdateParolaHashAsync(u.Id, PasswordHelper.HashPassword(ParolaNoua));
            ParolaVeche = ParolaNoua = ParolaConfirm = string.Empty;
            Mesaj = "Parola a fost schimbată.";
        }
        catch (Exception ex)
        {
            Mesaj = ex.Message;
        }
    }

    private async Task SalveazaNotificariAsync()
    {
        try
        {
            await _app.Setari.SetValoareAsync($"notif_pref_{_utilizator.Id}", "1");
            Mesaj = "Preferințele au fost salvate.";
        }
        catch (Exception ex)
        {
            Mesaj = ex.Message;
        }
    }
}
