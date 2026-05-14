using Mediclin.Data.Context;
using Mediclin.Data.Models;
using MySql.Data.MySqlClient;

namespace Mediclin.Data.Repositories;

public class UtilizatorRepository : IUtilizatorRepository
{
    private readonly DatabaseContext _db;

    public UtilizatorRepository(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<Utilizator?> GetByEmailAsync(string email)
    {
        const string sql = """
            SELECT id, email, parola_hash, rol, prenume, nume, telefon, avatar_url, activ, creat_la, actualizat_la, ultim_login
            FROM utilizatori
            WHERE email = @email
            LIMIT 1
            """;

        var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@email"] = email });
        if (rows.Count == 0)
        {
            return null;
        }

        return Map(rows[0]);
    }

    public async Task<List<Utilizator>> GetAllAsync()
    {
        const string sql = """
            SELECT id, email, parola_hash, rol, prenume, nume, telefon, avatar_url, activ, creat_la, actualizat_la, ultim_login
            FROM utilizatori
            ORDER BY creat_la DESC
            """;
        return (await _db.QueryAsync(sql)).Select(Map).ToList();
    }

    public async Task<Utilizator?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT id, email, parola_hash, rol, prenume, nume, telefon, avatar_url, activ, creat_la, actualizat_la, ultim_login
            FROM utilizatori
            WHERE id = @id
            """;
        var rows = await _db.QueryAsync(sql, new Dictionary<string, object> { ["@id"] = id });
        return rows.Count == 0 ? null : Map(rows[0]);
    }

    public async Task<int> CreateAsync(Utilizator utilizator)
    {
        await using var conexiune = ConnectionFactory.GetConnection();
        await conexiune.OpenAsync();

        const string sql = """
            INSERT INTO utilizatori (email, parola_hash, rol, prenume, nume, telefon, avatar_url, activ, creat_la, actualizat_la)
            VALUES (@email, @parola_hash, @rol, @prenume, @nume, @telefon, @avatar_url, @activ, NOW(), NOW());
            SELECT LAST_INSERT_ID();
            """;

        await using var cmd = new MySqlCommand(sql, conexiune);
        cmd.Parameters.AddWithValue("@email", utilizator.Email);
        cmd.Parameters.AddWithValue("@parola_hash", utilizator.ParolaHash);
        cmd.Parameters.AddWithValue("@rol", utilizator.Rol);
        cmd.Parameters.AddWithValue("@prenume", utilizator.Prenume);
        cmd.Parameters.AddWithValue("@nume", utilizator.Nume);
        cmd.Parameters.AddWithValue("@telefon", (object?)utilizator.Telefon ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@avatar_url", (object?)utilizator.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@activ", utilizator.Activ);

        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task UpdateUltimLoginAsync(int id)
    {
        const string sql = "UPDATE utilizatori SET ultim_login = NOW(), actualizat_la = NOW() WHERE id = @id";
        await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    public async Task UpdateAsync(Utilizator utilizator)
    {
        const string sql = """
            UPDATE utilizatori
            SET email=@email, rol=@rol, prenume=@prenume, nume=@nume, telefon=@telefon,
                avatar_url=@avatar_url, activ=@activ, actualizat_la=NOW()
            WHERE id=@id
            """;
        await _db.ExecuteAsync(sql, new Dictionary<string, object>
        {
            ["@id"] = utilizator.Id,
            ["@email"] = utilizator.Email,
            ["@rol"] = utilizator.Rol,
            ["@prenume"] = utilizator.Prenume,
            ["@nume"] = utilizator.Nume,
            ["@telefon"] = (object?)utilizator.Telefon ?? DBNull.Value,
            ["@avatar_url"] = (object?)utilizator.AvatarUrl ?? DBNull.Value,
            ["@activ"] = utilizator.Activ
        });
    }

    public async Task UpdateParolaHashAsync(int id, string parolaHash)
    {
        const string sql = "UPDATE utilizatori SET parola_hash=@h, actualizat_la=NOW() WHERE id=@id";
        await _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id, ["@h"] = parolaHash });
    }

    public Task DeleteAsync(int id)
    {
        const string sql = "DELETE FROM utilizatori WHERE id = @id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id });
    }

    public Task SetActivAsync(int id, bool activ)
    {
        const string sql = "UPDATE utilizatori SET activ=@activ, actualizat_la=NOW() WHERE id=@id";
        return _db.ExecuteAsync(sql, new Dictionary<string, object> { ["@id"] = id, ["@activ"] = activ });
    }

    public async Task<bool> ExistsEmailAsync(string email)
    {
        const string sql = "SELECT COUNT(1) FROM utilizatori WHERE email = @email";
        var result = await _db.ExecuteScalarAsync(sql, new Dictionary<string, object> { ["@email"] = email });
        return Convert.ToInt32(result) > 0;
    }

    private static Utilizator Map(Dictionary<string, object> row)
    {
        return new Utilizator
        {
            Id = Convert.ToInt32(row["id"]),
            Email = row["email"].ToString() ?? string.Empty,
            ParolaHash = row["parola_hash"].ToString() ?? string.Empty,
            Rol = row["rol"].ToString() ?? string.Empty,
            Prenume = row["prenume"].ToString() ?? string.Empty,
            Nume = row["nume"].ToString() ?? string.Empty,
            Telefon = row["telefon"] == DBNull.Value ? null : row["telefon"].ToString(),
            AvatarUrl = row["avatar_url"] == DBNull.Value ? null : row["avatar_url"].ToString(),
            Activ = Convert.ToBoolean(row["activ"]),
            CreatLa = Convert.ToDateTime(row["creat_la"]),
            ActualizatLa = Convert.ToDateTime(row["actualizat_la"]),
            UltimLogin = row["ultim_login"] == DBNull.Value ? null : Convert.ToDateTime(row["ultim_login"])
        };
    }
}
