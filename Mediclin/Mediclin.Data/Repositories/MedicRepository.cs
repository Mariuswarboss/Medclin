using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class MedicRepository : IRepository<Medic>
{
    private readonly DatabaseContext _db;

    public MedicRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Medic>> GetAllAsync()
    {
        try
        {
            const string sql = """
                SELECT m.*, u.prenume, u.nume, u.email, u.telefon, s.nume AS specialitate_nume
                FROM medici m
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE u.activ = 1
                ORDER BY u.nume, u.prenume
                """;
            return (await _db.QueryAsync(sql)).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-au putut încărca medicii.", ex);
        }
    }

    public async Task<Medic?> GetByIdAsync(int id)
    {
        try
        {
            const string sql = """
                SELECT m.*, u.prenume, u.nume, u.email, u.telefon, s.nume AS specialitate_nume
                FROM medici m
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE m.id = @id
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
            return rows.Count == 0 ? null : MapJoined(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Medicul nu a putut fi încărcat.", ex);
        }
    }

    public async Task<Medic?> GetByUtilizatorIdAsync(int utilizatorId)
    {
        try
        {
            const string sql = """
                SELECT m.*, u.prenume, u.nume, u.email, u.telefon, s.nume AS specialitate_nume
                FROM medici m
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE m.utilizator_id = @uid
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@uid"] = utilizatorId });
            return rows.Count == 0 ? null : MapJoined(rows[0]);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Profilul medic nu a putut fi încărcat.", ex);
        }
    }

    public async Task<List<Medic>> GetBySpecialitateAsync(int specialitateId)
    {
        try
        {
            const string sql = """
                SELECT m.*, u.prenume, u.nume, u.email, u.telefon, s.nume AS specialitate_nume
                FROM medici m
                INNER JOIN utilizatori u ON u.id = m.utilizator_id
                INNER JOIN specialitati s ON s.id = m.specialitate_id
                WHERE m.specialitate_id = @sid AND u.activ = 1
                ORDER BY u.nume, u.prenume
                """;
            return (await _db.QueryAsync(sql, new Dictionary<string, object> { ["@sid"] = specialitateId })).Select(MapJoined).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Medicii pe specialitate nu au putut fi încărcați.", ex);
        }
    }

    public async Task<int> CreateAsync(Medic medic)
    {
        try
        {
            const string sql = """
                INSERT INTO medici (utilizator_id, specialitate_id, cod_medic, titlu, biografie, durata_consultatie, tarif_consultatie, verificat, creat_la)
                VALUES (@uid, @sid, @cod, @titlu, @bio, @dur, @tarif, 0, NOW());
                SELECT LAST_INSERT_ID();
                """;
            var id = await _db.ScalarAsync(sql, new Dictionary<string, object>
            {
                ["@uid"] = medic.UtilizatorId,
                ["@sid"] = medic.SpecialitateId,
                ["@cod"] = medic.CodMedic,
                ["@titlu"] = (object?)medic.Titlu ?? DBNull.Value,
                ["@bio"] = (object?)medic.Biografie ?? DBNull.Value,
                ["@dur"] = medic.DurataConsultatie,
                ["@tarif"] = medic.TarifConsultatie
            });
            return Convert.ToInt32(id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Medicul nu a putut fi creat.", ex);
        }
    }

    public async Task UpdateAsync(Medic medic)
    {
        try
        {
            const string sql = """
                UPDATE medici
                SET specialitate_id=@sid, titlu=@titlu, biografie=@bio,
                    durata_consultatie=@dur, tarif_consultatie=@tarif
                WHERE id=@id
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            {
                ["@id"] = medic.Id,
                ["@sid"] = medic.SpecialitateId,
                ["@titlu"] = (object?)medic.Titlu ?? DBNull.Value,
                ["@bio"] = (object?)medic.Biografie ?? DBNull.Value,
                ["@dur"] = medic.DurataConsultatie,
                ["@tarif"] = medic.TarifConsultatie
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Medicul nu a putut fi actualizat.", ex);
        }
    }

    public async Task SetVerificatAsync(int id, bool verificat)
    {
        try
        {
            const string sql = """
                UPDATE medici
                SET verificat=@v, verificat_la=@vla
                WHERE id=@id
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object>
            {
                ["@id"] = id,
                ["@v"] = verificat ? 1 : 0,
                ["@vla"] = verificat ? DateTime.Now : DBNull.Value
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Verificarea medicului nu a putut fi salvată.", ex);
        }
    }

    public async Task<List<ProgramMedic>> GetProgramAsync(int medicId)
    {
        try
        {
            const string sql = """
                SELECT id, medic_id, zi_saptamana, ora_start, ora_sfarsit, activ
                FROM program_medici
                WHERE medic_id=@mid AND activ=1
                ORDER BY FIELD(zi_saptamana,'Luni','Marti','Miercuri','Joi','Vineri','Sambata','Duminica'), ora_start
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@mid"] = medicId });
            return rows.Select(MapProgram).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Programul medicului nu a putut fi încărcat.", ex);
        }
    }

    public Task<List<Dictionary<string, object>>> GetWithProgramAsync(int medicId)
    {
        const string sql = """
            SELECT m.*, pm.zi_saptamana, pm.ora_start, pm.ora_sfarsit, pm.activ AS program_activ
            FROM medici m
            LEFT JOIN program_medici pm ON pm.medic_id = m.id
            WHERE m.id = @id
            ORDER BY FIELD(pm.zi_saptamana,'Luni','Marti','Miercuri','Joi','Vineri','Sambata','Duminica'), pm.ora_start
            """;
        return _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = medicId });
    }

    public Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM medici WHERE id = @id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    private static ProgramMedic MapProgram(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        MedicId = Convert.ToInt32(row["medic_id"]),
        ZiSaptamana = row["zi_saptamana"].ToString() ?? string.Empty,
        OraStart = ParseTime(row["ora_start"]),
        OraSfarsit = ParseTime(row["ora_sfarsit"]),
        Activ = Convert.ToBoolean(row["activ"])
    };

    private static TimeSpan ParseTime(object v)
    {
        if (v == DBNull.Value)
        {
            return TimeSpan.Zero;
        }

        if (v is TimeSpan ts)
        {
            return ts;
        }

        if (v is DateTime dt)
        {
            return dt.TimeOfDay;
        }

        return TimeSpan.Parse(v.ToString()!);
    }

    private static Medic MapJoined(Dictionary<string, object> row)
    {
        var m = MapBase(row);
        m.Prenume = row.TryGetValue("prenume", out var pr) ? pr?.ToString() : null;
        m.Nume = row.TryGetValue("nume", out var n) ? n?.ToString() : null;
        m.Email = row.TryGetValue("email", out var e) ? e?.ToString() : null;
        m.Telefon = row.TryGetValue("telefon", out var t) ? t?.ToString() : null;
        m.SpecialitateNume = row.TryGetValue("specialitate_nume", out var sn) && sn != DBNull.Value ? sn.ToString() : null;
        return m;
    }

    private static Medic MapBase(Dictionary<string, object> row) => new()
    {
        Id = Convert.ToInt32(row["id"]),
        UtilizatorId = Convert.ToInt32(row["utilizator_id"]),
        SpecialitateId = Convert.ToInt32(row["specialitate_id"]),
        CodMedic = row["cod_medic"].ToString() ?? string.Empty,
        Titlu = row["titlu"] == DBNull.Value ? null : row["titlu"].ToString(),
        Biografie = row["biografie"] == DBNull.Value ? null : row["biografie"].ToString(),
        DurataConsultatie = Convert.ToInt32(row["durata_consultatie"]),
        TarifConsultatie = Convert.ToDecimal(row["tarif_consultatie"]),
        Verificat = Convert.ToBoolean(row["verificat"]),
        VerificatLa = row["verificat_la"] == DBNull.Value ? null : Convert.ToDateTime(row["verificat_la"]),
        CreatLa = Convert.ToDateTime(row["creat_la"])
    };
}
