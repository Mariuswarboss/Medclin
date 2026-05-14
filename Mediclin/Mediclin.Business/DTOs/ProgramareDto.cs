namespace Mediclin.Business.DTOs;

public class ProgramareDto
{
    public int Id { get; set; }
    public int PacientId { get; set; }
    public int MedicId { get; set; }
    public DateTime DataOra { get; set; }
    public int DurataMin { get; set; } = 30;
    public string Tip { get; set; } = "Initiala";
    public string Status { get; set; } = "Programata";
    public string? MotivVizita { get; set; }
    public string? ObservatiiAdmin { get; set; }
    public string PacientPrenume { get; set; } = string.Empty;
    public string PacientNumeFamilie { get; set; } = string.Empty;
    public string MedicPrenume { get; set; } = string.Empty;
    public string MedicNumeFamilie { get; set; } = string.Empty;
    public string Specialitate { get; set; } = string.Empty;
    public decimal TarifConsultatie { get; set; }
    public DateTime CreatLa { get; set; }
    public DateTime ActualizatLa { get; set; }
    public string PacientNume => $"{PacientPrenume} {PacientNumeFamilie}".Trim();
    public string MedicNume => $"Dr. {MedicPrenume} {MedicNumeFamilie}".Trim();
}
