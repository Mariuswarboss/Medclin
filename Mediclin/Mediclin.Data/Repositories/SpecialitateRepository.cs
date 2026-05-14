using Mediclin.Data.Context;
using Mediclin.Data.Models;

namespace Mediclin.Data.Repositories;

public class SpecialitateRepository
{
    private readonly DatabaseContext _db;

    public SpecialitateRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<List<Specialitate>> GetAllAsync()
    {
        try
        {
            const string sql = "SELECT id, nume, descriere, activa FROM specialitati WHERE activa=1 ORDER BY nume";
            var rows = await _db.QueryAsync(sql);
            return rows.Select(r => new Specialitate
            {
                Id = Convert.ToInt32(r["id"]),
                Nume = r["nume"].ToString() ?? string.Empty,
                Descriere = r["descriere"] == DBNull.Value ? null : r["descriere"].ToString(),
                Activa = Convert.ToBoolean(r["activa"])
            }).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Nu s-au putut încărca specialitățile.", ex);
        }
    }
}
