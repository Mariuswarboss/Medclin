using System.Collections.ObjectModel;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;

namespace Mediclin.UI.ViewModels;

public class NotificationsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private readonly Utilizator _utilizator;
    private readonly Action<int>? _unreadCountChanged;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private int _unreadCount;

    public NotificationsViewModel(ApplicationServices app, Utilizator utilizator, Action<int>? unreadCountChanged = null)
    {
        _app = app;
        _utilizator = utilizator;
        _unreadCountChanged = unreadCountChanged;
        Notifications = new ObservableCollection<NotificationItemViewModel>();
        RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
        MarkReadCommand = new AsyncRelayCommand(async p => await MarkReadAsync(p as NotificationItemViewModel));
        MarkAllReadCommand = new AsyncRelayCommand(_ => MarkAllReadAsync(), _ => UnreadCount > 0 && !IsLoading);
        _ = LoadAsync();
    }

    public ObservableCollection<NotificationItemViewModel> Notifications { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (SetProperty(ref _unreadCount, value))
            {
                OnPropertyChanged(nameof(UnreadSummary));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsEmpty => Notifications.Count == 0;
    public string UnreadSummary => UnreadCount == 0 ? "Toate notificarile sunt citite" : $"{UnreadCount} notificari necitite";

    public ICommand RefreshCommand { get; }
    public ICommand MarkReadCommand { get; }
    public ICommand MarkAllReadCommand { get; }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = string.Empty;
            Notifications.Clear();

            var list = await _app.NotificariRepo.GetByUtilizatorAsync(_utilizator.Id);
            foreach (var notification in list)
            {
                Notifications.Add(new NotificationItemViewModel(notification));
            }

            UnreadCount = Notifications.Count(n => n.IsUnread);
            _unreadCountChanged?.Invoke(UnreadCount);
            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Notificarile nu au putut fi incarcate: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task MarkReadAsync(NotificationItemViewModel? item)
    {
        if (item is null || !item.IsUnread)
        {
            return;
        }

        try
        {
            await _app.NotificariRepo.MarkReadAsync(item.Id, _utilizator.Id);
            item.Citita = true;
            UnreadCount = Math.Max(0, UnreadCount - 1);
            _unreadCountChanged?.Invoke(UnreadCount);
            StatusMessage = "Notificarea a fost marcata ca citita.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Nu s-a putut actualiza notificarea: {ex.Message}";
        }
    }

    private async Task MarkAllReadAsync()
    {
        try
        {
            await _app.NotificariRepo.MarkAllReadAsync(_utilizator.Id);
            foreach (var notification in Notifications)
            {
                notification.Citita = true;
            }

            UnreadCount = 0;
            _unreadCountChanged?.Invoke(UnreadCount);
            StatusMessage = "Toate notificarile au fost marcate ca citite.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Nu s-au putut marca notificarile: {ex.Message}";
        }
    }
}

public class NotificationItemViewModel : BaseViewModel
{
    private bool _citita;

    public NotificationItemViewModel(Notificare notificare)
    {
        Id = notificare.Id;
        Titlu = notificare.Titlu;
        Mesaj = notificare.Mesaj;
        Tip = notificare.Tip;
        Link = notificare.Link;
        CreatLa = notificare.CreatLa;
        _citita = notificare.Citita;
    }

    public int Id { get; }
    public string Titlu { get; }
    public string Mesaj { get; }
    public string Tip { get; }
    public string? Link { get; }
    public DateTime CreatLa { get; }

    public bool Citita
    {
        get => _citita;
        set
        {
            if (SetProperty(ref _citita, value))
            {
                OnPropertyChanged(nameof(IsUnread));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public bool IsUnread => !Citita;
    public string StatusText => Citita ? "Citita" : "Noua";
    public string CreatedText => CreatLa.ToString("dd.MM.yyyy HH:mm");
}
