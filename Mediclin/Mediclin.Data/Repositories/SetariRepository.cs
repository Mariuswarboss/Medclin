using Mediclin.Data.Context;

namespace Mediclin.Data.Repositories;

public class SetariRepository
{
    private readonly DatabaseContext _db;

    public SetariRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<string?> GetValoareAsync(string cheie)
    {
        try
        {
            const string sql = "SELECT valoare FROM setari_sistem WHERE cheie=@k LIMIT 1";
            var v = await _db.ScalarAsync(sql, new Dictionary<string, object> { ["@k"] = cheie });
            return v is null or DBNull ? null : v.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Setarea '{cheie}' nu a putut fi citită.", ex);
        }
    }

    public async Task SetValoareAsync(string cheie, string valoare)
    {
        try
        {
            const string sql = """
                INSERT INTO setari_sistem (cheie, valoare) VALUES (@k, @v)
                ON DUPLICATE KEY UPDATE valoare=@v
                """;
            await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@k"] = cheie, ["@v"] = valoare });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Setarea '{cheie}' nu a putut fi salvată.", ex);
        }
    }

    public async Task<List<(string Cheie, string Valoare)>> GetAllAsync()
    {
        try
        {
            const string sql = "SELECT cheie, valoare FROM setari_sistem ORDER BY cheie";
            var rows = await _db.QueryAsync(sql);
            return rows.Select(r => (r["cheie"].ToString() ?? string.Empty, r["valoare"].ToString() ?? string.Empty)).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Setările nu au putut fi încărcate.", ex);
        }
    }
}
