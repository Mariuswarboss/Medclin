using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: rezultate_analize
public class RezultatAnaliza
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int PacientId { get; set; }
    public int? ConsultatieId { get; set; }
    [Required]
    public DateTime DataRecoltare { get; set; }
    public DateTime? DataRezultat { get; set; }
    public string? Laborator { get; set; }
    public string? Interpretare { get; set; }
    public string? PdfUrl { get; set; }
    [Required]
    public DateTime CreatLa { get; set; }

    public int NrValori { get; set; }
    public int Anormale { get; set; }
}
