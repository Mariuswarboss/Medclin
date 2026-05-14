using System.Collections.ObjectModel;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Admin;

public class UsersViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;

    public UsersViewModel(ApplicationServices app)
    {
        _app = app;
        Users = new ObservableCollection<Utilizator>();
        ToggleActivCommand = new RelayCommand(p => _ = ToggleAsync(p));
        _ = LoadAsync();
    }

    private async Task ToggleAsync(object? p)
    {
        if (p is Utilizator u)
        {
            await _app.Utilizatori.SetActivAsync(u.Id, !u.Activ);
            await LoadAsync();
        }
    }

    public ObservableCollection<Utilizator> Users { get; }
    public ICommand ToggleActivCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            Users.Clear();
            foreach (var u in await _app.Utilizatori.GetAllAsync())
            {
                Users.Add(u);
            }
        }
        catch
        {
            // ignorat
        }
    }
}
