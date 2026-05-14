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
            await _programari.UpdateStatusAsync(c.ProgramareId, "Finalizata");
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
}
