using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class ProgramareRepository : IRepository<Programare>
{
    private readonly DatabaseContext _db;

    public ProgramareRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Programare>> GetAllAsync()
    {
        try
        {
            const string sql = "SELECT * FROM programari ORDER BY data_ora DESC";
            return (await _db.QueryAsync(sql)).Select(Map).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programările nu au putut fi încărcate.", ex);
        }
    }

    public async Task<Programare?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = "SELECT * FROM programari WHERE id = @id";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
            return rows.Count == 0 ? null : Map(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programarea nu a putut fi încărcată.", ex);
        }
    }

    public async Task<List<Programare>> GetByMedicDayAsync(int medicId, DateTime date)
    {
        try
        {
            const string sql = """
                SELECT pr.*, CONCAT(u.prenume,' ',u.nume) AS pacient_nume
                FROM programari pr
                INNER JOIN pacienti p ON p.id = pr.pacient_id
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE pr.medic_id = @mid
                  AND DATE(pr.data_ora) = DATE(@data)
                ORDER BY pr.data_ora
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object>
            {
                ["@mid"] = medicId,
                ["@data"] = date.Date
            })).Select(MapWithNume).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programările medicului nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<Programare>> GetByMedicAsync(int medicId, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            const string sql = """
                SELECT pr.*, CONCAT(u.prenume,' ',u.nume) AS pacient_nume
                FROM programari pr
                INNER JOIN pacienti p ON p.id = pr.pacient_id
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE pr.medic_id = @medic_id
                  AND (@from IS NULL OR pr.data_ora >= @from)
                  AND (@to IS NULL OR pr.data_ora <= @to)
                ORDER BY pr.data_ora
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object>
            {
                ["@medic_id"] = medicId,
                ["@from"] = (object?)from ?? DBNull.Value,
                ["@to"] = (object?)to ?? DBNull.Value
            })).Select(MapWithNume).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programările medicului nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<Programare>> GetByPacientAsync(int pacientId)
    {
        try
        {
            const string sql = """
                SELECT pr.*, CONCAT(up.prenume,' ',up.nume) AS pacient_nume,
                       CONCAT(um.prenume,' ',um.nume) AS medic_nume,
                       m.titlu AS medic_titlu,
                       s.nume AS specialitate_nume
                FROM programari pr
                INNER JOIN pacienti p ON p.id = pr.pacient_id
                INNER JOIN utilizatori up ON up.id = p.utilizator_id
                INNER JOIN medici m ON m.id = pr.medic_id
                INNER JOIN utilizatori um ON um.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE pr.pacient_id = @pid
                ORDER BY pr.data_ora DESC
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId })).Select(MapWithNume).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programările pacientului nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<Programare>> GetTodayAsync(int medicId)
    {
        try
        {
            const string sql = """
                SELECT pr.*, CONCAT(u.prenume,' ',u.nume) AS pacient_nume
                FROM programari pr
                INNER JOIN pacienti p ON p.id = pr.pacient_id
                INNER JOIN utilizatori u ON u.id = p.utilizator_id
                WHERE pr.medic_id = @mid AND DATE(pr.data_ora) = CURDATE()
                ORDER BY pr.data_ora
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@mid"] = medicId })).Select(MapWithNume).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programările de astăzi nu au putut fi încărcate.", ex);
        }
    }

    public async Task<List<int>> GetDistinctDaysWithAppointmentsAsync(int medicId, int year, int month)
    {
        try
        {
            const string sql = """
                SELECT DISTINCT DAY(data_ora) AS d
                FROM programari
                WHERE medic_id=@mid AND YEAR(data_ora)=@y AND MONTH(data_ora)=@m
                  AND status NOT IN ('Anulata')
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object>
            {
                ["@mid"] = medicId,
                ["@y"] = year,
                ["@m"] = month
            });
            return rows.Select(r => Convert.ToInt32(r["d"])).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Zilele cu programări nu au putut fi determinate.", ex);
        }
    }

    public async Task<int> CreateAsync(Programare programare)
    {
        try
        {
            const string sql = """
                INSERT INTO programari (pacient_id, medic_id, data_ora, durata_min, tip, status, motiv_vizita, observatii_admin, creat_la, actualizat_la)
                VALUES (@pacient_id, @medic_id, @data_ora, @durata_min, @tip, @status, @motiv_vizita, @observatii_admin, NOW(), NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, Params(programare));
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programarea nu a putut fi creată.", ex);
        }
    }

    public async Task UpdateAsync(Programare programare)
    {
        try
        {
            const string sql = """
                UPDATE programari
                SET pacient_id=@pacient_id, medic_id=@medic_id, data_ora=@data_ora, durata_min=@durata_min,
                    tip=@tip, status=@status, motiv_vizita=@motiv_vizita, observatii_admin=@observatii_admin, actualizat_la=NOW()
                WHERE id=@id
                """;
            await _db.ExecuteAsync(sql, Params(programare));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programarea nu a putut fi actualizată.", ex);
        }
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        try
        {
            const string sql = "UPDATE programari SET status = @status, actualizat_la = NOW() WHERE id = @id";
            await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id, ["@status"] = status });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Statusul programării nu a putut fi actualizat.", ex);
        }
    }

    public async Task<bool> HasOverlapAsync(int medicId, DateTime start, int durMin)
    {
        try
        {
            const string sql = """
                SELECT COUNT(*) FROM programari
                WHERE medic_id=@mid
                  AND status NOT IN ('Anulata','Neprezentata')
                  AND data_ora < DATE_ADD(@start, INTERVAL @dur MINUTE)
                  AND DATE_ADD(data_ora, INTERVAL durata_min MINUTE) > @start
                """;
            var v = await _db.ScalarAsync(sql, new Dictionary<string, object>
            {
                ["@mid"] = medicId,
                ["@start"] = start,
                ["@dur"] = durMin
            });
            return Convert.ToInt32(v) > 0;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Verificarea suprapunerii a eșuat.", ex);
        }
    }

    public async Task CancelAsync(int id)
    {
        try
        {
            const string sql = "UPDATE programari SET status='Anulata', actualizat_la=NOW() WHERE id=@id";
            await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programarea nu a putut fi anulată.", ex);
        }
    }

    public Task CancelAsync(int id, string motiv)
    {
        const string sql = "UPDATE programari SET status='Anulata', observatii_admin=@motiv, actualizat_la=NOW() WHERE id=@id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id, ["@motiv"] = motiv });
    }

    public Task<List<Dictionary<string, object>>> GetWithDetailsAsync(DateTime? date = null)
    {
        const string sql = """
            SELECT pr.*, up.prenume AS pacient_prenume, up.nume AS pacient_nume,
                   um.prenume AS medic_prenume, um.nume AS medic_nume, s.nume AS specialitate,
                   m.tarif_consultatie
            FROM programari pr
            INNER JOIN pacienti p ON p.id = pr.pacient_id
            INNER JOIN utilizatori up ON up.id = p.utilizator_id
            INNER JOIN medici m ON m.id = pr.medic_id
            INNER JOIN utilizatori um ON um.id = m.utilizator_id
            INNER JOIN specialitati s ON s.id = m.specialitate_id
            WHERE (@date IS NULL OR DATE(pr.data_ora) = DATE(@date))
            ORDER BY pr.data_ora
            """;
        return _db.QueryAsync(sql, new Dictionary<string, object> { ["@date"] = (object?)date?.Date ?? DBNull.Value });
    }

    public Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM programari WHERE id = @id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    private static Dictionary<string, object> Params(Programare programare) => new()
    {
        ["@id"] = programare.Id,
        ["@pacient_id"] = programare.PacientId,
        ["@medic_id"] = programare.MedicId,
        ["@data_ora"] = programare.DataOra,
        ["@durata_min"] = programare.DurataMin,
        ["@tip"] = programare.Tip,
        ["@status"] = programare.Status,
        ["@motiv_vizita"] = (object?)programare.MotivVizita ?? DBNull.Value,
        ["@observatii_admin"] = (object?)programare.ObservatiiAdmin ?? DBNull.Value
    };

    private static Programare MapWithNume(Dictionary<string, object> row)
    {
        var p = Map(row);
        if (row.TryGetValue("pacient_nume", out var pn) && pn != DBNull.Value)
        {
            p.PacientNume = pn.ToString();
        }

        if (row.TryGetValue("medic_nume", out var mn) && mn != DBNull.Value)
        {
            p.MedicNume = mn.ToString();
        }

        if (row.TryGetValue("medic_titlu", out var mt) && mt != DBNull.Value)
        {
            p.MedicTitlu = mt.ToString();
        }

        if (row.TryGetValue("specialitate_nume", out var sn) && sn != DBNull.Value)
        {
            p.SpecialitateNume = sn.ToString();
        }

        return p;
    }

    private static Programare Map(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        MedicId = Convert.ToInt32(row["medic_id"]),
        DataOra = Convert.ToDateTime(row["data_ora"]),
        DurataMin = Convert.ToInt32(row["durata_min"]),
        Tip = row["tip"].ToString() ?? string.Empty,
        Status = row["status"].ToString() ?? string.Empty,
        MotivVizita = row["motiv_vizita"] == DBNull.Value ? null : row["motiv_vizita"].ToString(),
        ObservatiiAdmin = row["observatii_admin"] == DBNull.Value ? null : row["observatii_admin"].ToString(),
        CreatLa = Convert.ToDateTime(row["creat_la"]),
        ActualizatLa = Convert.ToDateTime(row["actualizat_la"])
    };
}
