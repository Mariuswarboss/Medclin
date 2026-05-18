namespace Mediclin.Business.DTOs;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Parola { get; set; } = string.Empty;
    public string ConfirmareParola { get; set; } = string.Empty;
    public string Prenume { get; set; } = string.Empty;
    public string Nume { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Rol { get; set; } = "pacient";

    /// <summary>ID specialitate (din <c>specialitati</c>) — obligatoriu dacă <see cref="Rol"/> este medic.</summary>
    public int? SpecialitateId { get; set; }
}
