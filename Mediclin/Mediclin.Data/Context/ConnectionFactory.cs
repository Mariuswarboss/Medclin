using System.Text.Json;
using MySql.Data.MySqlClient;

namespace Mediclin.Data.Context;

public static class ConnectionFactory
{
    private static readonly Lazy<MySqlConnectionStringBuilder> Builder = new(CreateBuilder);
    public static string? LastErrorMessage { get; private set; }

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(Builder.Value.ConnectionString);
    }

    public static async Task<bool> TestConnectionAsync()
    {
        try
        {
            LastErrorMessage = null;
            await using var conexiune = GetConnection();
            await conexiune.OpenAsync();
            return true;
        }
        catch (Exception ex)
        {
            LastErrorMessage = ex.InnerException is null
                ? ex.Message
                : $"{ex.Message} Detalii: {ex.InnerException.Message}";
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

        var csb = new MySqlConnectionStringBuilder();
        csb.Server = db.GetProperty("Host").GetString();
        csb.Port = db.GetProperty("Port").GetUInt32();
        csb.Database = db.GetProperty("Name").GetString();
        csb.UserID = db.GetProperty("User").GetString();
        csb.Password = db.GetProperty("Password").GetString();
        csb.CharacterSet = "utf8mb4";
        csb.ConnectionTimeout = 10;
        csb.AllowUserVariables = true;
        csb.ConvertZeroDateTime = true;
        return csb;
    }
}
