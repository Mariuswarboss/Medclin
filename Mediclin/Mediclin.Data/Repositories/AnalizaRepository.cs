using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class AnalizaRepository
{
    private readonly DatabaseContext _db;

    public AnalizaRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<RezultatAnaliza>> GetByPacientAsync(int pacientId)
    {
        try
        {
            const string sql = """
                SELECT ra.*, COALESCE(x.nr_valori,0) AS nr_valori, COALESCE(x.anormale,0) AS anormale
                FROM rezultate_analize ra
                LEFT JOIN (
                    SELECT rezultat_id,
                           COUNT(*) AS nr_valori,
                           SUM(CASE WHEN status != 'Normal' THEN 1 ELSE 0 END) AS anormale
                    FROM valori_analize
                    GROUP BY rezultat_id
                ) x ON x.rezultat_id = ra.id
                WHERE ra.pacient_id=@pid
                ORDER BY ra.data_recoltare DESC
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId });
            return rows.Select(MapAgg).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rezultatele analizelor nu au putut fi încărcate.", ex);
        }
    }

    public async Task<RezultatAnaliza?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = "SELECT * FROM rezultate_analize WHERE id=@id";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
            return rows.Count == 0 ? null : MapPlain(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rezultatul analizei nu a putut fi încărcat.", ex);
        }
    }

    public async Task<List<ValoareAnaliza>> GetValoriAsync(int rezultatId)
    {
        try
        {
            const string sql = "SELECT * FROM valori_analize WHERE rezultat_id=@rid ORDER BY test_nume";
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@rid"] = rezultatId })).Select(MapValoare).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Valorile analizei nu au putut fi încărcate.", ex);
        }
    }

    public async Task<int> CreateAsync(RezultatAnaliza analiza)
    {
        try
        {
            const string sql = """
                INSERT INTO rezultate_analize (pacient_id, consultatie_id, data_recoltare, data_rezultat, laborator, interpretare, pdf_url, creat_la)
                VALUES (@pacient_id, @consultatie_id, @data_recoltare, @data_rezultat, @laborator, @interpretare, @pdf_url, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, new Dictionary<string, object>
            {
                ["@pacient_id"] = analiza.PacientId,
                ["@consultatie_id"] = (object?)analiza.ConsultatieId ?? DBNull.Value,
                ["@data_recoltare"] = analiza.DataRecoltare.Date,
                ["@data_rezultat"] = (object?)analiza.DataRezultat?.Date ?? DBNull.Value,
                ["@laborator"] = (object?)analiza.Laborator ?? DBNull.Value,
                ["@interpretare"] = (object?)analiza.Interpretare ?? DBNull.Value,
                ["@pdf_url"] = (object?)analiza.PdfUrl ?? DBNull.Value
            });
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Rezultatul analizei nu a putut fi creat.", ex);
        }
    }

    public async Task AddValoareAsync(ValoareAnaliza v)
    {
        try
        {
            const string sql = """
                INSERT INTO valori_analize (rezultat_id, test_nume, valoare, unitate, val_min_normal, val_max_normal, status)
                VALUES (@rezultat_id, @test_nume, @valoare, @unitate, @val_min_normal, @val_max_normal, @status)
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            {
                ["@rezultat_id"] = v.RezultatId,
                ["@test_nume"] = v.TestNume,
                ["@valoare"] = v.Valoare,
                ["@unitate"] = (object?)v.Unitate ?? DBNull.Value,
                ["@val_min_normal"] = (object?)v.ValMinNormal ?? DBNull.Value,
                ["@val_max_normal"] = (object?)v.ValMaxNormal ?? DBNull.Value,
                ["@status"] = v.Status
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Valoarea analizei nu a putut fi adăugată.", ex);
        }
    }

    public async Task<int> AddValoareAsync(int rezultatId, string testNume, string valoare, string? unitate, decimal? min, decimal? max, string status)
    {
        var v = new ValoareAnaliza
        {
            RezultatId = rezultatId,
            TestNume = testNume,
            Valoare = valoare,
            Unitate = unitate,
            ValMinNormal = min,
            ValMaxNormal = max,
            Status = status
        };
        await AddValoareAsync(v);
        return 0;
    }

    public Task<List<Dictionary<string, object>>> GetWithValoriAsync(int rezultatId)
    {
        const string sql = """
            SELECT r.*, v.id AS valoare_id, v.test_nume, v.valoare, v.unitate, v.val_min_normal, v.val_max_normal, v.status AS valoare_status
            FROM rezultate_analize r
            LEFT JOIN valori_analize v ON v.rezultat_id = r.id
            WHERE r.id = @id
            ORDER BY v.test_nume
            """;
        return _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = rezultatId });
    }

    private static ValoareAnaliza MapValoare(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        RezultatId = Convert.ToInt32(row["rezultat_id"]),
        TestNume = row["test_nume"].ToString() ?? string.Empty,
        Valoare = row["valoare"].ToString() ?? string.Empty,
        Unitate = row["unitate"] == DBNull.Value ? null : row["unitate"].ToString(),
        ValMinNormal = row["val_min_normal"] == DBNull.Value ? null : Convert.ToDecimal(row["val_min_normal"]),
        ValMaxNormal = row["val_max_normal"] == DBNull.Value ? null : Convert.ToDecimal(row["val_max_normal"]),
        Status = row["status"].ToString() ?? string.Empty
    };

    private static RezultatAnaliza MapPlain(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        ConsultatieId = row["consultatie_id"] == DBNull.Value ? null : Convert.ToInt32(row["consultatie_id"]),
        DataRecoltare = Convert.ToDateTime(row["data_recoltare"]),
        DataRezultat = row["data_rezultat"] == DBNull.Value ? null : Convert.ToDateTime(row["data_rezultat"]),
        Laborator = row["laborator"] == DBNull.Value ? null : row["laborator"].ToString(),
        Interpretare = row["interpretare"] == DBNull.Value ? null : row["interpretare"].ToString(),
        PdfUrl = row["pdf_url"] == DBNull.Value ? null : row["pdf_url"].ToString(),
        CreatLa = Convert.ToDateTime(row["creat_la"])
    };

    public async Task<int> DeleteByPacientUntilAsync(int pacientId, DateTime dataLimita)
    {
        try
        {
            // Ștergem mai întâi valorile asociate (FK constraint)
            const string sqlValori = """
                DELETE va FROM valori_analize va
                INNER JOIN rezultate_analize ra ON ra.id = va.rezultat_id
                WHERE ra.pacient_id = @pid AND ra.data_recoltare <= @data
                """;
            await _db.ExecuteAsync(sqlValori, new Dictionary<string, object>
            {
                ["@pid"] = pacientId,
                ["@data"] = dataLimita.Date
            });

            // Ștergem rezultatele
            const string sqlRez = """
                DELETE FROM rezultate_analize
                WHERE pacient_id = @pid AND data_recoltare <= @data
                """;
            var affected = await _db.ExecuteAsync(sqlRez, new Dictionary<string, object>
            {
                ["@pid"] = pacientId,
                ["@data"] = dataLimita.Date
            });
            return affected;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ștergerea rezultatelor analizelor a eșuat.", ex);
        }
    }

    private static RezultatAnaliza MapAgg(Dictionary<string, object> row)
    {
        var r = MapPlain(row);
        r.NrValori = row.ContainsKey("nr_valori") ? Convert.ToInt32(row["nr_valori"]) : 0;
        r.Anormale = row.ContainsKey("anormale") && row["anormale"] != DBNull.Value ? Convert.ToInt32(row["anormale"]) : 0;
        return r;
    }
}
