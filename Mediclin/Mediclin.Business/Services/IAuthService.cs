using Mediclin.Business.DTOs;
using Mediclin.Data.Models;

namespace Mediclin.Business.Services;

public interface IAuthService
{
    Task<Utilizator?> LoginAsync(LoginDto dto);
    Task<(bool success, string error)> RegisterAsync(RegisterDto dto);
    void Logout();
    Utilizator? UtilizatorCurent { get; }
}
