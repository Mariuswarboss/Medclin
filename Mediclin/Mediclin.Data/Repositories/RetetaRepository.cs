using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class RetetaRepository : IRepository<Reteta>
{
    private readonly DatabaseContext _db;
    private static readonly HashSet<string> FormePermise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Comprimat",
        "Capsula",
        "Sirop",
        "Injectie",
        "Crema",
        "Picaturi",
        "Alt"
    };

    public RetetaRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Reteta>> GetAllAsync()
    {
        try
        {
            const string sql = "SELECT * FROM retete ORDER BY data_emitere DESC";
            return (await _db.QueryAsync(sql)).Select(MapPlain).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețetele nu au putut fi încărcate.", ex);
        }
    }

    public async Task<Reteta?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = "SELECT * FROM retete WHERE id = @id";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
            return rows.Count == 0 ? null : MapPlain(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețeta nu a putut fi încărcată.", ex);
        }
    }

    public async Task<List<Reteta>> GetByPacientAsync(int pacientId)
    {
        try
        {
            const string sql = """
                SELECT r.*, CONCAT(u.prenume,' ',u.nume) AS medic_nume
                FROM retete r
                INNER JOIN medici m ON m.id = r.medic_id
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                WHERE r.pacient_id = @pid
                ORDER BY r.data_emitere DESC
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId })).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețetele pacientului nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<Reteta>> GetByConsultatieAsync(int consultatieId)
    {
        try
        {
            const string sql = "SELECT * FROM retete WHERE consultatie_id = @consultatie_id";
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@consultatie_id"] = consultatieId })).Select(MapPlain).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețetele consultației nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<Reteta>> GetActiveAsync(int pacientId)
    {
        try
        {
            const string sql = """
                SELECT r.*, CONCAT(u.prenume,' ',u.nume) AS medic_nume
                FROM retete r
                INNER JOIN medici m ON m.id = r.medic_id
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                WHERE r.pacient_id=@pid AND r.status='Activa' AND r.data_expirare >= CURDATE()
                ORDER BY r.data_emitere DESC
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId })).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețetele active nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<Reteta>> GetActiveExpiredBeforeTodayAsync()
    {
        try
        {
            const string sql = """
                SELECT r.* FROM retete r
                WHERE r.status='Activa' AND r.data_expirare IS NOT NULL AND r.data_expirare < CURDATE()
                """;
            return (await _db.QueryAsync(sql)).Select(MapPlain).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețetele expirate nu au putut fi identificate.", ex);
        }
    }

    public async Task<int> CreateAsync(Reteta reteta)
    {
        try
        {
            const string sql = """
                INSERT INTO retete (consultatie_id, pacient_id, medic_id, data_emitere, data_expirare, status, observatii)
                VALUES (@consultatie_id, @pacient_id, @medic_id, @data_emitere, @data_expirare, @status, @observatii);
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, Params(reteta));
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețeta nu a putut fi creată.", ex);
        }
    }

    public async Task UpdateAsync(Reteta reteta)
    {
        try
        {
            const string sql = "UPDATE retete SET data_expirare=@data_expirare, status=@status, observatii=@observatii WHERE id=@id";
            await _db.ExecuteAsync(sql, Params(reteta));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rețeta nu a putut fi actualizată.", ex);
        }
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        try
        {
            const string sql = "UPDATE retete SET status=@status WHERE id=@id";
            await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id, ["@status"] = status });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Statusul rețetei nu a putut fi actualizat.", ex);
        }
    }

    public async Task<List<Medicament>> GetMedicamenteAsync(int retetaId)
    {
        try
        {
            const string sql = "SELECT * FROM medicamente_reteta WHERE reteta_id=@rid ORDER BY id";
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@rid"] = retetaId })).Select(MapMedicament).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Medicamentele rețetei nu au putut fi încărcate.", ex);
        }
    }

    public async Task AddMedicamentAsync(Medicament medicament)
    {
        try
        {
            const string sql = """
                INSERT INTO medicamente_reteta (reteta_id, denumire, concentratie, forma, cantitate, dozaj, frecventa, durata_zile, instructiuni)
                VALUES (@reteta_id, @denumire, @concentratie, @forma, @cantitate, @dozaj, @frecventa, @durata_zile, @instructiuni)
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            {
                ["@reteta_id"] = medicament.RetetaId,
                ["@denumire"] = medicament.Denumire.Trim(),
                ["@concentratie"] = ToDbText(medicament.Concentratie),
                ["@forma"] = NormalizeForma(medicament.Forma),
                ["@cantitate"] = Math.Max(1, medicament.Cantitate),
                ["@dozaj"] = RequiredText(medicament.Dozaj),
                ["@frecventa"] = RequiredText(medicament.Frecventa),
                ["@durata_zile"] = (object?)medicament.DurataZile ?? DBNull.Value,
                ["@instructiuni"] = ToDbText(medicament.Instructiuni)
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Medicamentul nu a putut fi adăugat pe rețetă.", ex);
        }
    }

    public async Task<int> AddMedicamentAsyncReturnId(Medicament medicament)
    {
        const string sql = """
            INSERT INTO medicamente_reteta (reteta_id, denumire, concentratie, forma, cantitate, dozaj, frecventa, durata_zile, instructiuni)
            VALUES (@reteta_id, @denumire, @concentratie, @forma, @cantitate, @dozaj, @frecventa, @durata_zile, @instructiuni);
            SELECT LAST_INSERT_ID();
            """;
        var id = await _db.ScalarAsync(sql, new Dictionary<string, object>
        {
            ["@reteta_id"] = medicament.RetetaId,
            ["@denumire"] = medicament.Denumire.Trim(),
            ["@concentratie"] = ToDbText(medicament.Concentratie),
            ["@forma"] = NormalizeForma(medicament.Forma),
            ["@cantitate"] = Math.Max(1, medicament.Cantitate),
            ["@dozaj"] = RequiredText(medicament.Dozaj),
            ["@frecventa"] = RequiredText(medicament.Frecventa),
            ["@durata_zile"] = (object?)medicament.DurataZile ?? DBNull.Value,
            ["@instructiuni"] = ToDbText(medicament.Instructiuni)
        });
        return Convert.ToInt32(id);
    }

    public Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM retete WHERE id = @id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    private static Medicament MapMedicament(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        RetetaId = Convert.ToInt32(row["reteta_id"]),
        Denumire = row["denumire"].ToString() ?? string.Empty,
        Concentratie = row["concentratie"] == DBNull.Value ? null : row["concentratie"].ToString(),
        Forma = row["forma"] == DBNull.Value ? null : row["forma"].ToString(),
        Cantitate = Convert.ToInt32(row["cantitate"]),
        Dozaj = row["dozaj"] == DBNull.Value ? null : row["dozaj"].ToString(),
        Frecventa = row["frecventa"] == DBNull.Value ? null : row["frecventa"].ToString(),
        DurataZile = row["durata_zile"] == DBNull.Value ? null : Convert.ToInt32(row["durata_zile"]),
        Instructiuni = row["instructiuni"] == DBNull.Value ? null : row["instructiuni"].ToString()
    };

    private static Dictionary<string, object> Params(Reteta r) => new()
    {
        ["@id"] = r.Id,
        ["@consultatie_id"] = r.ConsultatieId,
        ["@pacient_id"] = r.PacientId,
        ["@medic_id"] = r.MedicId,
        ["@data_emitere"] = r.DataEmitere == default ? DateTime.Today : r.DataEmitere.Date,
        ["@data_expirare"] = (object?)r.DataExpirare?.Date ?? DBNull.Value,
        ["@status"] = r.Status,
        ["@observatii"] = (object?)r.Observatii ?? DBNull.Value
    };

    private static Reteta MapJoined(Dictionary<string, object> row)
    {
        var r = MapPlain(row);
        r.MedicNume = row.TryGetValue("medic_nume", out var mn) && mn != DBNull.Value ? mn.ToString() : null;
        return r;
    }

    private static Reteta MapPlain(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        ConsultatieId = Convert.ToInt32(row["consultatie_id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        MedicId = Convert.ToInt32(row["medic_id"]),
        DataEmitere = Convert.ToDateTime(row["data_emitere"]),
        DataExpirare = row["data_expirare"] == DBNull.Value ? null : Convert.ToDateTime(row["data_expirare"]),
        Status = row["status"].ToString() ?? string.Empty,
        Observatii = row["observatii"] == DBNull.Value ? null : row["observatii"].ToString()
    };
}
