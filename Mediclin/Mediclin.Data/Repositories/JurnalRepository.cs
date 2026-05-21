using Mediclin.Data.Context;

namespace Mediclin.Data.Repositories;

public class JurnalRepository
{
    private readonly DatabaseContext _db;

    public JurnalRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task LogAsync(string actiune, string modul, string detalii, string severitate, int? utilizatorId = null)
    {
        try
        {
            const string sql = """
                INSERT INTO jurnal_activitate (utilizator_id, actiune, modul, detalii, severitate, creat_la)
                VALUES (@uid, @a, @m, JSON_OBJECT('message', @d), @sev, NOW())
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            {
                ["@uid"] = (object?)utilizatorId ?? DBNull.Value,
                ["@a"] = actiune,
                ["@m"] = modul,
                ["@d"] = detalii.Length > 4000 ? detalii[..4000] : detalii,
                ["@sev"] = severitate
            });
        }
        catch
        {
            // Jurnalizarea nu trebuie să blocheze fluxul principal.
        }
    }

    public async Task WriteAsync(int? utilizatorId, string actiune, string modul, string detalii, string ipAdresa, string severitate)
    {
        try
        {
            const string sql = """
                INSERT INTO jurnal_activitate (utilizator_id, actiune, modul, detalii, ip_adresa, severitate, creat_la)
                VALUES (@utilizator_id, @actiune, @modul, JSON_OBJECT('message', @detalii), @ip_adresa, @severitate, NOW())
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            {
                ["@utilizator_id"] = (object?)utilizatorId ?? DBNull.Value,
                ["@actiune"] = actiune,
                ["@modul"] = modul,
                ["@detalii"] = detalii.Length > 4000 ? detalii[..4000] : detalii,
                ["@ip_adresa"] = ipAdresa,
                ["@severitate"] = severitate
            });
        }
        catch
        {
            // nu propagăm — jurnalul nu trebuie să blocheze fluxul principal
        }
    }

    public async Task<List<Dictionary<string, object>>> GetRecentAsync(int count = 10)
    {
        try
        {
            const string sql = """
                SELECT j.id, j.creat_la, j.severitate, j.actiune, j.modul, j.detalii, j.ip_adresa,
                       COALESCE(CONCAT(u.prenume,' ',u.nume), 'Sistem') AS utilizator_nume
                FROM jurnal_activitate j
                LEFT JOIN utilizatori u ON u.id = j.utilizator_id
                ORDER BY j.creat_la DESC
                LIMIT @c
                """;
            return await _db.QueryAsync(sql, new Dictionary<string, object> { ["@c"] = count });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-a putut citi jurnalul de activitate.", ex);
        }
    }

    public async Task<(List<Dictionary<string, object>> rows, int total)> GetPagedAsync(
        int page,
        int pageSize,
        string? severitate,
        string? search)
    {
        try
        {
            var off = Math.Max(0, (page - 1) * pageSize);
            const string whereBase = """
                WHERE (@sev IS NULL OR j.severitate = @sev)
                  AND (@q IS NULL OR j.actiune LIKE @q OR j.modul LIKE @q OR j.detalii LIKE @q
                       OR CONCAT(COALESCE(u.prenume,''),' ',COALESCE(u.nume,'')) LIKE @q)
                """;
            var qParam = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var p = new Dictionary<string, object>
            {
                ["@sev"] = string.IsNullOrWhiteSpace(severitate) ? DBNull.Value : severitate!,
                ["@q"] = qParam is null ? DBNull.Value : qParam,
                ["@lim"] = pageSize,
                ["@off"] = off
            };

            var countSql = $"""
                SELECT COUNT(*) FROM jurnal_activitate j
                LEFT JOIN utilizatori u ON u.id = j.utilizator_id
                {whereBase}
                """;
            var total = Convert.ToInt32(await _db.ScalarAsync(countSql, p));

            var listSql = $"""
                SELECT j.id, j.creat_la, j.severitate, j.actiune, j.modul, j.detalii, j.ip_adresa,
                       COALESCE(CONCAT(u.prenume,' ',u.nume), 'Sistem') AS utilizator_nume
                FROM jurnal_activitate j
                LEFT JOIN utilizatori u ON u.id = j.utilizator_id
                {whereBase}
                ORDER BY j.creat_la DESC
                LIMIT @lim OFFSET @off
                """;
            var rows = await _db.QueryAsync(listSql, p);
            return (rows, total);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-a putut citi jurnalul (paginare).", ex);
        }
    }
}
