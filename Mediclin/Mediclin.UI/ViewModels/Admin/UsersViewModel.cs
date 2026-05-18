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
    private string _cautare = string.Empty;

    public UsersViewModel(ApplicationServices app)
    {
        _app = app;
        Users = new ObservableCollection<Utilizator>();
        ActivateUserCommand = new AsyncRelayCommand(async p => await SetActivAsync(p, true));
        DeactivateUserCommand = new AsyncRelayCommand(async p => await SetActivAsync(p, false));
        AddUserCommand = new RelayCommand(_ => AddUserInfo());
        _ = LoadAsync();
    }

    public ObservableCollection<Utilizator> Users { get; }

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

        try
        {
            await _app.Utilizatori.SetActivAsync(u.Id, activ);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nu s-a putut actualiza statusul: {ex.Message}", "Eroare", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _toti.Clear();
            foreach (var x in await _app.Utilizatori.GetAllAsync())
            {
                _toti.Add(x);
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
