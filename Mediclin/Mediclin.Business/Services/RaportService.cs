using Mediclin.Business.DTOs;
using Mediclin.Data.Context;

namespace Mediclin.Business.Services;

public class RaportService
{
    private readonly DatabaseContext _db;

    public RaportService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<RaportDto> GetRaportZilnicAsync(int medicId, DateTime date)
    {
        try
        {
            const string sql = """
                SELECT COUNT(*) AS total,
                       SUM(CASE WHEN status='Finalizata' THEN 1 ELSE 0 END) AS finalizate,
                       SUM(CASE WHEN status='Neprezentata' THEN 1 ELSE 0 END) AS neprezentate
                FROM programari
                WHERE medic_id=@mid AND DATE(data_ora)=DATE(@data)
                """;
            var rows = await _db.QueryAsync(sql, new Dictionary<string, object>
            {
                ["@mid"] = medicId,
                ["@data"] = date.Date
            });
            var row = rows[0];
            return new RaportDto
            {
                TotalProgramari = Convert.ToInt32(row["total"]),
                ProgramariFinalizate = row["finalizate"] == DBNull.Value ? 0 : Convert.ToInt32(row["finalizate"]),
                ProgramariNeprezentate = row["neprezentate"] == DBNull.Value ? 0 : Convert.ToInt32(row["neprezentate"])
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Raportul zilnic nu a putut fi generat.", ex);
        }
    }

    public async Task<RaportDto> GetRaportLunarAsync(int medicId, int an, int luna)
    {
        try
        {
            var start = new DateTime(an, luna, 1);
            var end = start.AddMonths(1).AddDays(-1);
            const string progSql = """
                SELECT COUNT(*) AS total,
                       SUM(CASE WHEN status='Finalizata' THEN 1 ELSE 0 END) AS finalizate,
                       SUM(CASE WHEN status='Neprezentata' THEN 1 ELSE 0 END) AS neprezentate
                FROM programari
                WHERE medic_id=@mid AND YEAR(data_ora)=@an AND MONTH(data_ora)=@luna
                """;
            var p = new Dictionary<string, object> { ["@mid"] = medicId, ["@an"] = an, ["@luna"] = luna };
            var rows = await _db.QueryAsync(progSql, p);
            var row = rows[0];
            var total = Convert.ToInt32(row["total"]);
            var neprez = row["neprezentate"] == DBNull.Value ? 0 : Convert.ToInt32(row["neprezentate"]);

            const string tarifSql = "SELECT tarif_consultatie FROM medici WHERE id=@mid LIMIT 1";
            var tarif = Convert.ToDecimal(await _db.ScalarAsync(tarifSql, new Dictionary<string, object> { ["@mid"] = medicId }));
            var finalizate = row["finalizate"] == DBNull.Value ? 0 : Convert.ToInt32(row["finalizate"]);
            var venitEstimat = finalizate * tarif;

            const string perDaySql = """
                SELECT DATE(data_ora) AS data, COUNT(*) AS total
                FROM programari
                WHERE medic_id=@mid AND YEAR(data_ora)=@an AND MONTH(data_ora)=@luna
                GROUP BY DATE(data_ora)
                ORDER BY data
                """;
            var perDay = await _db.QueryAsync(perDaySql, p);

            return new RaportDto
            {
                TotalProgramari = total,
                ProgramariFinalizate = finalizate,
                ProgramariNeprezentate = neprez,
                RataNeprezentare = total == 0 ? 0 : Math.Round(neprez * 100m / total, 2),
                VenitEstimat = venitEstimat,
                ConsultatiiPerZi = perDay.Select(r => new ConsultatiiPerZiDto
                {
                    Data = Convert.ToDateTime(r["data"]),
                    Total = Convert.ToInt32(r["total"])
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Raportul lunar nu a putut fi generat.", ex);
        }
    }

    public async Task<RaportDto> GetStatisticiAdminAsync()
    {
        try
        {
            var u = Convert.ToInt32(await _db.ScalarAsync(
                "SELECT COUNT(*) FROM utilizatori WHERE activ=1", null));
            var m = Convert.ToInt32(await _db.ScalarAsync(
                "SELECT COUNT(*) FROM medici WHERE verificat=1", null));
            var p = Convert.ToInt32(await _db.ScalarAsync(
                "SELECT COUNT(*) FROM programari WHERE DATE(data_ora)=CURDATE()", null));
            return new RaportDto
            {
                UtilizatoriActivi = u,
                MediciVerificati = m,
                ProgramariAstazi = p
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Statisticile admin nu au putut fi încărcate.", ex);
        }
    }

    public Task<RaportDto> GetRaportZilnicAsync(DateTime date) => BuildRaportAsync(date.Date, date.Date.AddDays(1).AddTicks(-1), null);

    public Task<RaportDto> GetRaportLunarAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddTicks(-1);
        return BuildRaportAsync(start, end, null);
    }

    public Task<RaportDto> GetStatisticiMedicAsync(int medicId, DateTime start, DateTime end)
    {
        return BuildRaportAsync(start, end, medicId);
    }

    private async Task<RaportDto> BuildRaportAsync(DateTime start, DateTime end, int? medicId)
    {
        const string totalSql = """
            SELECT COUNT(1) FROM consultatii
            WHERE data_consultatie BETWEEN @start AND @end
              AND (@medic_id IS NULL OR medic_id=@medic_id)
            """;
        const string newPatientsSql = "SELECT COUNT(1) FROM pacienti WHERE creat_la BETWEEN @start AND @end";
        const string absentSql = """
            SELECT COUNT(1) FROM programari
            WHERE data_ora BETWEEN @start AND @end AND status='Neprezentata'
              AND (@medic_id IS NULL OR medic_id=@medic_id)
            """;
        const string allAppointmentsSql = """
            SELECT COUNT(1) FROM programari
            WHERE data_ora BETWEEN @start AND @end
              AND (@medic_id IS NULL OR medic_id=@medic_id)
            """;
        const string revenueSql = """
            SELECT COALESCE(SUM(m.tarif_consultatie),0)
            FROM consultatii c
            INNER JOIN medici m ON m.id = c.medic_id
            WHERE c.data_consultatie BETWEEN @start AND @end
              AND (@medic_id IS NULL OR c.medic_id=@medic_id)
            """;
        var p = new Dictionary<string, object> { ["@start"] = start, ["@end"] = end, ["@medic_id"] = (object?)medicId ?? DBNull.Value };
        var total = Convert.ToInt32(await _db.ScalarAsync(totalSql, p));
        var absente = Convert.ToInt32(await _db.ScalarAsync(absentSql, p));
        var programari = Convert.ToInt32(await _db.ScalarAsync(allAppointmentsSql, p));
        var venit = Convert.ToDecimal(await _db.ScalarAsync(revenueSql, p));
        var pacientiNoi = Convert.ToInt32(await _db.ScalarAsync(newPatientsSql, p));

        const string perDaySql = """
            SELECT DATE(data_consultatie) AS data, COUNT(1) AS total
            FROM consultatii
            WHERE data_consultatie BETWEEN @start AND @end
              AND (@medic_id IS NULL OR medic_id=@medic_id)
            GROUP BY DATE(data_consultatie)
            ORDER BY data
            """;
        var perDay = await _db.QueryAsync(perDaySql, p);

        return new RaportDto
        {
            TotalConsultatii = total,
            PacientiNoi = pacientiNoi,
            RataNeprezentare = programari == 0 ? 0 : Math.Round(absente * 100m / programari, 2),
            VenitEstimat = venit,
            ConsultatiiPerZi = perDay.Select(r => new ConsultatiiPerZiDto
            {
                Data = Convert.ToDateTime(r["data"]),
                Total = Convert.ToInt32(r["total"])
            }).ToList()
        };
    }
}
