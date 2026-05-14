using Mediclin.Data.Models;

namespace Mediclin.Business.Helpers;

public static class PdfExportHelper
{
    public static void ExportReteta(Reteta reteta)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MediClin",
            "Retete");
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, $"reteta-{reteta.Id}-{DateTime.Now:yyyyMMddHHmmss}.txt");
        var content = $"""
            MEDICLIN - RETETA MEDICALA
            Numar reteta: {reteta.Id}
            Pacient ID: {reteta.PacientId}
            Medic ID: {reteta.MedicId}
            Data emitere: {DateHelper.FormatDate(reteta.DataEmitere)}
            Data expirare: {(reteta.DataExpirare.HasValue ? DateHelper.FormatDate(reteta.DataExpirare.Value) : "Nespecificata")}
            Status: {reteta.Status}

            Observatii:
            {reteta.Observatii ?? "Fara observatii."}
            """;

        File.WriteAllText(path, content);
    }
}
