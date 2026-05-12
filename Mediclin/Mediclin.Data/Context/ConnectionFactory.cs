using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mediclin.Data.Context;

public static class ConnectionFactory
{
    private static readonly Lazy<MySqlConnectionStringBuilder> Builder = new(CreateBuilder);

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(Builder.Value.ConnectionString);
    }

    public static async Task<bool> TestConnectionAsync()
    {
        try
        {
            await using var conexiune = GetConnection();
            await conexiune.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ConnectionFactory] Eroare conexiune DB: {ex}");
            return false;
        }
    }

    private static MySqlConnectionStringBuilder CreateBuilder()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var settingsPath = Path.Combine(baseDir, "appsettings.json");

        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException($"Fisierul appsettings.json nu a fost gasit in {baseDir}.");
        }

        var json = File.ReadAllText(settingsPath);
        using var doc = JsonDocument.Parse(json);
        var db = doc.RootElement.GetProperty("Database");

        return new MySqlConnectionStringBuilder
        {
            Server = db.GetProperty("Host").GetString(),
            Port = db.GetProperty("Port").GetUInt32(),
            Database = db.GetProperty("Name").GetString(),
            UserID = db.GetProperty("User").GetString(),
            Password = db.GetProperty("Password").GetString(),
            SslMode = MySqlSslMode.Disabled,
            AllowUserVariables = true
        };
    }
}
