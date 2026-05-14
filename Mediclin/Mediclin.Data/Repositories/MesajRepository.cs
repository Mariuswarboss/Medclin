using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class MesajRepository
{
    private readonly DatabaseContext _db;

    public MesajRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<Conversatie?> GetConversatieAsync(int pacientId, int medicId)
    {
        try
        {
            const string sql = "SELECT * FROM conversatii WHERE pacient_id=@pid AND medic_id=@mid LIMIT 1";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId, ["@mid"] = medicId });
            return rows.Count == 0 ? null : MapConversatie(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Conversația nu a putut fi încărcată.", ex);
        }
    }

    public async Task<int> GetOrCreateConversatieAsync(int pacientId, int medicId)
    {
        try
        {
            var existing = await GetConversatieAsync(pacientId, medicId);
            if (existing is not null)
            {
                return existing.Id;
            }

            const string sql = """
                INSERT INTO conversatii (pacient_id, medic_id, creat_la) VALUES (@pacient_id, @medic_id, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, new Dictionary<string, object> { ["@pacient_id"] = pacientId, ["@medic_id"] = medicId });
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Conversația nu a putut fi creată.", ex);
        }
    }

    public async Task<List<Mesaj>> GetMesajeAsync(int conversatieId)
    {
        try
        {
            const string sql = """
                SELECT m.*, CONCAT(u.prenume,' ',u.nume) AS expeditor_nume
                FROM mesaje m
                INNER JOIN utilizatori u ON u.id = m.expeditor_id
                WHERE m.conversatie_id=@cid
                ORDER BY m.trimis_la ASC
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@cid"] = conversatieId })).Select(MapMesaj).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Mesajele nu au putut fi încărcate.", ex);
        }
    }

    public async Task<int> SendAsync(Mesaj mesaj) => await SendMesajAsync(mesaj);

    public async Task<int> SendMesajAsync(Mesaj mesaj)
    {
        try
        {
            const string sql = """
                INSERT INTO mesaje (expeditor_id, destinatar_id, conversatie_id, continut, tip, fisier_url, citit, citit_la, trimis_la)
                VALUES (@expeditor_id, @destinatar_id, @conversatie_id, @continut, @tip, @fisier_url, 0, NULL, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, new Dictionary<string, object>
            {
                ["@expeditor_id"] = mesaj.ExpeditorId,
                ["@destinatar_id"] = mesaj.DestinatarId,
                ["@conversatie_id"] = mesaj.ConversatieId,
                ["@continut"] = mesaj.Continut,
                ["@tip"] = string.IsNullOrWhiteSpace(mesaj.Tip) ? "Text" : mesaj.Tip,
                ["@fisier_url"] = (object?)mesaj.FisierUrl ?? DBNull.Value
            });
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Mesajul nu a putut fi trimis.", ex);
        }
    }

    public async Task MarkReadAsync(int conversatieId, int utilizatorId)
    {
        try
        {
            const string sql = """
                UPDATE mesaje SET citit=1, citit_la=NOW()
                WHERE conversatie_id=@cid AND destinatar_id=@uid AND citit=0
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@cid"] = conversatieId, ["@uid"] = utilizatorId });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Mesajele nu au putut fi marcate ca citite.", ex);
        }
    }

    public Task MarkAsReadAsync(int conversatieId, int destinatarId) => MarkReadAsync(conversatieId, destinatarId);

    public async Task<int> GetUnreadCountAsync(int utilizatorId)
    {
        try
        {
            const string sql = "SELECT COUNT(*) FROM mesaje WHERE destinatar_id=@uid AND citit=0";
            var count = await _db.ScalarAsync(sql, new Dictionary<string, object> { ["@uid"] = utilizatorId });
            return Convert.ToInt32(count);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Numărul mesajelor necitite nu a putut fi calculat.", ex);
        }
    }

    public async Task<List<ConversatieLista>> GetConversatiiForPacientAsync(int pacientId, int utilizatorDestinatarId)
    {
        try
        {
            const string sql = """
                SELECT c.id, c.pacient_id, c.medic_id, c.creat_la,
                       CONCAT(ud.prenume,' ',ud.nume) AS partener_nume,
                       (SELECT LEFT(m.continut, 140) FROM mesaje m WHERE m.conversatie_id=c.id ORDER BY m.trimis_la DESC LIMIT 1) AS ultim_mesaj,
                       (SELECT m.trimis_la FROM mesaje m WHERE m.conversatie_id=c.id ORDER BY m.trimis_la DESC LIMIT 1) AS ultim_la,
                       (SELECT COUNT(*) FROM mesaje m WHERE m.conversatie_id=c.id AND m.destinatar_id=@uid AND m.citit=0) AS necitite
                FROM conversatii c
                INNER JOIN medici md ON md.id = c.medic_id
                INNER JOIN utilizatori ud ON ud.id = md.utilizator_id
                WHERE c.pacient_id = @pid
                ORDER BY COALESCE((SELECT MAX(m.trimis_la) FROM mesaje m WHERE m.conversatie_id=c.id), c.creat_la) DESC
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId, ["@uid"] = utilizatorDestinatarId });
            return rows.Select(MapLista).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Lista conversațiilor nu a putut fi încărcată.", ex);
        }
    }

    public async Task<List<ConversatieLista>> GetConversatiiForMedicAsync(int medicId, int utilizatorDestinatarId)
    {
        try
        {
            const string sql = """
                SELECT c.id, c.pacient_id, c.medic_id, c.creat_la,
                       CONCAT(up.prenume,' ',up.nume) AS partener_nume,
                       (SELECT LEFT(m.continut, 140) FROM mesaje m WHERE m.conversatie_id=c.id ORDER BY m.trimis_la DESC LIMIT 1) AS ultim_mesaj,
                       (SELECT m.trimis_la FROM mesaje m WHERE m.conversatie_id=c.id ORDER BY m.trimis_la DESC LIMIT 1) AS ultim_la,
                       (SELECT COUNT(*) FROM mesaje m WHERE m.conversatie_id=c.id AND m.destinatar_id=@uid AND m.citit=0) AS necitite
                FROM conversatii c
                INNER JOIN pacienti p ON p.id = c.pacient_id
                INNER JOIN utilizatori up ON up.id = p.utilizator_id
                WHERE c.medic_id = @mid
                ORDER BY COALESCE((SELECT MAX(m.trimis_la) FROM mesaje m WHERE m.conversatie_id=c.id), c.creat_la) DESC
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@mid"] = medicId, ["@uid"] = utilizatorDestinatarId });
            return rows.Select(MapLista).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Lista conversațiilor nu a putut fi încărcată.", ex);
        }
    }

    private static ConversatieLista MapLista(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        MedicId = Convert.ToInt32(row["medic_id"]),
        CreatLa = Convert.ToDateTime(row["creat_la"]),
        PartenerNume = row["partener_nume"].ToString() ?? string.Empty,
        UltimMesajScurt = row.TryGetValue("ultim_mesaj", out var u) && u != DBNull.Value ? u.ToString() : null,
        UltimMesajLa = row.TryGetValue("ultim_la", out var ul) && ul != DBNull.Value ? Convert.ToDateTime(ul) : null,
        Necitite = row.TryGetValue("necitite", out var n) && n != DBNull.Value ? Convert.ToInt32(n) : 0
    };

    private static Conversatie MapConversatie(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        MedicId = Convert.ToInt32(row["medic_id"]),
        CreatLa = Convert.ToDateTime(row["creat_la"])
    };

    private static Mesaj MapMesaj(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        ExpeditorId = Convert.ToInt32(row["expeditor_id"]),
        DestinatarId = Convert.ToInt32(row["destinatar_id"]),
        ConversatieId = Convert.ToInt32(row["conversatie_id"]),
        Continut = row["continut"].ToString() ?? string.Empty,
        Tip = row["tip"].ToString() ?? "Text",
        FisierUrl = row["fisier_url"] == DBNull.Value ? null : row["fisier_url"].ToString(),
        Citit = Convert.ToBoolean(row["citit"]),
        CititLa = row["citit_la"] == DBNull.Value ? null : Convert.ToDateTime(row["citit_la"]),
        TrimisLa = Convert.ToDateTime(row["trimis_la"]),
        ExpeditorNume = row.TryGetValue("expeditor_nume", out var en) && en != DBNull.Value ? en.ToString() : null
    };
}
