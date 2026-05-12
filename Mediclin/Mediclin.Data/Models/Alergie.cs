using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: alergii
public class Alergie
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int PacientId { get; set; }
    [Required]
    public string Substanta { get; set; } = string.Empty;
    [Required]
    public string Severitate { get; set; } = string.Empty;
    public string? Observatii { get; set; }
}
