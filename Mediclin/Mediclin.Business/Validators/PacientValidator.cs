using Mediclin.Business.DTOs;

namespace Mediclin.Business.Validators;

public static class PacientValidator
{
    public static (bool valid, string error) ValidateCNP(string? cnp)
    {
        if (string.IsNullOrWhiteSpace(cnp))
        {
            return (true, string.Empty);
        }

        if (cnp.Length is < 7 or > 20 || !cnp.All(char.IsDigit))
        {
            return (false, "CNP-ul trebuie sa contina doar cifre si sa aiba intre 7 si 20 caractere.");
        }

        return (true, string.Empty);
    }

    public static (bool valid, string error) ValidateAge(DateOnly? dataNasterii)
    {
        if (dataNasterii is null)
        {
            return (true, string.Empty);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dataNasterii > today)
        {
            return (false, "Data nasterii nu poate fi in viitor.");
        }

        if (dataNasterii < today.AddYears(-120))
        {
            return (false, "Varsta introdusa nu este valida.");
        }

        return (true, string.Empty);
    }

    public static List<string> ValidateRequiredFields(PacientDto pacient)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(pacient.Prenume))
        {
            errors.Add("Prenumele pacientului este obligatoriu.");
        }

        if (string.IsNullOrWhiteSpace(pacient.Nume))
        {
            errors.Add("Numele pacientului este obligatoriu.");
        }

        var cnp = ValidateCNP(pacient.Cnp);
        if (!cnp.valid)
        {
            errors.Add(cnp.error);
        }

        var age = ValidateAge(pacient.DataNasterii);
        if (!age.valid)
        {
            errors.Add(age.error);
        }

        return errors;
    }
}
