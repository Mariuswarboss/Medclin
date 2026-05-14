using System.Collections.ObjectModel;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.Views.Dialogs;

namespace Mediclin.UI.ViewModels.Patient;

public class MyAppointmentsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private string _filtru = "Toate";

    public MyAppointmentsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        _utilizator = utilizator;
        Items = new ObservableCollection<Programare>();
        ProgramareNouaCommand = new RelayCommand(_ => DeschideProgramareNoua());
        _ = LoadAsync();
    }

    public string Filtru
    {
        get => _filtru;
        set
        {
            if (SetProperty(ref _filtru, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public ObservableCollection<Programare> Items { get; }
    public ICommand ProgramareNouaCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            var p = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
            if (p is null)
            {
                return;
            }

            var list = await _app.ProgramariRepo.GetByPacientAsync(p.Id);
            IEnumerable<Programare> q = list;
            q = Filtru switch
            {
                "Viitoare" => q.Where(x => x.DataOra >= DateTime.Today && x.Status != "Anulata"),
                "Finalizate" => q.Where(x => x.Status == "Finalizata"),
                "Anulate" => q.Where(x => x.Status == "Anulata"),
                _ => q
            };
            Items.Clear();
            foreach (var x in q)
            {
                Items.Add(x);
            }
        }
        catch
        {
            // ignorat
        }
    }

    private void DeschideProgramareNoua()
    {
        new AddAppointmentDialog().ShowDialog();
        _ = LoadAsync();
    }
}
