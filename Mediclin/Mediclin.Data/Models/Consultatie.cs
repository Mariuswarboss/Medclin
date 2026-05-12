using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: consultatii
public class Consultatie
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int ProgramareId { get; set; }
    [Required]
    public int PacientId { get; set; }
    [Required]
    public int MedicId { get; set; }
    [Required]
    public DateTime DataConsultatie { get; set; }
    public string? Simptome { get; set; }
    public string? DiagnosticCod { get; set; }
    public string? DiagnosticText { get; set; }
    public string? Recomandari { get; set; }
    public string? TensiuneArteriala { get; set; }
    public int? Puls { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? Greutate { get; set; }
    public decimal? Inaltime { get; set; }
    public string? NotePrivate { get; set; }
    [Required]
    public DateTime CreatLa { get; set; }
}
