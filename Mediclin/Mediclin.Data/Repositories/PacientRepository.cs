using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class PacientRepository : IRepository<Pacient>
{
    private readonly DatabaseContext _db;

    public PacientRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Pacient>> GetAllAsync()
    {
        try
        {
            const string sql = """
                SELECT p.*, u.prenume, u.nume, u.email, u.telefon,
                       (SELECT s.nume
                        FROM consultatii c
                        INNER JOIN medici m ON m.id = c.medic_id
                        INNER JOIN specialitati s ON s.id = m.specialitate_id
                        WHERE c.pacient_id = p.id
                        ORDER BY c.data_consultatie DESC
                        LIMIT 1) AS specialitate_nume,
                       (SELECT MAX(c.data_consultatie) FROM consultatii c WHERE c.pacient_id = p.id) AS ultima_vizita
                FROM pacienti p
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE u.activ = 1
                ORDER BY u.nume, u.prenume
                """;
            return (await _db.QueryAsync(sql)).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Lista pacienților nu a putut fi încărcată.", ex);
        }
    }

    public async Task<Pacient?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = """
                SELECT p.*, u.prenume, u.nume, u.email, u.telefon,
                       (SELECT s.nume
                        FROM consultatii c
                        INNER JOIN medici m ON m.id = c.medic_id
                        INNER JOIN specialitati s ON s.id = m.specialitate_id
                        WHERE c.pacient_id = p.id
                        ORDER BY c.data_consultatie DESC
                        LIMIT 1) AS specialitate_nume,
                       (SELECT MAX(c.data_consultatie) FROM consultatii c WHERE c.pacient_id = p.id) AS ultima_vizita
                FROM pacienti p
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE p.id = @id
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
            return rows.Count == 0 ? null : MapJoined(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Pacientul nu a putut fi încărcat.", ex);
        }
    }

    public async Task<Pacient?> GetByUtilizatorIdAsync(int utilizatorId)
    {
        try
        {
            const string sql = """
                SELECT p.*, u.prenume, u.nume, u.email, u.telefon,
                       (SELECT s.nume
                        FROM consultatii c
                        INNER JOIN medici m ON m.id = c.medic_id
                        INNER JOIN specialitati s ON s.id = m.specialitate_id
                        WHERE c.pacient_id = p.id
                        ORDER BY c.data_consultatie DESC
                        LIMIT 1) AS specialitate_nume,
                       (SELECT MAX(c.data_consultatie) FROM consultatii c WHERE c.pacient_id = p.id) AS ultima_vizita
                FROM pacienti p
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE p.utilizator_id = @uid
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@uid"] = utilizatorId });
            return rows.Count == 0 ? null : MapJoined(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Profilul pacient nu a putut fi încărcat.", ex);
        }
    }

    public async Task<List<Pacient>> SearchByNameAsync(string query)
    {
        try
        {
            const string sql = """
                SELECT p.*, u.prenume, u.nume, u.email, u.telefon,
                       (SELECT s.nume
                        FROM consultatii c
                        INNER JOIN medici m ON m.id = c.medic_id
                        INNER JOIN specialitati s ON s.id = m.specialitate_id
                        WHERE c.pacient_id = p.id
                        ORDER BY c.data_consultatie DESC
                        LIMIT 1) AS specialitate_nume,
                       (SELECT MAX(c.data_consultatie) FROM consultatii c WHERE c.pacient_id = p.id) AS ultima_vizita
                FROM pacienti p
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE (CONCAT(u.prenume,' ',u.nume) LIKE @q OR u.email LIKE @q)
                  AND u.activ = 1
                ORDER BY u.nume, u.prenume
                LIMIT 200
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@q"] = $"%{query}%" })).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Căutarea pacienților a eșuat.", ex);
        }
    }

    /// <summary>
    /// Returns patients who have had a programare or consultatie with doctors of the given specialty.
    /// Used to restrict each doctor's patient list to their specialty.
    /// </summary>
    public async Task<List<Pacient>> GetByMedicSpecialitateAsync(int specialitateId)
    {
        try
        {
            const string sql = """
                SELECT DISTINCT p.*, u.prenume, u.nume, u.email, u.telefon,
                       (SELECT s2.nume
                        FROM consultatii c2
                        INNER JOIN medici m2 ON m2.id = c2.medic_id
                        INNER JOIN specialitati s2 ON s2.id = m2.specialitate_id
                        WHERE c2.pacient_id = p.id
                        ORDER BY c2.data_consultatie DESC
                        LIMIT 1) AS specialitate_nume,
                       (SELECT MAX(c3.data_consultatie) FROM consultatii c3 WHERE c3.pacient_id = p.id) AS ultima_vizita
                FROM pacienti p
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE u.activ = 1
                  AND p.id IN (
                      SELECT DISTINCT pr.pacient_id
                      FROM programari pr
                      INNER JOIN medici m ON m.id = pr.medic_id
                      WHERE m.specialitate_id = @sid
                      UNION
                      SELECT DISTINCT c.pacient_id
                      FROM consultatii c
                      INNER JOIN medici m ON m.id = c.medic_id
                      WHERE m.specialitate_id = @sid
                  )
                ORDER BY u.nume, u.prenume
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@sid"] = specialitateId })).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Lista pacienților pe specialitate nu a putut fi încărcată.", ex);
        }
    }

    public async Task<int> CreateAsync(Pacient pacient)
    {
        try
        {
            const string sql = """
                INSERT INTO pacienti (utilizator_id, data_nasterii, sex, cnp, grupa_sanguina, adresa, oras,
                    contact_urgenta_nume, contact_urgenta_telefon, contact_urgenta_relatie, medic_de_familie_id, creat_la)
                VALUES (@utilizator_id, @data_nasterii, @sex, @cnp, @grupa_sanguina, @adresa, @oras,
                    @contact_urgenta_nume, @contact_urgenta_telefon, @contact_urgenta_relatie, @medic_de_familie_id, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, Params(pacient));
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Pacientul nu a putut fi creat.", ex);
        }
    }

    public async Task UpdateAsync(Pacient pacient)
    {
        try
        {
            const string sql = """
                UPDATE pacienti
                SET data_nasterii=@data_nasterii, sex=@sex, cnp=@cnp, grupa_sanguina=@grupa_sanguina,
                    adresa=@adresa, oras=@oras, contact_urgenta_nume=@contact_urgenta_nume,
                    contact_urgenta_telefon=@contact_urgenta_telefon, contact_urgenta_relatie=@contact_urgenta_relatie,
                    medic_de_familie_id=@medic_de_familie_id
                WHERE id=@id
                """;
            await _db.ExecuteAsync(sql, Params(pacient));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Pacientul nu a putut fi actualizat.", ex);
        }
    }

    public async Task<List<Alergie>> GetAlergiiAsync(int pacientId)
    {
        try
        {
            var columns = await GetAlergiiColumnsAsync();
            var nameColumn = columns.Contains("substanta") ? "substanta"
                : columns.Contains("denumire") ? "denumire"
                : "id";
            var severityOrder = columns.Contains("severitate") ? "severitate DESC, " : string.Empty;
            var sql = $"SELECT * FROM alergii WHERE pacient_id=@pid ORDER BY {severityOrder}{nameColumn}";
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId })).Select(MapAlergie).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Alergiile nu au putut fi încărcate.", ex);
        }
    }

    public async Task AddAlergieAsync(Alergie a)
    {
        try
        {
            var columns = await GetAlergiiColumnsAsync();
            var nameColumn = columns.Contains("substanta") ? "substanta"
                : columns.Contains("denumire") ? "denumire"
                : throw new InvalidOperationException("Tabela alergii nu are coloana pentru denumirea alergiei.");
            var notesColumn = columns.Contains("observatii") ? "observatii"
                : columns.Contains("simptome") ? "simptome"
                : null;
            var insertColumns = notesColumn is null
                ? $"pacient_id, {nameColumn}, severitate"
                : $"pacient_id, {nameColumn}, severitate, {notesColumn}";
            var insertValues = notesColumn is null
                ? "@pacient_id, @substanta, @severitate"
                : "@pacient_id, @substanta, @severitate, @observatii";
            var sql = $"INSERT INTO alergii ({insertColumns}) VALUES ({insertValues})";
            var parameters = new Dictionary<string, object>
            {
                ["@pacient_id"] = a.PacientId,
                ["@substanta"] = a.Substanta,
                ["@severitate"] = a.Severitate
            };
            if (notesColumn is not null)
            {
                parameters["@observatii"] = (object?)a.Observatii ?? DBNull.Value;
            }

            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            (parameters));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Alergia nu a putut fi adăugată.", ex);
        }
    }

    public async Task<List<BolaCronica>> GetBolicroniceAsync(int pacientId)
    {
        try
        {
            const string sql = "SELECT * FROM boli_cronice WHERE pacient_id=@pid ORDER BY data_diagnostic DESC";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId });
            return rows.Select(MapBoala).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Bolile cronice nu au putut fi încărcate.", ex);
        }
    }

    public Task<List<Dictionary<string, object>>> GetBoliCroniceAsync(int pacientId)
    {
        const string sql = "SELECT * FROM boli_cronice WHERE pacient_id = @pacient_id ORDER BY data_diagnostic DESC";
        return _db.QueryAsync(sql, new Dictionary<string, object> { ["@pacient_id"] = pacientId });
    }

    public Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM pacienti WHERE id = @id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    private static BolaCronica MapBoala(Dictionary<string, object> row)
    {
        var diagnostic = row.ContainsKey("diagnostic") ? row["diagnostic"]?.ToString()
            : row.ContainsKey("denumire") ? row["denumire"]?.ToString() : null;
        return new BolaCronica
        {
            Id = Convert.ToInt32(row["id"]),
            PacientId = Convert.ToInt32(row["pacient_id"]),
            Diagnostic = diagnostic,
            DataDiagnostic = row.ContainsKey("data_diagnostic") && row["data_diagnostic"] != DBNull.Value
                ? Convert.ToDateTime(row["data_diagnostic"])
                : null,
            Observatii = row.ContainsKey("observatii") && row["observatii"] != DBNull.Value ? row["observatii"].ToString() : null
        };
    }

    private async Task<HashSet<string>> GetAlergiiColumnsAsync()
    {
        const string sql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'alergii'
            """;
        var rows = await _db.QueryAsync(sql);
        return rows
            .Select(row => row["COLUMN_NAME"]?.ToString() ?? string.Empty)
            .Where(column => !string.IsNullOrWhiteSpace(column))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Alergie MapAlergie(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        Substanta = ReadText(row, "substanta") ?? ReadText(row, "denumire") ?? string.Empty,
        Severitate = ReadText(row, "severitate") ?? string.Empty,
        Observatii = ReadText(row, "observatii") ?? ReadText(row, "simptome")
    };

    private static string? ReadText(Dictionary<string, object> row, string key)
    {
        return row.TryGetValue(key, out var value) && value != DBNull.Value ? value?.ToString() : null;
    }

    private static Dictionary<string, object> Params(Pacient pacient) => new()
    {
        ["@id"] = pacient.Id,
        ["@utilizator_id"] = pacient.UtilizatorId,
        ["@data_nasterii"] = pacient.DataNasterii?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value,
        ["@sex"] = (object?)pacient.Sex ?? DBNull.Value,
        ["@cnp"] = (object?)pacient.Cnp ?? DBNull.Value,
        ["@grupa_sanguina"] = (object?)pacient.GrupaSanguina ?? DBNull.Value,
        ["@adresa"] = (object?)pacient.Adresa ?? DBNull.Value,
        ["@oras"] = (object?)pacient.Oras ?? DBNull.Value,
        ["@contact_urgenta_nume"] = (object?)pacient.ContactUrgentaNume ?? DBNull.Value,
        ["@contact_urgenta_telefon"] = (object?)pacient.ContactUrgentaTelefon ?? DBNull.Value,
        ["@contact_urgenta_relatie"] = (object?)pacient.ContactUrgentaRelatie ?? DBNull.Value,
        ["@medic_de_familie_id"] = (object?)pacient.MedicDeFamilieId ?? DBNull.Value
    };

    private static Pacient MapJoined(Dictionary<string, object> row)
    {
        var p = MapBase(row);
        p.Prenume = row.TryGetValue("prenume", out var pr) && pr != DBNull.Value ? pr.ToString() : null;
        p.Nume = row.TryGetValue("nume", out var n) && n != DBNull.Value ? n.ToString() : null;
        p.Email = row.TryGetValue("email", out var e) && e != DBNull.Value ? e.ToString() : null;
        p.Telefon = row.TryGetValue("telefon", out var t) && t != DBNull.Value ? t.ToString() : null;
        p.SpecialitateNume = row.TryGetValue("specialitate_nume", out var sn) && sn != DBNull.Value ? sn.ToString() : null;
        p.UltimaVizita = row.TryGetValue("ultima_vizita", out var uv) && uv != DBNull.Value ? Convert.ToDateTime(uv) : null;
        return p;
    }

    private static Pacient MapBase(Dictionary<string, object> row)
    {
        var birth = row["data_nasterii"] == DBNull.Value ? (DateOnly?)null : DateOnly.FromDateTime(Convert.ToDateTime(row["data_nasterii"]));
        return new Pacient
        {
            Id = Convert.ToInt32(row["id"]),
            UtilizatorId = Convert.ToInt32(row["utilizator_id"]),
            DataNasterii = birth,
            Sex = row["sex"] == DBNull.Value ? null : row["sex"].ToString(),
            Cnp = row["cnp"] == DBNull.Value ? null : row["cnp"].ToString(),
            GrupaSanguina = row["grupa_sanguina"] == DBNull.Value ? null : row["grupa_sanguina"].ToString(),
            Adresa = row["adresa"] == DBNull.Value ? null : row["adresa"].ToString(),
            Oras = row["oras"] == DBNull.Value ? null : row["oras"].ToString(),
            ContactUrgentaNume = row["contact_urgenta_nume"] == DBNull.Value ? null : row["contact_urgenta_nume"].ToString(),
            ContactUrgentaTelefon = row["contact_urgenta_telefon"] == DBNull.Value ? null : row["contact_urgenta_telefon"].ToString(),
            ContactUrgentaRelatie = row["contact_urgenta_relatie"] == DBNull.Value ? null : row["contact_urgenta_relatie"].ToString(),
            MedicDeFamilieId = row["medic_de_familie_id"] == DBNull.Value ? null : Convert.ToInt32(row["medic_de_familie_id"]),
            CreatLa = Convert.ToDateTime(row["creat_la"])
        };
    }
}
