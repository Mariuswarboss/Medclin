namespace Mediclin.Data.Models;

public class ValoareAnaliza
{
    public int Id { get; set; }
    public int RezultatId { get; set; }
    public string TestNume { get; set; } = string.Empty;
    public string Valoare { get; set; } = string.Empty;
    public string? Unitate { get; set; }
    public decimal? ValMinNormal { get; set; }
    public decimal? ValMaxNormal { get; set; }
    public string Status { get; set; } = string.Empty;
}
