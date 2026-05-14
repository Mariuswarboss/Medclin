namespace Mediclin.Data.Models;

public class ConversatieLista
{
    public int Id { get; set; }
    public int PacientId { get; set; }
    public int MedicId { get; set; }
    public DateTime CreatLa { get; set; }
    public string PartenerNume { get; set; } = string.Empty;
    public string? UltimMesajScurt { get; set; }
    public DateTime? UltimMesajLa { get; set; }
    public int Necitite { get; set; }
}
