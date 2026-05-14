using Mediclin.Business.DTOs;
using Mediclin.Business.Helpers;
using Mediclin.Business.Services;
using Mediclin.Data.Models;
using Mediclin.Data.Repositories;
using Xunit;

namespace Mediclin.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsUser_WhenCredentialsAreValid()
    {
        var repo = new FakeUtilizatorRepository
        {
            UtilizatorByEmail = new Utilizator
            {
                Id = 1,
                Email = "demo@mediclin.ro",
                ParolaHash = PasswordHelper.HashPassword("Parola123"),
                Rol = "medic",
                Prenume = "Ana",
                Nume = "Ionescu"
            }
        };
        var service = new AuthService(repo);

        var user = await service.LoginAsync(new LoginDto { Email = "demo@mediclin.ro", Parola = "Parola123" });

        Assert.NotNull(user);
        Assert.Equal(1, repo.UpdatedUltimLoginId);
        Assert.Equal(user, service.UtilizatorCurent);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenPasswordInvalid()
    {
        var repo = new FakeUtilizatorRepository
        {
            UtilizatorByEmail = new Utilizator
            {
                Id = 2,
                Email = "p@mediclin.ro",
                ParolaHash = PasswordHelper.HashPassword("AltaParola123"),
                Rol = "pacient",
                Prenume = "Mihai",
                Nume = "Pop"
            }
        };
        var service = new AuthService(repo);

        var user = await service.LoginAsync(new LoginDto { Email = "p@mediclin.ro", Parola = "gresit" });

        Assert.Null(user);
        Assert.Null(service.UtilizatorCurent);
        Assert.Null(repo.UpdatedUltimLoginId);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenParolaHashIsInvalid_DoesNotThrow()
    {
        var repo = new FakeUtilizatorRepository
        {
            UtilizatorByEmail = new Utilizator
            {
                Id = 3,
                Email = "legacy@mediclin.ro",
                ParolaHash = "plaintext-or-invalid",
                Rol = "pacient",
                Prenume = "X",
                Nume = "Y"
            }
        };
        var service = new AuthService(repo);

        var user = await service.LoginAsync(new LoginDto { Email = "legacy@mediclin.ro", Parola = "anything" });

        Assert.Null(user);
        Assert.Null(repo.UpdatedUltimLoginId);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsError_WhenEmailExists()
    {
        var repo = new FakeUtilizatorRepository { EmailExists = true };
        var service = new AuthService(repo);

        var result = await service.RegisterAsync(new RegisterDto
        {
            Email = "existent@mediclin.ro",
            Parola = "Parola123",
            ConfirmareParola = "Parola123",
            Prenume = "Ion",
            Nume = "Marin",
            Telefon = "0711111111"
        });

        Assert.False(result.success);
        Assert.Contains("deja utilizat", result.error);
    }

    private sealed class FakeUtilizatorRepository : IUtilizatorRepository
    {
        public Utilizator? UtilizatorByEmail { get; set; }
        public bool EmailExists { get; set; }
        public int? UpdatedUltimLoginId { get; private set; }
        public Utilizator? CreatedUtilizator { get; private set; }

        public Task<Utilizator?> GetByEmailAsync(string email) => Task.FromResult(UtilizatorByEmail);

        public Task<List<Utilizator>> GetAllAsync() => Task.FromResult(new List<Utilizator>());

        public Task<Utilizator?> GetByIdAsync(int id) => Task.FromResult(UtilizatorByEmail);

        public Task<int> CreateAsync(Utilizator utilizator)
        {
            CreatedUtilizator = utilizator;
            return Task.FromResult(10);
        }

        public Task UpdateUltimLoginAsync(int id)
        {
            UpdatedUltimLoginId = id;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsEmailAsync(string email) => Task.FromResult(EmailExists);

        public Task UpdateAsync(Utilizator entity) => Task.CompletedTask;

        public Task DeleteAsync(int id) => Task.CompletedTask;

        public Task SetActivAsync(int id, bool activ) => Task.CompletedTask;

        public Task UpdateParolaHashAsync(int id, string parolaHash) => Task.CompletedTask;
    }
}
