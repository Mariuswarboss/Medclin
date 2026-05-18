using Mediclin.Data.Models;
using System.Linq;

namespace Mediclin.UI.Services;

public static class UserProfileInitializer
{
    /// <param name="medicSpecialitateId">Specialitate aleasă la înregistrare (doar pentru rol medic).</param>
    public static async Task EnsureRoleProfileAsync(ApplicationServices app, Utilizator utilizator, int? medicSpecialitateId = null)
    {
        switch ((utilizator.Rol ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "pacient":
                await EnsurePacientProfileAsync(app, utilizator.Id);
                break;
            case "medic":
                await EnsureMedicProfileAsync(app, utilizator.Id, medicSpecialitateId);
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

    private static async Task EnsureMedicProfileAsync(ApplicationServices app, int utilizatorId, int? preferintaSpecialitateId)
    {
        if (await app.Medici.GetByUtilizatorIdAsync(utilizatorId) is not null)
        {
            return;
        }

        var specialitati = await app.Specialitati.GetAllAsync();
        int specialitateId;
        if (preferintaSpecialitateId is > 0 && specialitati.Any(s => s.Id == preferintaSpecialitateId))
        {
            specialitateId = preferintaSpecialitateId.Value;
        }
        else
        {
            specialitateId = specialitati.FirstOrDefault()?.Id
                ?? throw new InvalidOperationException("Nu există specialități configurate pentru crearea profilului de medic.");
        }

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
