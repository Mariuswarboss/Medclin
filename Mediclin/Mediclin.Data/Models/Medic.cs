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

    // Populat la JOIN cu utilizatori / specialitati
    public string? Prenume { get; set; }
    public string? Nume { get; set; }
    public string? Email { get; set; }
    public string? Telefon { get; set; }
    public string? SpecialitateNume { get; set; }
    public string NumeComplet => $"{Prenume} {Nume}".Trim();
    public string NumeCompletCuTitlu => $"{Titlu} {Prenume} {Nume}".Trim();
    public string Initiale => $"{(string.IsNullOrWhiteSpace(Prenume) ? ' ' : Prenume[0])}{(string.IsNullOrWhiteSpace(Nume) ? ' ' : Nume[0])}".ToUpper().Trim();
    public string TarifText => TarifConsultatie > 0 ? $"{TarifConsultatie:N0} MDL" : "Gratuit";
    public string DurataText => $"{DurataConsultatie} min";
}
