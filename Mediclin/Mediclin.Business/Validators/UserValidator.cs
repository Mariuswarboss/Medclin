using System.Text.RegularExpressions;
using Mediclin.Business.DTOs;

namespace Mediclin.Business.Validators;

public static class UserValidator
{
    public static (bool valid, string error) ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return (false, "Email-ul este obligatoriu.");
        }

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return (false, "Formatul email-ului este invalid.");
        }

        return (true, string.Empty);
    }

    public static (bool valid, string error) ValidatePassword(string password)
    {
        if (password.Length < 8)
        {
            return (false, "Parola trebuie sa aiba minimum 8 caractere.");
        }

        if (!password.Any(char.IsUpper))
        {
            return (false, "Parola trebuie sa contina cel putin o litera mare.");
        }

        if (!password.Any(char.IsDigit))
        {
            return (false, "Parola trebuie sa contina cel putin o cifra.");
        }

        return (true, string.Empty);
    }

    public static List<string> ValidateRegisterDto(RegisterDto dto)
    {
        var errors = new List<string>();
        var emailCheck = ValidateEmail(dto.Email);
        if (!emailCheck.valid)
        {
            errors.Add(emailCheck.error);
        }

        var passCheck = ValidatePassword(dto.Parola);
        if (!passCheck.valid)
        {
            errors.Add(passCheck.error);
        }

        if (dto.Parola != dto.ConfirmareParola)
        {
            errors.Add("Parolele nu coincid.");
        }

        if (string.IsNullOrWhiteSpace(dto.Prenume))
        {
            errors.Add("Prenumele este obligatoriu.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nume))
        {
            errors.Add("Numele este obligatoriu.");
        }

        return errors;
    }
}
