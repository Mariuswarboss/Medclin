using Mediclin.Data.Models;

namespace Mediclin.UI.ViewModels.Doctor;

public class SettingsViewModel : BaseViewModel
{
    public SettingsViewModel(Utilizator utilizator)
    {
        Mesaj = $"Profil: {utilizator.NumeComplet} ({utilizator.Email})";
    }

    public string Mesaj { get; }
}
