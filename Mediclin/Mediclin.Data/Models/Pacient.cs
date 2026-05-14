using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: pacienti
public class Pacient
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int UtilizatorId { get; set; }
    public DateOnly? DataNasterii { get; set; }
    public string? Sex { get; set; }
    public string? Cnp { get; set; }
    public string? GrupaSanguina { get; set; }
    public string? Adresa { get; set; }
    public string? Oras { get; set; }
    public string? ContactUrgentaNume { get; set; }
    public string? ContactUrgentaTelefon { get; set; }
    public string? ContactUrgentaRelatie { get; set; }
    public int? MedicDeFamilieId { get; set; }
    [Required]
    public DateTime CreatLa { get; set; }

    public string? Prenume { get; set; }
    public string? Nume { get; set; }
    public string? Email { get; set; }
    public string? Telefon { get; set; }
    public string NumeComplet => string.IsNullOrWhiteSpace($"{Prenume} {Nume}".Trim()) ? $"Pacient #{Id}" : $"{Prenume} {Nume}".Trim();
    public string? SpecialitateNume { get; set; }
    public DateTime? UltimaVizita { get; set; }
}
