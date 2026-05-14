using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class NotificareRepository
{
    private readonly DatabaseContext _db;

    public NotificareRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(Notificare n)
    {
        try
        {
            const string sql = """
                INSERT INTO notificari (utilizator_id, titlu, mesaj, tip, citita, link, creat_la)
                VALUES (@utilizator_id, @titlu, @mesaj, @tip, @citita, @link, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, new Dictionary<string, object>
            {
                ["@utilizator_id"] = n.UtilizatorId,
                ["@titlu"] = n.Titlu,
                ["@mesaj"] = n.Mesaj,
                ["@tip"] = n.Tip,
                ["@citita"] = n.Citita ? 1 : 0,
                ["@link"] = (object?)n.Link ?? DBNull.Value
            });
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-a putut crea notificarea.", ex);
        }
    }

    public Task<int> CreateAsync(int uid, string titlu, string mesaj, string tip, string? link = null)
    {
        return CreateAsync(new Notificare
        {
            UtilizatorId = uid,
            Titlu = titlu,
            Mesaj = mesaj,
            Tip = tip,
            Link = link,
            Citita = false
        });
    }

    public async Task<List<Notificare>> GetUnreadAsync(int utilizatorId, int limit = 20)
    {
        try
        {
            const string sql = """
                SELECT id, utilizator_id, titlu, mesaj, tip, citita, link, creat_la
                FROM notificari
                WHERE utilizator_id=@uid AND citita=0
                ORDER BY creat_la DESC
                LIMIT @lim
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@uid"] = utilizatorId, ["@lim"] = limit });
            return rows.Select(Map).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-au putut încărca notificările necitite.", ex);
        }
    }

    public async Task MarkAllReadAsync(int utilizatorId)
    {
        try
        {
            const string sql = "UPDATE notificari SET citita=1 WHERE utilizator_id=@uid AND citita=0";
            await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@uid"] = utilizatorId });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-au putut marca notificările ca citite.", ex);
        }
    }

    public async Task<int> GetUnreadCountAsync(int utilizatorId)
    {
        try
        {
            const string sql = "SELECT COUNT(*) FROM notificari WHERE utilizator_id=@uid AND citita=0";
            var v = await _db.ScalarAsync(sql, new Dictionary<string, object> { ["@uid"] = utilizatorId });
            return Convert.ToInt32(v);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-a putut număra notificările necitite.", ex);
        }
    }

    private static Notificare Map(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        UtilizatorId = Convert.ToInt32(row["utilizator_id"]),
        Titlu = row["titlu"].ToString() ?? string.Empty,
        Mesaj = row["mesaj"].ToString() ?? string.Empty,
        Tip = row["tip"].ToString() ?? string.Empty,
        Citita = Convert.ToBoolean(row["citita"]),
        Link = row["link"] == DBNull.Value ? null : row["link"].ToString(),
        CreatLa = Convert.ToDateTime(row["creat_la"])
    };
}
