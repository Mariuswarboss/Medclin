using Mediclin.Data.Models;
using Mediclin.Data.Repositories;

namespace Mediclin.Business.Services;

public class ProgramareService
{
    private static readonly HashSet<string> StatusPermise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Programata", "In_asteptare", "In_cabinet", "Finalizata", "Anulata", "Neprezentata", "Confirmata"
    };

    private static readonly string[] ZileRo = { "Duminica", "Luni", "Marti", "Miercuri", "Joi", "Vineri", "Sambata" };

    private readonly ProgramareRepository _programari;
    private readonly MedicRepository _medici;
    private readonly PacientRepository _pacienti;
    private readonly NotificareService _notificari;
    private readonly JurnalRepository _jurnal;
    private readonly Func<int?> _utilizatorCurentId;

    public ProgramareService(
        ProgramareRepository programari,
        MedicRepository medici,
        PacientRepository pacienti,
        NotificareService notificari,
        JurnalRepository jurnal,
        Func<int?> utilizatorCurentId)
    {
        _programari = programari;
        _medici = medici;
        _pacienti = pacienti;
        _notificari = notificari;
        _jurnal = jurnal;
        _utilizatorCurentId = utilizatorCurentId;
    }

    private static string ZiInRomana(DateTime d) => ZileRo[(int)d.DayOfWeek];

    public async Task<List<DateTime>> GetSloturiDisponibileAsync(int medicId, DateTime date)
    {
        try
        {
            var zi = ZiInRomana(date);
            var program = (await _medici.GetProgramAsync(medicId))
                .Where(p => string.Equals(p.ZiSaptamana, zi, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (program.Count == 0)
            {
                return new List<DateTime>();
            }

            var medic = await _medici.GetByIdAsync(medicId);
            if (medic is null)
            {
                return new List<DateTime>();
            }

            var dur = medic.DurataConsultatie > 0 ? medic.DurataConsultatie : 30;
            var slots = new List<DateTime>();

            foreach (var slotZi in program)
            {
                var ziStart = date.Date + slotZi.OraStart;
                var ziEnd = date.Date + slotZi.OraSfarsit;
                for (var t = ziStart; t.AddMinutes(dur) <= ziEnd; t = t.AddMinutes(dur))
                {
                    if (t <= DateTime.Now)
                    {
                        continue;
                    }

                    if (await _programari.HasOverlapAsync(medicId, t, dur))
                    {
                        continue;
                    }

                    slots.Add(t);
                }
            }

            return slots;
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "GetSloturiDisponibileAsync", "Programari", ex.Message, "127.0.0.1", "Error");
            throw new InvalidOperationException("Sloturile disponibile nu au putut fi calculate.", ex);
        }
    }

    public async Task<(bool ok, string error)> CreateProgramareAsync(
        int pacientId, int medicId, DateTime dataOra, string tip, string motivVizita)
    {
        try
        {
            if (dataOra < DateTime.Now.AddMinutes(-2))
            {
                return (false, "Data și ora programării nu pot fi în trecut.");
            }

            var medic = await _medici.GetByIdAsync(medicId);
            if (medic is null)
            {
                return (false, "Medicul selectat nu există.");
            }

            var dur = medic.DurataConsultatie > 0 ? medic.DurataConsultatie : 30;
            if (await _programari.HasOverlapAsync(medicId, dataOra, dur))
            {
                return (false, "Intervalul selectat se suprapune cu o altă programare.");
            }

            var pacient = await _pacienti.GetByIdAsync(pacientId);
            if (pacient is null)
            {
                return (false, "Pacientul selectat nu există.");
            }

            var programare = new Programare
            {
                PacientId = pacientId,
                MedicId = medicId,
                DataOra = dataOra,
                DurataMin = dur,
                Tip = string.IsNullOrWhiteSpace(tip) ? "Initiala" : tip,
                Status = "Programata",
                MotivVizita = motivVizita
            };

            _ = await _programari.CreateAsync(programare);

            await _notificari.CreateAsync(
                pacient.UtilizatorId,
                "Programare nouă",
                $"Aveți o programare la {dataOra:dd.MM.yyyy HH:mm}.",
                "Programare",
                null);

            await _notificari.CreateAsync(
                medic.UtilizatorId,
                "Programare nouă",
                $"Programare cu pacientul {pacient.NumeComplet} la {dataOra:dd.MM.yyyy HH:mm}.",
                "Programare",
                null);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "CreateProgramareAsync", "Programari", ex.Message, "127.0.0.1", "Error");
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string error, int id)> CreateProgramareAsync(Programare programare)
    {
        var (ok, err) = await CreateProgramareAsync(
            programare.PacientId,
            programare.MedicId,
            programare.DataOra,
            programare.Tip,
            programare.MotivVizita ?? string.Empty);
        if (!ok)
        {
            return (false, err, 0);
        }

        var recent = await _programari.GetByMedicDayAsync(programare.MedicId, programare.DataOra.Date);
        var found = recent
            .Where(x => x.PacientId == programare.PacientId)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault(x => Math.Abs((x.DataOra - programare.DataOra).TotalSeconds) < 90);
        return (true, string.Empty, found?.Id ?? 0);
    }

    public Task CancelProgramareAsync(int id, string motiv) => _programari.CancelAsync(id, motiv);

    public Task<List<Programare>> GetTodayForMedicAsync(int medicId) => _programari.GetTodayAsync(medicId);

    public async Task UpdateStatusAsync(int id, string status)
    {
        try
        {
            if (!StatusPermise.Contains(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Statusul programării nu este permis.");
            }

            await _programari.UpdateStatusAsync(id, status);
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "UpdateStatusAsync", "Programari", ex.Message, "127.0.0.1", "Error");
            throw;
        }
    }
}
