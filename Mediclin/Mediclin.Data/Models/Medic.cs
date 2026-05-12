using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: medici
public class Medic
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int UtilizatorId { get; set; }
    [Required]
    public int SpecialitateId { get; set; }
    [Required]
    public string CodMedic { get; set; } = string.Empty;
    public string? Titlu { get; set; }
    public string? Biografie { get; set; }
    [Required]
    public int DurataConsultatie { get; set; }
    [Required]
    public decimal TarifConsultatie { get; set; }
    [Required]
    public bool Verificat { get; set; }
    public DateTime? VerificatLa { get; set; }
    [Required]
    public DateTime CreatLa { get; set; }
}
