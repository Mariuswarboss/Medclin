using Mediclin.Business.DTOs;
using Mediclin.Business.Helpers;
using Mediclin.Data.Models;
using Mediclin.Data.Repositories;

namespace Mediclin.Business.Services;

public class AuthService : IAuthService
{
    private readonly IUtilizatorRepository _utilizatorRepository;

    public AuthService(IUtilizatorRepository utilizatorRepository)
    {
        _utilizatorRepository = utilizatorRepository;
    }

    public Utilizator? UtilizatorCurent { get; private set; }

    public async Task<Utilizator?> LoginAsync(LoginDto dto)
    {
        var utilizator = await _utilizatorRepository.GetByEmailAsync(dto.Email);
        if (utilizator is null)
        {
            return null;
        }

        if (!PasswordHelper.VerifyPassword(dto.Parola, utilizator.ParolaHash))
        {
            return null;
        }

        await _utilizatorRepository.UpdateUltimLoginAsync(utilizator.Id);
        UtilizatorCurent = utilizator;
        return utilizator;
    }

    public async Task<(bool success, string error)> RegisterAsync(RegisterDto dto)
    {
        if (await _utilizatorRepository.ExistsEmailAsync(dto.Email))
        {
            return (false, "Email-ul este deja utilizat.");
        }

        if (dto.Parola != dto.ConfirmareParola)
        {
            return (false, "Parolele nu coincid.");
        }

        if (dto.Parola.Length < 8)
        {
            return (false, "Parola trebuie sa aiba minimum 8 caractere.");
        }

        var utilizator = new Utilizator
        {
            Email = dto.Email,
            ParolaHash = PasswordHelper.HashPassword(dto.Parola),
            Rol = "pacient",
            Prenume = dto.Prenume,
            Nume = dto.Nume,
            Telefon = string.IsNullOrWhiteSpace(dto.Telefon) ? null : dto.Telefon,
            Activ = true
        };

        _ = await _utilizatorRepository.CreateAsync(utilizator);
        return (true, string.Empty);
    }

    public void Logout()
    {
        UtilizatorCurent = null;
    }
}
