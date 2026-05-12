using System.Windows.Input;
using Mediclin.Business.DTOs;
using Mediclin.Business.Services;
using Mediclin.Business.Validators;
using Mediclin.UI.Views.Auth;

namespace Mediclin.UI.ViewModels.Auth;

public class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _email = string.Empty;
    private string _parola = string.Empty;
    private string _confirmareParola = string.Empty;
    private string _prenume = string.Empty;
    private string _nume = string.Empty;
    private string _telefon = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isLoading;
    private int _passwordStrength;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
        RegisterCommand = new RelayCommand(async _ => await RegisterAsync(), _ => !IsLoading);
        NavigateToLoginCommand = new RelayCommand(_ => NavigateToLogin());
    }

    public Action? CloseAction { get; set; }

    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Parola
    {
        get => _parola;
        set
        {
            if (SetProperty(ref _parola, value))
            {
                PasswordStrength = CalculatePasswordStrength(value);
            }
        }
    }
    public string ConfirmareParola { get => _confirmareParola; set => SetProperty(ref _confirmareParola, value); }
    public string Prenume { get => _prenume; set => SetProperty(ref _prenume, value); }
    public string Nume { get => _nume; set => SetProperty(ref _nume, value); }
    public string Telefon { get => _telefon; set => SetProperty(ref _telefon, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public string SuccessMessage { get => _successMessage; set => SetProperty(ref _successMessage, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public int PasswordStrength { get => _passwordStrength; set => SetProperty(ref _passwordStrength, value); }
    public ICommand RegisterCommand { get; }
    public ICommand NavigateToLoginCommand { get; }

    private async Task RegisterAsync()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var dto = new RegisterDto
        {
            Email = Email.Trim(),
            Parola = Parola,
            ConfirmareParola = ConfirmareParola,
            Prenume = Prenume.Trim(),
            Nume = Nume.Trim(),
            Telefon = Telefon.Trim(),
            Rol = "pacient"
        };

        var validationErrors = UserValidator.ValidateRegisterDto(dto);
        if (validationErrors.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, validationErrors);
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _authService.RegisterAsync(dto);
            if (!result.success)
            {
                ErrorMessage = result.error;
                return;
            }

            SuccessMessage = "Cont creat! Te poti autentifica.";
            await Task.Delay(2000);
            NavigateToLogin();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Inregistrarea a esuat: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NavigateToLogin()
    {
        var loginVm = new LoginViewModel(_authService);
        var loginWindow = new LoginWindow(loginVm);
        loginWindow.Show();
        CloseAction?.Invoke();
    }

    private static int CalculatePasswordStrength(string parola)
    {
        if (string.IsNullOrWhiteSpace(parola))
        {
            return 0;
        }

        var score = 0;
        if (parola.Length >= 8) score += 33;
        if (parola.Any(char.IsUpper)) score += 33;
        if (parola.Any(char.IsDigit)) score += 34;
        return Math.Min(100, score);
    }
}
