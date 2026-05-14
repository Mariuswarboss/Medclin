using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels.Patient;

public class MessagesViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private readonly bool _esteMedic;
    private ConversatieLista? _selectata;
    private string _text = string.Empty;
    private readonly DispatcherTimer _timer;

    public MessagesViewModel(ApplicationServices app, Utilizator utilizator, bool esteMedic)
    {
        _app = app;
        _utilizator = utilizator;
        _esteMedic = esteMedic;
        Conversatii = new ObservableCollection<ConversatieLista>();
        CurrentMessages = new ObservableCollection<Mesaj>();
        SendCommand = new RelayCommand(_ => _ = SendAsync(), _ => Selectata is not null && !string.IsNullOrWhiteSpace(Text));
        _ = LoadConversatiiAsync();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timer.Tick += async (_, _) => await RefreshMesajeAsync();
        _timer.Start();
    }

    public ObservableCollection<ConversatieLista> Conversatii { get; }
    public ObservableCollection<Mesaj> CurrentMessages { get; }

    public ConversatieLista? Selectata
    {
        get => _selectata;
        set
        {
            if (SetProperty(ref _selectata, value))
            {
                _ = LoadMesajeAsync();
            }
        }
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public ICommand SendCommand { get; }

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

            Selectata = Conversatii.FirstOrDefault();
        }
        catch
        {
            // ignorat
        }
    }

    private async Task LoadMesajeAsync()
    {
        if (Selectata is null)
        {
            return;
        }

        try
        {
            CurrentMessages.Clear();
            foreach (var m in await _app.MesajeRepo.GetMesajeAsync(Selectata.Id))
            {
                CurrentMessages.Add(m);
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
        if (Selectata is null)
        {
            return;
        }

        try
        {
            CurrentMessages.Clear();
            foreach (var m in await _app.MesajeRepo.GetMesajeAsync(Selectata.Id))
            {
                CurrentMessages.Add(m);
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
