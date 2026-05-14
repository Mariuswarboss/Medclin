using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Mediclin.UI.Views.Dialogs;
using System.Linq;

namespace Mediclin.UI.ViewModels.Doctor;

public class PatientsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Action<Pacient> _openEmr;
    private readonly DispatcherTimer _searchDebounceTimer;
    private string _searchText = string.Empty;
    private string _selectedSpecialitate = "Toți";
    private PatientAnalyticsItem? _selectedPacient;

    public PatientsViewModel(ApplicationServices app, Action<Pacient> openEmr)
    {
        _app = app;
        _openEmr = openEmr;
        AllPacients = new ObservableCollection<PatientAnalyticsItem>();
        FilteredPacients = new ObservableCollection<PatientAnalyticsItem>();
        Specialitati = new ObservableCollection<string> { "Toți" };

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilter();
        };

        OpenEMRCommand = new RelayCommand(OpenEmr, _ => SelectedPacient is not null);
        AddPacientCommand = new RelayCommand(_ =>
        {
            var dialog = new AddPatientDialog(_app)
            {
                Owner = Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                _ = LoadAsync();
            }
        });
        SelectSpecialitateCommand = new RelayCommand(parameter =>
        {
            if (parameter is string specialitate)
            {
                SelectedSpecialitate = specialitate;
            }
        });
        SearchCommand = new RelayCommand(_ => ApplyFilter());

        _ = LoadAsync();
    }

    public ObservableCollection<PatientAnalyticsItem> AllPacients { get; }
    public ObservableCollection<PatientAnalyticsItem> FilteredPacients { get; }
    public ObservableCollection<string> Specialitati { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            }
        }
    }

    public string SelectedSpecialitate
    {
        get => _selectedSpecialitate;
        set
        {
            if (SetProperty(ref _selectedSpecialitate, value))
            {
                ApplyFilter();
            }
        }
    }

    public PatientAnalyticsItem? SelectedPacient
    {
        get => _selectedPacient;
        set => SetProperty(ref _selectedPacient, value);
    }

    public ICommand OpenEMRCommand { get; }
    public ICommand AddPacientCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand SelectSpecialitateCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            Specialitati.Clear();
            Specialitati.Add("Toți");
            foreach (var name in (await _app.Specialitati.GetAllAsync()).Select(x => x.Nume))
            {
                if (!Specialitati.Contains(name))
                {
                    Specialitati.Add(name);
                }
            }

            var pacients = await _app.Pacienti.GetAllAsync();
            var items = await Task.WhenAll(pacients.Select(async pacient =>
            {
                var allergies = await _app.Pacienti.GetAlergiiAsync(pacient.Id);
                return new PatientAnalyticsItem(pacient, allergies);
            }));

            AllPacients.Clear();
            foreach (var item in items.OrderBy(item => item.DisplayName))
            {
                AllPacients.Add(item);
            }

            ApplyFilter();
        }
        catch
        {
            // Lăsăm suprafața existentă neutră dacă datele lipsesc.
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<PatientAnalyticsItem> query = AllPacients;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(item =>
                item.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.Equals(SelectedSpecialitate, "Toți", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(item => string.Equals(item.Specialty, SelectedSpecialitate, StringComparison.OrdinalIgnoreCase));
        }

        FilteredPacients.Clear();
        foreach (var item in query)
        {
            FilteredPacients.Add(item);
        }

        if (SelectedPacient is not null && !FilteredPacients.Contains(SelectedPacient))
        {
            SelectedPacient = FilteredPacients.FirstOrDefault();
        }
    }

    private void OpenEmr(object? parameter)
    {
        var item = parameter as PatientAnalyticsItem ?? SelectedPacient;
        if (item?.Pacient is not null)
        {
            SelectedPacient = item;
            _openEmr(item.Pacient);
        }
    }
}

public class PatientAnalyticsItem : BaseViewModel
{
    private bool _isReadyForReview;
    private bool _needsFollowUp;

    public PatientAnalyticsItem(Pacient pacient, IEnumerable<Alergie> allergies)
    {
        Pacient = pacient;
        var allergyList = allergies.Where(a => !string.IsNullOrWhiteSpace(a.Substanta)).Select(a => a.Substanta.Trim()).ToList();
        AllergySummary = allergyList.Count == 0 ? "Fără alergii active" : string.Join(", ", allergyList.Take(3));
        LastVisitText = pacient.UltimaVizita?.ToString("dd MMM yyyy") ?? "Fără vizite recente";
        IsReadyForReview = pacient.UltimaVizita.HasValue && pacient.UltimaVizita.Value >= DateTime.Now.AddDays(-30);
        NeedsFollowUp = !IsReadyForReview;
    }

    public Pacient Pacient { get; }
    public string DisplayName => Pacient.NumeComplet;
    public string Email => Pacient.Email ?? "Fără email";
    public string Specialty => Pacient.SpecialitateNume ?? "General";
    public string LastVisitText { get; }
    public string AllergySummary { get; }
    public string AgeText => Pacient.DataNasterii.HasValue ? $"{Math.Max(0, DateTime.Today.Year - Pacient.DataNasterii.Value.Year)} ani" : "Vârstă indisponibilă";
    public string Initials => string.Concat((Pacient.NumeComplet ?? "P").Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0])).ToUpperInvariant();

    public bool IsReadyForReview
    {
        get => _isReadyForReview;
        set => SetProperty(ref _isReadyForReview, value);
    }

    public bool NeedsFollowUp
    {
        get => _needsFollowUp;
        set => SetProperty(ref _needsFollowUp, value);
    }
}
