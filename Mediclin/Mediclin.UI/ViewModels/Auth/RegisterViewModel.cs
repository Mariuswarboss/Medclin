using System.Windows.Input;
using System.Windows;
using Mediclin.Business.DTOs;
using Mediclin.Business.Services;
using Mediclin.Business.Validators;
using Mediclin.UI.Services;
using Mediclin.UI.Views.Admin;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Doctor;
using Mediclin.UI.Views.Patient;

namespace Mediclin.UI.ViewModels.Auth;

public class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ApplicationServices _app;
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
    private string _rolSelectat = "pacient";
    private bool _isPacientSelected = true;
    private bool _isMedicSelected;
    private bool _isAdminSelected;

    public RegisterViewModel(IAuthService authService, ApplicationServices app)
    {
        _authService = authService;
        _app = app;
        RegisterCommand = new AsyncRelayCommand(async _ => await RegisterAsync(), _ => !IsLoading);
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
                OnPropertyChanged(nameof(IsParolaEmpty));
            }
        }
    }
    public string ConfirmareParola
    {
        get => _confirmareParola;
        set
        {
            if (SetProperty(ref _confirmareParola, value))
            {
                OnPropertyChanged(nameof(IsConfirmareParolaEmpty));
            }
        }
    }
    public string Prenume { get => _prenume; set => SetProperty(ref _prenume, value); }
    public string Nume { get => _nume; set => SetProperty(ref _nume, value); }
    public string Telefon { get => _telefon; set => SetProperty(ref _telefon, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public string SuccessMessage { get => _successMessage; set => SetProperty(ref _successMessage, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public int PasswordStrength { get => _passwordStrength; set => SetProperty(ref _passwordStrength, value); }
    public string RolSelectat { get => _rolSelectat; set => SetProperty(ref _rolSelectat, value); }
    public bool IsParolaEmpty => string.IsNullOrEmpty(Parola);
    public bool IsConfirmareParolaEmpty => string.IsNullOrEmpty(ConfirmareParola);

    public bool IsPacientSelected
    {
        get => _isPacientSelected;
        set
        {
            if (!SetProperty(ref _isPacientSelected, value) || !value) return;
            RolSelectat = "pacient";
            IsMedicSelected = false;
            IsAdminSelected = false;
        }
    }

    public bool IsMedicSelected
    {
        get => _isMedicSelected;
        set
        {
            if (!SetProperty(ref _isMedicSelected, value) || !value) return;
            RolSelectat = "medic";
            IsPacientSelected = false;
            IsAdminSelected = false;
        }
    }

    public bool IsAdminSelected
    {
        get => _isAdminSelected;
        set
        {
            if (!SetProperty(ref _isAdminSelected, value) || !value) return;
            RolSelectat = "admin";
            IsPacientSelected = false;
            IsMedicSelected = false;
        }
    }

    public ICommand RegisterCommand { get; }
    public ICommand NavigateToLoginCommand { get; }

    private async Task RegisterAsync()
    {
        IsLoading = true;
        try
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
                Rol = RolSelectat
            };

            var validationErrors = UserValidator.ValidateRegisterDto(dto);
            if (validationErrors.Count > 0)
            {
                ErrorMessage = string.Join(Environment.NewLine, validationErrors);
                return;
            }

            var result = await _authService.RegisterAsync(dto);
            if (!result.success)
            {
                ErrorMessage = result.error;
                return;
            }

            var loginDto = new LoginDto
            {
                Email = (Email ?? string.Empty).Trim(),
                Parola = Parola ?? string.Empty
            };
            var utilizator = await _authService.LoginAsync(loginDto);

            if (utilizator != null)
            {
                _app.CurrentUserId = utilizator.Id;
                await UserProfileInitializer.EnsureRoleProfileAsync(_app, utilizator);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var rol = (utilizator.Rol ?? string.Empty).Trim().ToLowerInvariant();
                    Window mainWindow = rol switch
                    {
                        "medic" => new DoctorMainWindow(utilizator, _authService, _app),
                        "admin" => new AdminMainWindow(utilizator, _authService, _app),
                        _ => new PatientMainWindow(utilizator, _authService, _app)
                    };
                    mainWindow.Show();

                    foreach (Window w in Application.Current.Windows)
                    {
                        if (w is RegisterWindow)
                        {
                            w.Close();
                            break;
                        }
                    }
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var loginWindow = new LoginWindow(_authService, _app);
                    loginWindow.Show();
                    foreach (Window w in Application.Current.Windows)
                    {
                        if (w is RegisterWindow)
                        {
                            w.Close();
                            break;
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Eroare neașteptată: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NavigateToLogin()
    {
        var loginWindow = new LoginWindow(_authService, _app);
        loginWindow.Show();
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is RegisterWindow)
                {
                    w.Close();
                    break;
                }
            }
        });
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
