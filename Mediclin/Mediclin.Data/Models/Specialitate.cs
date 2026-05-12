using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: specialitati
public class Specialitate
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Nume { get; set; } = string.Empty;
    public string? Descriere { get; set; }
    [Required]
    public bool Activa { get; set; }
}
