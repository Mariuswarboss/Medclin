using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: programari
public class Programare
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int PacientId { get; set; }
    [Required]
    public int MedicId { get; set; }
    [Required]
    public DateTime DataOra { get; set; }
    [Required]
    public int DurataMin { get; set; }
    [Required]
    public string Tip { get; set; } = string.Empty;
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? MotivVizita { get; set; }
    public string? ObservatiiAdmin { get; set; }
    [Required]
    public DateTime CreatLa { get; set; }
    [Required]
    public DateTime ActualizatLa { get; set; }

    public string? PacientNume { get; set; }
    public string? MedicNume { get; set; }
    public string? MedicTitlu { get; set; }
    public string? SpecialitateNume { get; set; }
}
