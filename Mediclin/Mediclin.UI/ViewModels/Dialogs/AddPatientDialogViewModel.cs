using System.Windows;
using System.Windows.Input;
using Mediclin.UI.ViewModels;
using Mediclin.Business.DTOs;
using Mediclin.Business.Helpers;
using Mediclin.Business.Validators;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Dialogs;

public class AddPatientDialogViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Window _window;
    private string _prenume = string.Empty;
    private string _nume = string.Empty;
    private string _email = string.Empty;
    private string _telefon = string.Empty;
    private DateTime? _dataNasterii;
    private string _sex = "Feminin";
    private string _cnp = string.Empty;
    private string _grupaSanguina = "A+";
    private string _parolaGenerata = string.Empty;
    private string _errorMessage = string.Empty;

    public AddPatientDialogViewModel(ApplicationServices app, Window window)
    {
        _app = app;
        _window = window;
        _parolaGenerata = GenereazaParolaSigura();
        SexOptions = new List<string> { "Masculin", "Feminin", "Altul" };
        GrupaOptions = new List<string> { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        SalveazaCommand = new AsyncRelayCommand(_ => SalveazaAsync());
        AnuleazaCommand = new RelayCommand(_ => { _window.DialogResult = false; _window.Close(); });
    }

    public List<string> SexOptions { get; }
    public List<string> GrupaOptions { get; }

    public string ParolaGenerata
    {
        get => _parolaGenerata;
        set => SetProperty(ref _parolaGenerata, value);
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

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Telefon
    {
        get => _telefon;
        set => SetProperty(ref _telefon, value);
    }

    public DateTime? DataNasterii
    {
        get => _dataNasterii;
        set => SetProperty(ref _dataNasterii, value);
    }

    public string Sex
    {
        get => _sex;
        set => SetProperty(ref _sex, value);
    }

    public string Cnp
    {
        get => _cnp;
        set => SetProperty(ref _cnp, value);
    }

    public string GrupaSanguina
    {
        get => _grupaSanguina;
        set => SetProperty(ref _grupaSanguina, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy { get; private set; }

    public ICommand SalveazaCommand { get; }
    public ICommand AnuleazaCommand { get; }

    private static string GenereazaParolaSigura()
    {
        var rnd = Guid.NewGuid().ToString("N")[..10];
        return $"Mc{rnd}A1";
    }

    private async Task SalveazaAsync()
    {
        ErrorMessage = string.Empty;
        var dto = new RegisterDto
        {
            Email = Email.Trim(),
            Parola = ParolaGenerata,
            ConfirmareParola = ParolaGenerata,
            Prenume = Prenume.Trim(),
            Nume = Nume.Trim(),
            Telefon = Telefon.Trim(),
            Rol = "pacient"
        };

        var errs = UserValidator.ValidateRegisterDto(dto);
        if (errs.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, errs);
            return;
        }

        if (await _app.Utilizatori.ExistsEmailAsync(dto.Email))
        {
            ErrorMessage = "Acest email este deja înregistrat.";
            return;
        }

        IsBusy = true;
        try
        {
            var utilizator = new Utilizator
            {
                Email = dto.Email,
                ParolaHash = PasswordHelper.HashPassword(ParolaGenerata),
                Rol = "pacient",
                Prenume = dto.Prenume,
                Nume = dto.Nume,
                Telefon = string.IsNullOrWhiteSpace(Telefon) ? null : Telefon.Trim(),
                Activ = true
            };

            var uid = await _app.Utilizatori.CreateAsync(utilizator);

            var pacient = new Pacient
            {
                UtilizatorId = uid,
                DataNasterii = DataNasterii.HasValue ? DateOnly.FromDateTime(DataNasterii.Value) : null,
                Sex = Sex,
                Cnp = string.IsNullOrWhiteSpace(Cnp) ? null : Cnp.Trim(),
                GrupaSanguina = GrupaSanguina
            };

            await _app.Pacienti.CreateAsync(pacient);

            MessageBox.Show($"Pacient creat.\nParola temporară (trimiteți pacientului): {ParolaGenerata}", "MediClin",
                MessageBoxButton.OK, MessageBoxImage.Information);

            _window.DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
