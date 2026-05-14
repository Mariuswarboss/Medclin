namespace Mediclin.Data.Models;

/// <summary>Rând din program_medici.</summary>
public class ProgramMedic
{
    public int Id { get; set; }
    public int MedicId { get; set; }
    public string ZiSaptamana { get; set; } = string.Empty;
    public TimeSpan OraStart { get; set; }
    public TimeSpan OraSfarsit { get; set; }
    public bool Activ { get; set; }
}
