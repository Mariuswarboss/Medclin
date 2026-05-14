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
        var email = dto.Email?.Trim() ?? string.Empty;
        if (email.Length == 0)
        {
            return null;
        }

        var utilizator = await _utilizatorRepository.GetByEmailAsync(email);
        if (utilizator is null)
        {
            return null;
        }

        var passwordOk = await Task.Run(() => PasswordHelper.VerifyPassword(dto.Parola, utilizator.ParolaHash));
        if (!passwordOk)
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

        var validationErrors = Validators.UserValidator.ValidateRegisterDto(dto);
        if (validationErrors.Count > 0)
        {
            return (false, string.Join(Environment.NewLine, validationErrors));
        }

        var hash = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(dto.Parola, 12));

        var utilizator = new Utilizator
        {
            Email = dto.Email,
            ParolaHash = hash,
            Rol = string.IsNullOrWhiteSpace(dto.Rol) ? "pacient" : dto.Rol,
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
