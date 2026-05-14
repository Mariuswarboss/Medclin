using Mediclin.Data.Models;

namespace Mediclin.UI.Services;

public static class UserProfileInitializer
{
    public static async Task EnsureRoleProfileAsync(ApplicationServices app, Utilizator utilizator)
    {
        switch ((utilizator.Rol ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "pacient":
                await EnsurePacientProfileAsync(app, utilizator.Id);
                break;
            case "medic":
                await EnsureMedicProfileAsync(app, utilizator.Id);
                break;
        }
    }

    private static async Task EnsurePacientProfileAsync(ApplicationServices app, int utilizatorId)
    {
        if (await app.Pacienti.GetByUtilizatorIdAsync(utilizatorId) is not null)
        {
            return;
        }

        await app.Pacienti.CreateAsync(new Pacient
        {
            UtilizatorId = utilizatorId
        });
    }

    private static async Task EnsureMedicProfileAsync(ApplicationServices app, int utilizatorId)
    {
        if (await app.Medici.GetByUtilizatorIdAsync(utilizatorId) is not null)
        {
            return;
        }

        var specialitati = await app.Specialitati.GetAllAsync();
        var specialitateId = specialitati.FirstOrDefault()?.Id
            ?? throw new InvalidOperationException("Nu exista specialitati configurate pentru crearea profilului de medic.");

        await app.Medici.CreateAsync(new Medic
        {
            UtilizatorId = utilizatorId,
            SpecialitateId = specialitateId,
            CodMedic = $"MED-{utilizatorId:D5}",
            Titlu = "Dr.",
            DurataConsultatie = 30,
            TarifConsultatie = 0
        });
    }
}
