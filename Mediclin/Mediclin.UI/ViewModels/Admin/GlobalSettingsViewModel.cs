using System.Threading.Tasks;
using System.Windows.Input;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public class GlobalSettingsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private string _clinicName = "MediClin";
    private string _clinicPhone = string.Empty;
    private int _timeout = 30;
    private int _maxLogin = 5;
    private bool _twoFa;
    private string _apiKey = string.Empty;
    private string _sectiune = "General";
    private string _mesaj = string.Empty;

    public GlobalSettingsViewModel(ApplicationServices app)
    {
        _app = app;
        SalveazaGeneralCommand = new RelayCommand(_ => _ = SalveazaGeneralAsync());
        SalveazaSecuritateCommand = new RelayCommand(_ => _ = SalveazaSecuritateAsync());
        RegenereazaApiCommand = new RelayCommand(_ => _ = RegenereazaApiAsync());
        CopiazaApiCommand = new RelayCommand(_ => System.Windows.Clipboard.SetText(ApiKey));
        SchimbaSectiuneCommand = new RelayCommand(p =>
        {
            if (p is string s)
            {
                Sectiune = s;
                Mesaj = string.Empty;
            }
        });
        _ = IncarcaAsync();
    }

    public string ClinicName
    {
        get => _clinicName;
        set => SetProperty(ref _clinicName, value);
    }

    public string ClinicPhone
    {
        get => _clinicPhone;
        set => SetProperty(ref _clinicPhone, value);
    }

    public int TimeoutMinute
    {
        get => _timeout;
        set => SetProperty(ref _timeout, value);
    }

    public int MaxLogin
    {
        get => _maxLogin;
        set => SetProperty(ref _maxLogin, value);
    }

    public bool TwoFa
    {
        get => _twoFa;
        set => SetProperty(ref _twoFa, value);
    }

    public string ApiKey
    {
        get => _apiKey;
        set => SetProperty(ref _apiKey, value);
    }

    public string Sectiune
    {
        get => _sectiune;
        set => SetProperty(ref _sectiune, value);
    }

    public string Mesaj
    {
        get => _mesaj;
        set => SetProperty(ref _mesaj, value);
    }

    public ICommand SalveazaGeneralCommand { get; }
    public ICommand SalveazaSecuritateCommand { get; }
    public ICommand RegenereazaApiCommand { get; }
    public ICommand CopiazaApiCommand { get; }
    public ICommand SchimbaSectiuneCommand { get; }

    private async Task IncarcaAsync()
    {
        try
        {
            ClinicName = await _app.Setari.GetValoareAsync("clinic_name") ?? ClinicName;
            ClinicPhone = await _app.Setari.GetValoareAsync("clinic_phone") ?? string.Empty;
            ApiKey = await _app.Setari.GetValoareAsync("api_key") ?? string.Empty;
        }
        catch
        {
            // ignorat
        }
    }

    private async Task SalveazaGeneralAsync()
    {
        await _app.Setari.SetValoareAsync("clinic_name", ClinicName);
        await _app.Setari.SetValoareAsync("clinic_phone", ClinicPhone);
        Mesaj = "Setările generale au fost salvate.";
    }

    private async Task SalveazaSecuritateAsync()
    {
        await _app.Setari.SetValoareAsync("session_timeout_min", TimeoutMinute.ToString());
        await _app.Setari.SetValoareAsync("max_login_attempts", MaxLogin.ToString());
        await _app.Setari.SetValoareAsync("two_fa_required", TwoFa ? "1" : "0");
        Mesaj = "Setările de securitate au fost salvate.";
    }

    private async Task RegenereazaApiAsync()
    {
        ApiKey = Guid.NewGuid().ToString("N");
        await _app.Setari.SetValoareAsync("api_key", ApiKey);
        OnPropertyChanged(nameof(ApiKey));
    }
}
