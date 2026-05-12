using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: medicamente_reteta
public class Medicament
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int RetetaId { get; set; }
    [Required]
    public string Denumire { get; set; } = string.Empty;
    public string? Concentratie { get; set; }
    public string? Forma { get; set; }
    [Required]
    public int Cantitate { get; set; }
    public string? Dozaj { get; set; }
    public string? Frecventa { get; set; }
    public int? DurataZile { get; set; }
    public string? Instructiuni { get; set; }
}
