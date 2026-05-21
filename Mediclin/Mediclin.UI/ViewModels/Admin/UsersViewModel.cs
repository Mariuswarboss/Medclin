using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public class UsersViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly List<Utilizator> _toti = new();
    private readonly Dictionary<int, string> _roluriPersistente = new();
    private string _cautare = string.Empty;

    public UsersViewModel(ApplicationServices app)
    {
        _app = app;
        Users = new ObservableCollection<Utilizator>();
        RoleOptions = new ObservableCollection<string> { "pacient", "medic", "admin" };
        ActivateUserCommand = new AsyncRelayCommand(async p => await SetActivAsync(p, true));
        DeactivateUserCommand = new AsyncRelayCommand(async p => await SetActivAsync(p, false));
        SaveRoleCommand = new AsyncRelayCommand(async p => await SaveRoleAsync(p));
        AddUserCommand = new RelayCommand(_ => AddUserInfo());
        _ = LoadAsync();
    }

    public ObservableCollection<Utilizator> Users { get; }
    public ObservableCollection<string> RoleOptions { get; }

    public string Cautare
    {
        get => _cautare;
        set
        {
            if (SetProperty(ref _cautare, value))
            {
                ApplyFilter();
            }
        }
    }

    public ICommand ActivateUserCommand { get; }
    public ICommand DeactivateUserCommand { get; }
    public ICommand SaveRoleCommand { get; }
    public ICommand AddUserCommand { get; }

    private static void AddUserInfo()
    {
        MessageBox.Show(
            "Pentru conturi noi folosiți ecranul public „Înregistrează-te” din fereastra de autentificare.\n\nAici puteți activa sau dezactiva utilizatorii existenți.",
            "MediClin — utilizatori",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async Task SetActivAsync(object? p, bool activ)
    {
        if (p is not Utilizator u)
        {
            return;
        }

        if (u.Activ == activ)
        {
            return;
        }

        var action = activ ? "activezi" : "dezactivezi";
        var confirm = MessageBox.Show(
            $"Sigur vrei sa {action} contul {u.NumeComplet} ({u.Email})?",
            "Confirmare modificare utilizator",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _app.Utilizatori.SetActivAsync(u.Id, activ);
            await _app.JurnalRepo.WriteAsync(_app.CurrentUserId, activ ? "ACTIVATE_USER" : "DEACTIVATE_USER", "Utilizatori", $"{u.Email} - {u.NumeComplet}", "127.0.0.1", "Info");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nu s-a putut actualiza statusul: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task SaveRoleAsync(object? p)
    {
        if (p is not Utilizator u)
        {
            return;
        }

        var role = (u.Rol ?? string.Empty).Trim().ToLowerInvariant();
        if (!RoleOptions.Contains(role))
        {
            MessageBox.Show("Rolul selectat nu este valid.", "MediClin - rol utilizator", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _roluriPersistente.TryGetValue(u.Id, out var persistedRole);
        if (string.Equals(persistedRole, role, StringComparison.OrdinalIgnoreCase))
        {
            await UserProfileInitializer.EnsureRoleProfileAsync(_app, u);
            await _app.JurnalRepo.WriteAsync(_app.CurrentUserId, "VERIFY_ROLE_PROFILE", "Utilizatori", $"Profil verificat pentru {u.Email} ({role})", "127.0.0.1", "Info");
            await LoadAsync();
            MessageBox.Show("Rolul este deja salvat. Profilul asociat rolului a fost verificat.", "MediClin - rol utilizator", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Sigur vrei sa schimbi rolul pentru {u.NumeComplet} in {role}?",
            "Confirmare schimbare rol",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            if (!string.IsNullOrWhiteSpace(persistedRole))
            {
                u.Rol = persistedRole;
            }

            ApplyFilter();
            return;
        }

        try
        {
            u.Rol = role;
            await _app.Utilizatori.UpdateAsync(u);
            await UserProfileInitializer.EnsureRoleProfileAsync(_app, u);
            _roluriPersistente[u.Id] = role;
            await _app.JurnalRepo.WriteAsync(_app.CurrentUserId, "CHANGE_USER_ROLE", "Utilizatori", $"{u.Email} -> {role}", "127.0.0.1", "Info");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Rolul nu a putut fi salvat: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _toti.Clear();
            _roluriPersistente.Clear();
            foreach (var x in await _app.Utilizatori.GetAllAsync())
            {
                _toti.Add(x);
                _roluriPersistente[x.Id] = x.Rol;
            }

            ApplyFilter();
        }
        catch
        {
            // ignorat
        }
    }

    private void ApplyFilter()
    {
        var q = (Cautare ?? string.Empty).Trim();
        IEnumerable<Utilizator> src = _toti;
        if (q.Length > 0)
        {
            src = _toti.Where(u =>
                u.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                u.NumeComplet.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (u.Rol ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        Users.Clear();
        foreach (var u in src.OrderByDescending(x => x.CreatLa))
        {
            Users.Add(u);
        }
    }
}
