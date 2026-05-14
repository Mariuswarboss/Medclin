namespace Mediclin.Data.Models;

public class Notificare
{
    public int Id { get; set; }
    public int UtilizatorId { get; set; }
    public string Titlu { get; set; } = string.Empty;
    public string Mesaj { get; set; } = string.Empty;
    public string Tip { get; set; } = string.Empty;
    public bool Citita { get; set; }
    public string? Link { get; set; }
    public DateTime CreatLa { get; set; }
}
