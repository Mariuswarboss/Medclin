using Mediclin.Data.Models;
using Mediclin.Data.Repositories;

namespace Mediclin.Business.Services;

public class RetetaService
{
    private readonly RetetaRepository _retete;
    private readonly PacientRepository _pacienti;
    private readonly NotificareService _notificari;
    private readonly JurnalRepository _jurnal;
    private readonly Func<int?> _utilizatorCurentId;

    public RetetaService(
        RetetaRepository retete,
        PacientRepository pacienti,
        NotificareService notificari,
        JurnalRepository jurnal,
        Func<int?> utilizatorCurentId)
    {
        _retete = retete;
        _pacienti = pacienti;
        _notificari = notificari;
        _jurnal = jurnal;
        _utilizatorCurentId = utilizatorCurentId;
    }

    public async Task<(bool ok, string error, int id)> EmiteRetetaAsync(
        int consultatieId, int pacientId, int medicId,
        List<Medicament> medicamente, string observatii)
    {
        var retetaId = 0;
        try
        {
            if (medicamente is null || medicamente.Count == 0)
            {
                return (false, "Adăugați cel puțin un medicament pe rețetă.", 0);
            }

            var azi = DateTime.Today;
            var reteta = new Reteta
            {
                ConsultatieId = consultatieId,
                PacientId = pacientId,
                MedicId = medicId,
                DataEmitere = azi,
                DataExpirare = azi.AddDays(30),
                Status = "Activa",
                Observatii = string.IsNullOrWhiteSpace(observatii) ? null : observatii
            };

            retetaId = await _retete.CreateAsync(reteta);
            foreach (var m in medicamente)
            {
                m.RetetaId = retetaId;
                await _retete.AddMedicamentAsync(m);
            }

            var pacient = await _pacienti.GetByIdAsync(pacientId);
            if (pacient is not null)
            {
                await _notificari.CreateAsync(
                    pacient.UtilizatorId,
                    "Rețetă nouă",
                    "Rețetă nouă disponibilă în contul dumneavoastră.",
                    "Reteta",
                    null);
            }

            await _jurnal.WriteAsync(_utilizatorCurentId(), "EmiteReteta", "Retete", $"Reteta #{retetaId}", "127.0.0.1", "Info");
            return (true, string.Empty, retetaId);
        }
        catch (Exception ex)
        {
            if (retetaId > 0)
            {
                try
                {
                    await _retete.DeleteAsync(retetaId);
                }
                catch
                {
                    // Daca stergerea compensatorie esueaza, pastram eroarea initiala.
                }
            }

            await _jurnal.WriteAsync(_utilizatorCurentId(), "EmiteRetetaAsync", "Retete", ex.Message, "127.0.0.1", "Error");
            return (false, ex.Message, 0);
        }
    }

    public async Task<int> EmiteRetetaAsync(Reteta reteta, IEnumerable<Medicament> medicamente)
    {
        var list = medicamente.ToList();
        var (ok, err, id) = await EmiteRetetaAsync(
            reteta.ConsultatieId,
            reteta.PacientId,
            reteta.MedicId,
            list,
            reteta.Observatii ?? string.Empty);
        if (!ok)
        {
            throw new InvalidOperationException(err);
        }

        return id;
    }

    public async Task<List<Reteta>> GetRetetePacientAsync(int pacientId)
    {
        try
        {
            var retete = await _retete.GetByPacientAsync(pacientId);
            foreach (var r in retete)
            {
                r.Medicamente = await _retete.GetMedicamenteAsync(r.Id);
            }

            return retete;
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "GetRetetePacientAsync", "Retete", ex.Message, "127.0.0.1", "Error");
            throw;
        }
    }

    public async Task CheckSiActualizeazaExpirareAsync()
    {
        try
        {
            var expirate = await _retete.GetActiveExpiredBeforeTodayAsync();
            foreach (var r in expirate)
            {
                await _retete.UpdateStatusAsync(r.Id, "Expirata");
            }
        }
        catch (Exception ex)
        {
            await _jurnal.WriteAsync(_utilizatorCurentId(), "CheckSiActualizeazaExpirareAsync", "Retete", ex.Message, "127.0.0.1", "Error");
            throw;
        }
    }

    public async Task<Dictionary<string, int>> GetRaportReteteAsync(int pacientId)
    {
        var retete = await _retete.GetByPacientAsync(pacientId);
        return retete.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task CheckExpirareAsync(int pacientId)
    {
        var active = await _retete.GetActiveAsync(pacientId);
        foreach (var reteta in active.Where(r => r.DataExpirare.HasValue && r.DataExpirare.Value.Date < DateTime.Today))
        {
            await _retete.UpdateStatusAsync(reteta.Id, "Expirata");
        }
    }
}
