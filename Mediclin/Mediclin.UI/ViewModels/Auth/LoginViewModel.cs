using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Markup;
using Mediclin.Business.DTOs;
using Mediclin.Business.Services;
using Mediclin.UI.Services;
using Mediclin.UI.Views.Admin;
using Mediclin.UI.Views.Auth;
using Mediclin.UI.Views.Doctor;
using Mediclin.UI.Views.Patient;

namespace Mediclin.UI.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ApplicationServices _app;
    private string _email = string.Empty;
    private string _parola = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public LoginViewModel(IAuthService authService, ApplicationServices app)
    {
        _authService = authService;
        _app = app;
        LoginCommand = new AsyncRelayCommand(async _ => await LoginAsync(), _ => !IsLoading);
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
        set
        {
            if (SetProperty(ref _parola, value))
            {
                OnPropertyChanged(nameof(IsParolaEmpty));
            }
        }
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

    public bool IsParolaEmpty => string.IsNullOrEmpty(Parola);

    public ICommand LoginCommand { get; }
    public ICommand NavigateToRegisterCommand { get; }

    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var email = (Email ?? string.Empty).Trim();
            var utilizator = await _authService.LoginAsync(new LoginDto { Email = email, Parola = Parola ?? string.Empty });
            if (utilizator is null)
            {
                ErrorMessage = "Email sau parola incorecta.";
                return;
            }

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
                    if (w is LoginWindow)
                    {
                        w.Close();
                        break;
                    }
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Autentificarea a esuat: {BuildErrorDetails(ex)}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NavigateToRegister()
    {
        var registerVm = new RegisterViewModel(_authService, _app);
        var registerWindow = new RegisterWindow(registerVm);
        registerWindow.Show();
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is LoginWindow)
                {
                    w.Close();
                    break;
                }
            }
        });
    }

    private static string BuildErrorDetails(Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        var depth = 0;

        while (current is not null && depth < 6)
        {
            if (depth > 0)
            {
                sb.Append(" | Inner: ");
            }

            sb.Append(current.GetType().Name);
            if (current is XamlParseException xaml)
            {
                sb.Append($" line {xaml.LineNumber}, position {xaml.LinePosition}");
            }

            sb.Append(": ");
            sb.Append(current.Message);
            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }
}
