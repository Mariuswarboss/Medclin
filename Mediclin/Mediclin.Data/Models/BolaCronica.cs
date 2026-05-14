namespace Mediclin.Data.Models;

/// <summary>Rând din boli_cronice.</summary>
public class BolaCronica
{
    public int Id { get; set; }
    public int PacientId { get; set; }
    public string? Diagnostic { get; set; }
    public DateTime? DataDiagnostic { get; set; }
    public string? Observatii { get; set; }
}
