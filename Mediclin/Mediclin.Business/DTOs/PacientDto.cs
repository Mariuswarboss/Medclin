namespace Mediclin.Business.DTOs;

public class PacientDto
{
    public int Id { get; set; }
    public int UtilizatorId { get; set; }
    public string Prenume { get; set; } = string.Empty;
    public string Nume { get; set; } = string.Empty;
    public string NumeComplet => $"{Prenume} {Nume}".Trim();
    public string Email { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public DateOnly? DataNasterii { get; set; }
    public int? Varsta => DataNasterii is null ? null : CalculateAge(DataNasterii.Value);
    public string? Sex { get; set; }
    public string? Cnp { get; set; }
    public string? GrupaSanguina { get; set; }
    public string? Adresa { get; set; }
    public string? Oras { get; set; }
    public string? ContactUrgentaNume { get; set; }
    public string? ContactUrgentaTelefon { get; set; }
    public string? ContactUrgentaRelatie { get; set; }
    public string? MedicDeFamilie { get; set; }
    public DateTime CreatLa { get; set; }

    private static int CalculateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
