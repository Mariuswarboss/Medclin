using MySql.Data.MySqlClient;

namespace Mediclin.Data.Context;

public class DatabaseContext
{
    public async Task<List<Dictionary<string, object>>> QueryAsync(
        string sql,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            await using var conexiune = ConnectionFactory.GetConnection();
            await conexiune.OpenAsync();
            await using var command = BuildCommand(conexiune, sql, parameters);
            await using var reader = await command.ExecuteReaderAsync();

            var rezultat = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var rand = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    rand[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? DBNull.Value : reader.GetValue(i);
                }
                rezultat.Add(rand);
            }

            return rezultat;
        }
        catch (Exception ex)
        {
            await LogErrorAsync("QueryAsync", sql, ex);
            throw;
        }
    }

    public async Task<int> ExecuteAsync(
        string sql,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            await using var conexiune = ConnectionFactory.GetConnection();
            await conexiune.OpenAsync();
            await using var command = BuildCommand(conexiune, sql, parameters);
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            await LogErrorAsync("ExecuteAsync", sql, ex);
            throw;
        }
    }

    public async Task<object?> ExecuteScalarAsync(
        string sql,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            await using var conexiune = ConnectionFactory.GetConnection();
            await conexiune.OpenAsync();
            await using var command = BuildCommand(conexiune, sql, parameters);
            return await command.ExecuteScalarAsync();
        }
        catch (Exception ex)
        {
            await LogErrorAsync("ExecuteScalarAsync", sql, ex);
            throw;
        }
    }

    public Task<object?> ScalarAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        return ExecuteScalarAsync(sql, parameters);
    }

    private static MySqlCommand BuildCommand(
        MySqlConnection conexiune,
        string sql,
        Dictionary<string, object>? parameters)
    {
        var command = new MySqlCommand(sql, conexiune);
        if (parameters is null)
        {
            return command;
        }

        foreach (var param in parameters)
        {
            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }

        return command;
    }

    private static async Task LogErrorAsync(string actiune, string sql, Exception ex)
    {
        try
        {
            await using var conexiune = ConnectionFactory.GetConnection();
            await conexiune.OpenAsync();

            const string insertSql = """
                INSERT INTO jurnal_activitate (utilizator_id, actiune, modul, detalii, ip_adresa, severitate, creat_la)
                VALUES (@utilizator_id, @actiune, @modul, @detalii, @ip_adresa, @severitate, NOW())
                """;
            await using var command = new MySqlCommand(insertSql, conexiune);
            command.Parameters.AddWithValue("@utilizator_id", DBNull.Value);
            command.Parameters.AddWithValue("@actiune", actiune);
            command.Parameters.AddWithValue("@modul", "DatabaseContext");
            command.Parameters.AddWithValue("@detalii", $$"""{"sql":"{{sql.Replace("\"", "'")}}","error":"{{ex.Message.Replace("\"", "'")}}"}""");
            command.Parameters.AddWithValue("@ip_adresa", "127.0.0.1");
            command.Parameters.AddWithValue("@severitate", "Error");
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignoram orice eroare de logging pentru a evita exceptii in lant.
        }
    }
}
