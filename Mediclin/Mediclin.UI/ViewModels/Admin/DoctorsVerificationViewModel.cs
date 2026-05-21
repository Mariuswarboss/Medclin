using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public class DoctorsVerificationViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;

    public DoctorsVerificationViewModel(ApplicationServices app)
    {
        _app = app;
        InAsteptare = new ObservableCollection<Medic>();
        Verificati = new ObservableCollection<Medic>();
        AprobaCommand = new RelayCommand(p => _ = AprobaAsync(p));
        RespingeCommand = new RelayCommand(p => _ = RespingeAsync(p));
        _ = LoadAsync();
    }

    private async Task AprobaAsync(object? p)
    {
        if (p is Medic m)
        {
            var confirm = MessageBox.Show(
                $"Sigur vrei sa aprobi medicul {m.NumeCompletCuTitlu}?",
                "Confirmare aprobare medic",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            await _app.Medici.SetVerificatAsync(m.Id, true);
            await _app.JurnalRepo.WriteAsync(_app.CurrentUserId, "APPROVE_DOCTOR", "Medici", $"{m.Email} - {m.NumeCompletCuTitlu}", "127.0.0.1", "Info");
            await LoadAsync();
        }
    }

    private async Task RespingeAsync(object? p)
    {
        if (p is Medic m)
        {
            var confirm = MessageBox.Show(
                $"Sigur vrei sa respingi medicul {m.NumeCompletCuTitlu}? Contul asociat va fi dezactivat.",
                "Confirmare respingere medic",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            await _app.Medici.SetVerificatAsync(m.Id, false);
            await _app.Utilizatori.SetActivAsync(m.UtilizatorId, false);
            await _app.JurnalRepo.WriteAsync(_app.CurrentUserId, "REJECT_DOCTOR", "Medici", $"{m.Email} - {m.NumeCompletCuTitlu}", "127.0.0.1", "Warning");
            await LoadAsync();
        }
    }

    public ObservableCollection<Medic> InAsteptare { get; }
    public ObservableCollection<Medic> Verificati { get; }
    public ICommand AprobaCommand { get; }
    public ICommand RespingeCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            foreach (var user in (await _app.Utilizatori.GetAllAsync()).Where(u =>
                         u.Activ && string.Equals(u.Rol?.Trim(), "medic", StringComparison.OrdinalIgnoreCase)))
            {
                await UserProfileInitializer.EnsureRoleProfileAsync(_app, user);
            }

            var toti = await _app.Medici.GetAllAsync();
            InAsteptare.Clear();
            Verificati.Clear();
            foreach (var m in toti.Where(x => !x.Verificat))
            {
                InAsteptare.Add(m);
            }

            foreach (var m in toti.Where(x => x.Verificat))
            {
                Verificati.Add(m);
            }
        }
        catch
        {
            // ignorat
        }
    }
}
