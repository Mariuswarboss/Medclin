using Mediclin.Business.DTOs;
using Mediclin.Business.Helpers;
using Mediclin.Business.Validators;
using Xunit;

namespace Mediclin.Tests.Services;

public class ValidatorTests
{
    [Fact]
    public void RegisterValidator_ReturnsErrors_WhenRequiredFieldsAreMissing()
    {
        var errors = UserValidator.ValidateRegisterDto(new RegisterDto
        {
            Email = "invalid",
            Parola = "weak",
            ConfirmareParola = "different",
            Prenume = "",
            Nume = "",
            Rol = "medic"
        });

        Assert.Contains(errors, e => e.Contains("email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("minimum 8", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("Parolele", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("Prenumele", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("specialitatea", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProgramareValidator_DetectsOverlappingAppointments()
    {
        var start = DateTime.Today.AddDays(1).AddHours(10);
        var existing = new[] { (start: start.AddMinutes(15), durataMin: 30) };

        var result = ProgramareValidator.ValidateNoOverlap(start, 30, existing);

        Assert.False(result.valid);
        Assert.Contains("interval", result.error);
    }

    [Fact]
    public void ProgramareValidator_AcceptsFutureWorkingHour()
    {
        var result = ProgramareValidator.ValidateDateTime(DateTime.Today.AddDays(1).AddHours(10));

        Assert.True(result.valid);
        Assert.Equal(string.Empty, result.error);
    }

    [Fact]
    public void PacientValidator_RejectsFutureBirthDate()
    {
        var result = PacientValidator.ValidateAge(DateOnly.FromDateTime(DateTime.Today.AddDays(1)));

        Assert.False(result.valid);
        Assert.Contains("viitor", result.error);
    }

    [Fact]
    public void PasswordHelper_ReturnsFalse_ForInvalidHash()
    {
        var ok = PasswordHelper.VerifyPassword("Parola123", "not-a-bcrypt-hash");

        Assert.False(ok);
    }
}
