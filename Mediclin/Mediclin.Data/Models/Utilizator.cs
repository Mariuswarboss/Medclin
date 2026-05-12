using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: utilizatori
public class Utilizator
{
    [Required]
    public int Id { get; set; }
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string ParolaHash { get; set; } = string.Empty;
    [Required]
    public string Rol { get; set; } = string.Empty;
    [Required]
    public string Prenume { get; set; } = string.Empty;
    [Required]
    public string Nume { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public string? AvatarUrl { get; set; }
    [Required]
    public bool Activ { get; set; } = true;
    [Required]
    public DateTime CreatLa { get; set; }
    [Required]
    public DateTime ActualizatLa { get; set; }
    public DateTime? UltimLogin { get; set; }

    public string NumeComplet => $"{Prenume} {Nume}";
}
