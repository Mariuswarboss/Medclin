using Mediclin.Data.Models;
using Mediclin.Data.Repositories;

namespace Mediclin.Business.Services;

public class ConsultatieService
{
    private readonly ConsultatieRepository _consultatii;
    private readonly ProgramareRepository _programari;
    private readonly JurnalRepository _jurnal;
    private readonly Func<int?> _utilizatorCurentId;

    public ConsultatieService(
        ConsultatieRepository consultatii,
        ProgramareRepository programari,
        JurnalRepository jurnal,
        Func<int?> utilizatorCurentId)
    {
        _consultatii = consultatii;
        _programari = programari;
        _jurnal = jurnal;
        _utilizatorCurentId = utilizatorCurentId;
    }

    public async Task<Consultatie> StartConsultatieAsync(int programareId, int pacientId, int medicId)
    {
        try
        {
            var existing = await _consultatii.GetByProgramareAsync(programareId);
            if (existing is not null)
            {
                await _programari.UpdateStatusAsync(programareId, "In_cabinet");
                return existing;
            }

            var programare = await _programari.GetByIdAsync(programareId)
                ?? throw new InvalidOperationException("Programarea nu există.");

            var consultatie = new Consultatie
            {
                ProgramareId = programare.Id,
                PacientId = pacientId,
                MedicId = medicId,
                DataConsultatie = DateTime.Now
            };
            consultatie.Id = await _consultatii.CreateAsync(consultatie);
            await _programari.UpdateStatusAsync(programareId, "In_cabinet");
            await _jurnal.WriteAsync(_utilizatorCurentId(), "StartConsultatie", "Consultatii",
                $"Programare #{programareId}", "127.0.0.1", "Info");
            return consultatie;
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "StartConsultatieAsync", "Consultatii", ex.Message, "127.0.0.1", "Error");
            throw;
        }
    }

    public async Task<(bool ok, string error)> SaveConsultatieAsync(Consultatie c)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(c.Simptome))
            {
                return (false, "Simptomele sunt obligatorii.");
            }

            await _consultatii.UpdateAsync(c);

            // Actualizăm statusul programării doar dacă există una atașată
            if (c.ProgramareId > 0)
            {
                await _programari.UpdateStatusAsync(c.ProgramareId, "Finalizata");
            }

            await _jurnal.WriteAsync(_utilizatorCurentId(), "SaveConsultatie", "Consultatii",
                $"Consultatie #{c.Id} salvată", "127.0.0.1", "Info");
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "SaveConsultatieAsync", "Consultatii", ex.Message, "127.0.0.1", "Error");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Creează o consultație directă, fără programare prealabilă.
    /// Folosită când medicul deschide fișa și inițiază manual o consultație nouă.
    /// </summary>
    public async Task<Consultatie> StartConsultatieFaraProgramareAsync(int pacientId, int medicId)
    {
        try
        {
            var consultatie = new Consultatie
            {
                ProgramareId = 0,
                PacientId = pacientId,
                MedicId = medicId,
                DataConsultatie = DateTime.Now
            };
            consultatie.Id = await _consultatii.CreateAsync(consultatie);
            await _jurnal.WriteAsync(_utilizatorCurentId(), "StartConsultatieFaraProgramare", "Consultatii",
                $"Consultatie directa pacient #{pacientId}", "127.0.0.1", "Info");
            return consultatie;
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "StartConsultatieFaraProgramareAsync", "Consultatii", ex.Message, "127.0.0.1", "Error");
            throw;
        }
    }

    public async Task<List<Consultatie>> GetIstoricAsync(int pacientId)
    {
        try
        {
            return await _consultatii.GetByPacientAsync(pacientId);
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "GetIstoricAsync", "Consultatii", ex.Message, "127.0.0.1", "Error");
            throw;
        }
    }

    public Task<List<Consultatie>> GetIstoricPacientAsync(int pacientId) => GetIstoricAsync(pacientId);

    /// <summary>
    /// Salvează consultația ca draft fără validări obligatorii.
    /// Dacă nu există ancora în DB, o creează.
    /// </summary>
    public async Task<(bool ok, string error)> SaveDraftAsync(Consultatie c)
    {
        try
        {
            if (c.Id <= 0)
            {
                // Creare nouă
                c.Id = await _consultatii.CreateAsync(c);
            }
            else
            {
                await _consultatii.UpdateAsync(c);
            }

            await _jurnal.WriteAsync(_utilizatorCurentId(), "SaveDraft", "Consultatii",
                $"Draft consultatie #{c.Id} salvat", "127.0.0.1", "Info");
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "SaveDraftAsync", "Consultatii",
                ex.Message, "127.0.0.1", "Error");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Finalizează consultația și trimite rezumatul la pacient prin notificare.
    /// </summary>
    public async Task<(bool ok, string error)> TrimiteRezultatAsync(
        Consultatie c,
        int pacientUtilizatorId,
        NotificareRepository notificari)
    {
        try
        {
            // Salvăm mai întâi
            if (c.Id <= 0)
                c.Id = await _consultatii.CreateAsync(c);
            else
                await _consultatii.UpdateAsync(c);

            // Finalizăm programarea dacă există
            if (c.ProgramareId > 0)
                await _programari.UpdateStatusAsync(c.ProgramareId, "Finalizata");

            // Construim mesajul pentru pacient (fara emoji pentru compatibilitate MySQL)
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Rezultat consultatie");
            sb.AppendLine($"Data: {c.DataConsultatie:dd.MM.yyyy HH:mm}");
            if (!string.IsNullOrWhiteSpace(c.DiagnosticText))
                sb.AppendLine($"Diagnostic: {c.DiagnosticCod} - {c.DiagnosticText}");
            if (!string.IsNullOrWhiteSpace(c.Recomandari))
                sb.AppendLine($"Recomandari: {c.Recomandari}");
            if (!string.IsNullOrWhiteSpace(c.TensiuneArteriala))
                sb.AppendLine($"Tensiune: {c.TensiuneArteriala}");

            await notificari.CreateAsync(
                pacientUtilizatorId,
                "Rezultat consultatie disponibil",
                sb.ToString().Trim(),
                "Programare");

            await _jurnal.WriteAsync(_utilizatorCurentId(), "TrimiteRezultat", "Consultatii",
                $"Consultatie #{c.Id} trimisă la pacient #{pacientUtilizatorId}", "127.0.0.1", "Info");

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "TrimiteRezultatAsync", "Consultatii",
                ex.Message, "127.0.0.1", "Error");
            return (false, ex.Message);
        }
    }
}
