using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.ViewModels;

namespace Mediclin.UI.ViewModels.Dialogs;

public class PrescriptionDialogViewModel : BaseViewModel
{
    private readonly Window _window;
    private string _denumire = string.Empty;
    private string _concentratie = string.Empty;
    private string _forma = "Comprimat";
    private string _cantitate = "1";
    private string _dozaj = string.Empty;
    private string _frecventa = string.Empty;
    private string _durataZile = "7";
    private string _instructiuni = string.Empty;
    private string _observatii = string.Empty;

    public PrescriptionDialogViewModel(Window window)
    {
        _window = window;
        Medicamente = new ObservableCollection<Medicament>();
        FormaOptions = new ObservableCollection<string>
        {
            "Comprimat",
            "Capsula",
            "Sirop",
            "Injectie",
            "Crema",
            "Picaturi",
            "Alt"
        };
        AddMedicamentCommand = new RelayCommand(_ => AddMedicament(), _ => !string.IsNullOrWhiteSpace(Denumire));
        EmitCommand = new RelayCommand(_ => Emit(), _ => Medicamente.Count > 0);
        CancelCommand = new RelayCommand(_ => { _window.DialogResult = false; _window.Close(); });
    }

    public ObservableCollection<Medicament> Medicamente { get; }
    public ObservableCollection<string> FormaOptions { get; }

    public string Denumire
    {
        get => _denumire;
        set
        {
            if (SetProperty(ref _denumire, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Concentratie
    {
        get => _concentratie;
        set => SetProperty(ref _concentratie, value);
    }

    public string Forma
    {
        get => _forma;
        set => SetProperty(ref _forma, value);
    }

    public string Cantitate
    {
        get => _cantitate;
        set => SetProperty(ref _cantitate, value);
    }

    public string Dozaj
    {
        get => _dozaj;
        set => SetProperty(ref _dozaj, value);
    }

    public string Frecventa
    {
        get => _frecventa;
        set => SetProperty(ref _frecventa, value);
    }

    public string DurataZile
    {
        get => _durataZile;
        set => SetProperty(ref _durataZile, value);
    }

    public string Instructiuni
    {
        get => _instructiuni;
        set => SetProperty(ref _instructiuni, value);
    }

    public string Observatii
    {
        get => _observatii;
        set => SetProperty(ref _observatii, value);
    }

    public ICommand AddMedicamentCommand { get; }
    public ICommand EmitCommand { get; }
    public ICommand CancelCommand { get; }

    private void AddMedicament()
    {
        var m = new Medicament
        {
            RetetaId = 0,
            Denumire = Denumire.Trim(),
            Concentratie = string.IsNullOrWhiteSpace(Concentratie) ? null : Concentratie.Trim(),
            Forma = string.IsNullOrWhiteSpace(Forma) ? "Comprimat" : Forma.Trim(),
            Cantitate = int.TryParse(Cantitate, out var c) ? c : 1,
            Dozaj = string.IsNullOrWhiteSpace(Dozaj) ? "-" : Dozaj.Trim(),
            Frecventa = string.IsNullOrWhiteSpace(Frecventa) ? "-" : Frecventa.Trim(),
            DurataZile = int.TryParse(DurataZile, out var d) ? d : null,
            Instructiuni = string.IsNullOrWhiteSpace(_instructiuni) ? null : _instructiuni.Trim()
        };
        Medicamente.Add(m);
        Denumire = string.Empty;
        Concentratie = string.Empty;
        Forma = "Comprimat";
        Cantitate = "1";
        Dozaj = string.Empty;
        Frecventa = string.Empty;
        DurataZile = "7";
        Instructiuni = string.Empty;
        CommandManager.InvalidateRequerySuggested();
    }

    private void Emit()
    {
        _window.DialogResult = true;
        _window.Close();
    }
}
