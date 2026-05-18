using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public sealed class MesajChatItem
{
    public MesajChatItem(string text, string timeLabel, bool isMine, string? senderLabel = null)
    {
        Text = text;
        TimeLabel = timeLabel;
        IsMine = isMine;
        SenderLabel = senderLabel;
    }

    public string Text { get; }
    public string TimeLabel { get; }
    public bool IsMine { get; }
    public string? SenderLabel { get; }
}

public class MessagesViewModel : BaseViewModel, IDisposable
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private readonly bool _esteMedic;
    private ConversatieLista? _selectata;
    private string _text = string.Empty;
    private readonly DispatcherTimer _timer;
    private Medic? _medicNou;
    private Pacient? _pacientNou;
    private bool _disposed;

    public MessagesViewModel(ApplicationServices app, Utilizator utilizator, bool esteMedic)
    {
        _app = app;
        _utilizator = utilizator;
        _esteMedic = esteMedic;
        Conversatii = new ObservableCollection<ConversatieLista>();
        ChatItems = new ObservableCollection<MesajChatItem>();
        MediciPentruNou = new ObservableCollection<Medic>();
        PacientiPentruNou = new ObservableCollection<Pacient>();

        SendCommand = new RelayCommand(_ => _ = SendAsync(), _ => Selectata is not null && !string.IsNullOrWhiteSpace(Text));
        StartConversatieNouaCommand = new AsyncRelayCommand(async _ => await StartConversatieNouaAsync(), _ => !IsStartingConversation);

        _ = LoadInitialAsync();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private async void OnTimerTick(object? sender, EventArgs e) => await RefreshMesajeAsync();

    public bool EsteMedic => _esteMedic;

    public ObservableCollection<ConversatieLista> Conversatii { get; }
    public ObservableCollection<MesajChatItem> ChatItems { get; }
    public ObservableCollection<Medic> MediciPentruNou { get; }
    public ObservableCollection<Pacient> PacientiPentruNou { get; }

    public ConversatieLista? Selectata
    {
        get => _selectata;
        set
        {
            if (SetProperty(ref _selectata, value))
            {
                OnPropertyChanged(nameof(HasConversation));
                _ = LoadMesajeAsync();
            }
        }
    }

    public bool HasConversation => Selectata is not null;

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public Medic? MedicNou
    {
        get => _medicNou;
        set => SetProperty(ref _medicNou, value);
    }

    public Pacient? PacientNou
    {
        get => _pacientNou;
        set => SetProperty(ref _pacientNou, value);
    }

    private bool _isStartingConversation;
    public bool IsStartingConversation
    {
        get => _isStartingConversation;
        set => SetProperty(ref _isStartingConversation, value);
    }

    public ICommand SendCommand { get; }
    public ICommand StartConversatieNouaCommand { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private async Task LoadInitialAsync()
    {
        try
        {
            if (!_esteMedic)
            {
                MediciPentruNou.Clear();
                foreach (var m in await _app.Medici.GetAllAsync())
                {
                    MediciPentruNou.Add(m);
                }
            }
            else
            {
                PacientiPentruNou.Clear();
                foreach (var p in await _app.Pacienti.GetAllAsync())
                {
                    PacientiPentruNou.Add(p);
                }
            }

            await LoadConversatiiAsync();
        }
        catch
        {
            // ignorat
        }
    }

    private async Task LoadConversatiiAsync()
    {
        try
        {
            Conversatii.Clear();
            if (_esteMedic)
            {
                var m = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
                if (m is null)
                {
                    return;
                }

                foreach (var c in await _app.MesajeRepo.GetConversatiiForMedicAsync(m.Id, _utilizator.Id))
                {
                    Conversatii.Add(c);
                }
            }
            else
            {
                var p = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
                if (p is null)
                {
                    return;
                }

                foreach (var c in await _app.MesajeRepo.GetConversatiiForPacientAsync(p.Id, _utilizator.Id))
                {
                    Conversatii.Add(c);
                }
            }

            Selectata ??= Conversatii.FirstOrDefault();
        }
        catch
        {
            // ignorat
        }
    }

    private async Task StartConversatieNouaAsync()
    {
        if (_disposed)
        {
            return;
        }

        IsStartingConversation = true;
        try
        {
            if (_esteMedic)
            {
                var med = await _app.Medici.GetByUtilizatorIdAsync(_utilizator.Id);
                if (med is null || PacientNou is null)
                {
                    return;
                }

                _ = await _app.MesajeRepo.GetOrCreateConversatieAsync(PacientNou.Id, med.Id);
            }
            else
            {
                var p = await _app.Pacienti.GetByUtilizatorIdAsync(_utilizator.Id);
                if (p is null || MedicNou is null)
                {
                    return;
                }

                _ = await _app.MesajeRepo.GetOrCreateConversatieAsync(p.Id, MedicNou.Id);
            }

            await LoadConversatiiAsync();
            if (_esteMedic && PacientNou is not null)
            {
                Selectata = Conversatii.FirstOrDefault(c => c.PacientId == PacientNou.Id);
            }
            else if (!_esteMedic && MedicNou is not null)
            {
                Selectata = Conversatii.FirstOrDefault(c => c.MedicId == MedicNou.Id);
            }
            else
            {
                Selectata = Conversatii.FirstOrDefault();
            }
        }
        catch
        {
            // ignorat
        }
        finally
        {
            IsStartingConversation = false;
        }
    }

    private async Task LoadMesajeAsync()
    {
        if (Selectata is null)
        {
            ChatItems.Clear();
            return;
        }

        try
        {
            ChatItems.Clear();
            foreach (var m in await _app.MesajeRepo.GetMesajeAsync(Selectata.Id))
            {
                var mine = m.ExpeditorId == _utilizator.Id;
                var label = m.TrimisLa.ToString("dd.MM HH:mm");
        var who = mine ? null : (string.IsNullOrWhiteSpace(m.ExpeditorNume) ? null : m.ExpeditorNume);
                ChatItems.Add(new MesajChatItem(m.Continut, label, mine, who));
            }

            await _app.MesajeRepo.MarkReadAsync(Selectata.Id, _utilizator.Id);
        }
        catch
        {
            // ignorat
        }
    }

    private async Task RefreshMesajeAsync()
    {
        if (Selectata is null || _disposed)
        {
            return;
        }

        try
        {
            ChatItems.Clear();
            foreach (var m in await _app.MesajeRepo.GetMesajeAsync(Selectata.Id))
            {
                var mine = m.ExpeditorId == _utilizator.Id;
                var label = m.TrimisLa.ToString("dd.MM HH:mm");
        var who = mine ? null : (string.IsNullOrWhiteSpace(m.ExpeditorNume) ? null : m.ExpeditorNume);
                ChatItems.Add(new MesajChatItem(m.Continut, label, mine, who));
            }
        }
        catch
        {
            // ignorat
        }
    }

    private async Task SendAsync()
    {
        if (Selectata is null)
        {
            return;
        }

        try
        {
            var destinatarId = await ResolveDestinatarAsync();
            if (destinatarId <= 0)
            {
                return;
            }

            var mesaj = new Mesaj
            {
                ExpeditorId = _utilizator.Id,
                DestinatarId = destinatarId,
                ConversatieId = Selectata.Id,
                Continut = Text.Trim(),
                Tip = "Text"
            };
            await _app.MesajeRepo.SendAsync(mesaj);
            Text = string.Empty;
            await LoadMesajeAsync();
            await LoadConversatiiAsync();
        }
        catch
        {
            // ignorat
        }
    }

    private async Task<int> ResolveDestinatarAsync()
    {
        if (Selectata is null)
        {
            return 0;
        }

        if (_esteMedic)
        {
            var p = await _app.Pacienti.GetByIdAsync(Selectata.PacientId);
            return p?.UtilizatorId ?? 0;
        }

        var m = await _app.Medici.GetByIdAsync(Selectata.MedicId);
        return m is null ? 0 : (await _app.Utilizatori.GetByIdAsync(m.UtilizatorId))?.Id ?? 0;
    }
}
