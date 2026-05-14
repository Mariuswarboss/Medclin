using Mediclin.Data.Models;
using Mediclin.Data.Repositories;

namespace Mediclin.Business.Services;

public class NotificareService
{
    private readonly NotificareRepository _repo;
    private readonly JurnalRepository _jurnal;
    private readonly Func<int?> _utilizatorCurentId;

    public NotificareService(NotificareRepository repo, JurnalRepository jurnal, Func<int?> utilizatorCurentId)
    {
        _repo = repo;
        _jurnal = jurnal;
        _utilizatorCurentId = utilizatorCurentId;
    }

    public async Task CreateAsync(int utilizatorId, string titlu, string mesaj, string tip, string? link = null)
    {
        try
        {
            _ = await _repo.CreateAsync(new Notificare
            {
                UtilizatorId = utilizatorId,
                Titlu = titlu,
                Mesaj = mesaj,
                Tip = tip,
                Citita = false,
                Link = link
            });
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "CreateNotificare", "Notificari", ex.Message, "127.0.0.1", "Error");
            throw new InvalidOperationException("Notificarea nu a putut fi creată.", ex);
        }
    }

    public Task<int> CreateNotificareAsync(int utilizatorId, string titlu, string mesaj, string tip, string? link = null)
        => _repo.CreateAsync(new Notificare
        {
            UtilizatorId = utilizatorId,
            Titlu = titlu,
            Mesaj = mesaj,
            Tip = tip,
            Citita = false,
            Link = link
        });

    public Task<List<Notificare>> GetUnreadAsync(int utilizatorId) => _repo.GetUnreadAsync(utilizatorId, 20);

    public Task MarkAllReadAsync(int utilizatorId) => _repo.MarkAllReadAsync(utilizatorId);

    public Task<int> GetUnreadCountAsync(int utilizatorId) => _repo.GetUnreadCountAsync(utilizatorId);
}
