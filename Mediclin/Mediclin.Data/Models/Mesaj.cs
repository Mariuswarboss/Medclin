using System.ComponentModel.DataAnnotations;

namespace Mediclin.Data.Models;

// Table: mesaje
public class Mesaj
{
    [Required]
    public int Id { get; set; }
    [Required]
    public int ExpeditorId { get; set; }
    [Required]
    public int DestinatarId { get; set; }
    [Required]
    public int ConversatieId { get; set; }
    [Required]
    public string Continut { get; set; } = string.Empty;
    [Required]
    public string Tip { get; set; } = string.Empty;
    public string? FisierUrl { get; set; }
    [Required]
    public bool Citit { get; set; }
    public DateTime? CititLa { get; set; }
    [Required]
    public DateTime TrimisLa { get; set; }

    public string? ExpeditorNume { get; set; }
}
