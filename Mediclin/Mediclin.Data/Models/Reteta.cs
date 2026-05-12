using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: retete
public class Reteta
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int ConsultatieId { get; set; }
    [Required]
    public int PacientId { get; set; }
    [Required]
    public int MedicId { get; set; }
    [Required]
    public DateTime DataEmitere { get; set; }
    public DateTime? DataExpirare { get; set; }
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? Observatii { get; set; }
}
