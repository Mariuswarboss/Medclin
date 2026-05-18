namespace Mediclin.Data.Models;

public class ConversatieLista
{
    public int Id { get; set; }
    public int PacientId { get; set; }
    public int MedicId { get; set; }
    public DateTime CreatLa { get; set; }
    public string PartenerNume { get; set; } = string.Empty;
    /// <summary>Alias pentru binding-uri XAML care folosesc NumePartener.</summary>
    public string NumePartener => PartenerNume;

    public string InitialePartener
    {
        get
        {
            var s = (PartenerNume ?? string.Empty).Trim();
            if (s.Length == 0)
            {
                return "?";
            }

            var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            }

            return char.ToUpperInvariant(s[0]).ToString();
        }
    }

    public string? UltimMesajScurt { get; set; }
    public DateTime? UltimMesajLa { get; set; }
    public int Necitite { get; set; }
}
