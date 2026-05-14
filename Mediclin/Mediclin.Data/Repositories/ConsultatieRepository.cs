using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class ConsultatieRepository : IRepository<Consultatie>
{
    private readonly DatabaseContext _db;

    public ConsultatieRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Consultatie>> GetAllAsync()
    {
        try
        {
            const string sql = "SELECT * FROM consultatii ORDER BY data_consultatie DESC";
            return (await _db.QueryAsync(sql)).Select(MapPlain).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Consultațiile nu au putut fi încărcate.", ex);
        }
    }

    public async Task<Consultatie?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = "SELECT * FROM consultatii WHERE id = @id";
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
            return rows.Count == 0 ? null : MapPlain(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Consultația nu a putut fi încărcată.", ex);
        }
    }

    public async Task<List<Consultatie>> GetByPacientAsync(int pacientId)
    {
        try
        {
            const string sql = """
                SELECT c.*, CONCAT(u.prenume,' ',u.nume) AS medic_nume, s.nume AS specialitate
                FROM consultatii c
                INNER JOIN medici m ON m.id = c.medic_id
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE c.pacient_id = @pid
                ORDER BY c.data_consultatie DESC
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@pid"] = pacientId })).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Istoricul consultațiilor nu a putut fi încărcat.", ex);
        }
    }

    public async Task<List<Consultatie>> GetByMedicAsync(int medicId)
    {
        try
        {
            const string sql = "SELECT * FROM consultatii WHERE medic_id = @medic_id ORDER BY data_consultatie DESC";
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@medic_id"] = medicId })).Select(MapPlain).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Consultațiile medicului nu au putut fi încărcate.", ex);
        }
    }

    public async Task<Consultatie?> GetByProgramareAsync(int programareId)
    {
        try
        {
            const string sql = """
                SELECT c.*, CONCAT(u.prenume,' ',u.nume) AS medic_nume, s.nume AS specialitate
                FROM consultatii c
                INNER JOIN medici m ON m.id = c.medic_id
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE c.programare_id = @prid
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@prid"] = programareId });
            return rows.Count == 0 ? null : MapJoined(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Consultația pentru programare nu a putut fi încărcată.", ex);
        }
    }

    public async Task<int> CreateAsync(Consultatie consultatie)
    {
        try
        {
            const string sql = """
                INSERT INTO consultatii (programare_id, pacient_id, medic_id, data_consultatie, simptome, diagnostic_cod,
                    diagnostic_text, recomandari, tensiune_arteriala, puls, temperatura, greutate, inaltime, note_private, creat_la)
                VALUES (@programare_id, @pacient_id, @medic_id, @data_consultatie, @simptome, @diagnostic_cod,
                    @diagnostic_text, @recomandari, @tensiune_arteriala, @puls, @temperatura, @greutate, @inaltime, @note_private, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, Params(consultatie));
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Consultația nu a putut fi creată.", ex);
        }
    }

    public async Task UpdateAsync(Consultatie consultatie)
    {
        try
        {
            const string sql = """
                UPDATE consultatii
                SET simptome=@simptome, diagnostic_cod=@diagnostic_cod, diagnostic_text=@diagnostic_text,
                    recomandari=@recomandari, tensiune_arteriala=@tensiune_arteriala, puls=@puls,
                    temperatura=@temperatura, greutate=@greutate, inaltime=@inaltime, note_private=@note_private
                WHERE id=@id
                """;
            await _db.ExecuteAsync(sql, Params(consultatie));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Consultația nu a putut fi actualizată.", ex);
        }
    }

    public Task<List<Dictionary<string, object>>> GetWithReteteAsync(int consultatieId)
    {
        const string sql = """
            SELECT c.*, r.id AS reteta_id, r.data_emitere, r.data_expirare, r.status AS reteta_status, r.observatii AS reteta_observatii
            FROM consultatii c
            LEFT JOIN retete r ON r.consultatie_id = c.id
            WHERE c.id = @id
            """;
        return _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = consultatieId });
    }

    public Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM consultatii WHERE id = @id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    private static Dictionary<string, object> Params(Consultatie c) => new()
    {
        ["@id"] = c.Id,
        ["@programare_id"] = c.ProgramareId,
        ["@pacient_id"] = c.PacientId,
        ["@medic_id"] = c.MedicId,
        ["@data_consultatie"] = c.DataConsultatie == default ? DateTime.Now : c.DataConsultatie,
        ["@simptome"] = (object?)c.Simptome ?? DBNull.Value,
        ["@diagnostic_cod"] = (object?)c.DiagnosticCod ?? DBNull.Value,
        ["@diagnostic_text"] = (object?)c.DiagnosticText ?? DBNull.Value,
        ["@recomandari"] = (object?)c.Recomandari ?? DBNull.Value,
        ["@tensiune_arteriala"] = (object?)c.TensiuneArteriala ?? DBNull.Value,
        ["@puls"] = (object?)c.Puls ?? DBNull.Value,
        ["@temperatura"] = (object?)c.Temperatura ?? DBNull.Value,
        ["@greutate"] = (object?)c.Greutate ?? DBNull.Value,
        ["@inaltime"] = (object?)c.Inaltime ?? DBNull.Value,
        ["@note_private"] = (object?)c.NotePrivate ?? DBNull.Value
    };

    private static Consultatie MapJoined(Dictionary<string, object> row)
    {
        var c = MapPlain(row);
        c.MedicNume = row.TryGetValue("medic_nume", out var mn) && mn != DBNull.Value ? mn.ToString() : null;
        c.Specialitate = row.TryGetValue("specialitate", out var s) && s != DBNull.Value ? s.ToString() : null;
        return c;
    }

    private static Consultatie MapPlain(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        ProgramareId = Convert.ToInt32(row["programare_id"]),
        PacientId = Convert.ToInt32(row["pacient_id"]),
        MedicId = Convert.ToInt32(row["medic_id"]),
        DataConsultatie = Convert.ToDateTime(row["data_consultatie"]),
        Simptome = row["simptome"] == DBNull.Value ? null : row["simptome"].ToString(),
        DiagnosticCod = row["diagnostic_cod"] == DBNull.Value ? null : row["diagnostic_cod"].ToString(),
        DiagnosticText = row["diagnostic_text"] == DBNull.Value ? null : row["diagnostic_text"].ToString(),
        Recomandari = row["recomandari"] == DBNull.Value ? null : row["recomandari"].ToString(),
        TensiuneArteriala = row["tensiune_arteriala"] == DBNull.Value ? null : row["tensiune_arteriala"].ToString(),
        Puls = row["puls"] == DBNull.Value ? null : Convert.ToInt32(row["puls"]),
        Temperatura = row["temperatura"] == DBNull.Value ? null : Convert.ToDecimal(row["temperatura"]),
        Greutate = row["greutate"] == DBNull.Value ? null : Convert.ToDecimal(row["greutate"]),
        Inaltime = row["inaltime"] == DBNull.Value ? null : Convert.ToDecimal(row["inaltime"]),
        NotePrivate = row["note_private"] == DBNull.Value ? null : row["note_private"].ToString(),
        CreatLa = Convert.ToDateTime(row["creat_la"])
    };
}
