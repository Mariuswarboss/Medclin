using System.Windows;
using System.Windows.Input;
using Mediclin.Business.DTOs;
using Mediclin.Business.Services;
using Mediclin.UI.Views.Admin;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Doctor;
using Mediclin.UI.Views.Patient;

namespace Mediclin.UI.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _email = string.Empty;
    private string _parola = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoading);
        NavigateToRegisterCommand = new RelayCommand(_ => NavigateToRegister());
    }

    public Action? CloseAction { get; set; }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Parola
    {
        get => _parola;
        set => SetProperty(ref _parola, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoginCommand { get; }
    public ICommand NavigateToRegisterCommand { get; }

    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var utilizator = await _authService.LoginAsync(new LoginDto { Email = Email.Trim(), Parola = Parola });
            if (utilizator is null)
            {
                ErrorMessage = "Email sau parola incorecta";
                return;
            }

            Window fereastra;
            var rol = utilizator.Rol.ToLowerInvariant();
            if (rol == "medic")
            {
                fereastra = new DoctorMainWindow(utilizator, _authService);
            }
            else if (rol == "admin")
            {
                fereastra = new AdminMainWindow(utilizator, _authService);
            }
            else
            {
                fereastra = new PatientMainWindow(utilizator, _authService);
            }

            fereastra.Show();
            CloseAction?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Autentificarea a esuat: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NavigateToRegister()
    {
        var registerVm = new RegisterViewModel(_authService);
        var registerWindow = new RegisterWindow(registerVm);
        registerWindow.Show();
        CloseAction?.Invoke();
    }
}
