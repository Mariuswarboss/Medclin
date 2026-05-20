using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Mediclin.Data.Models;
using Mediclin.UI.Services;
using Microsoft.Win32;

namespace Mediclin.UI.ViewModels.Patient;

public class LabResultsViewModel : BaseViewModel
{
    private readonly ApplicationServices _app;
    private int _pacientId;

    private DateTime _dataLimita = DateTime.Today;
    public DateTime DataLimita
    {
        get => _dataLimita;
        set => SetProperty(ref _dataLimita, value);
    }

    private string _statusMesaj = string.Empty;
    public string StatusMesaj
    {
        get => _statusMesaj;
        set => SetProperty(ref _statusMesaj, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public ObservableCollection<RezultatAnaliza> Rezultate { get; } = new();
    public ObservableCollection<ValoareAnaliza> UltimeleValori { get; } = new();

    public ICommand DeleteUntilCommand { get; }
    public ICommand ExportCommand { get; }

    public LabResultsViewModel(ApplicationServices app, Utilizator utilizator)
    {
        _app = app;
        DeleteUntilCommand = new AsyncRelayCommand(_ => DeleteUntilAsync());
        ExportCommand = new AsyncRelayCommand(_ => ExportPdfAsync(), _ => !IsBusy);
        _ = LoadAsync(utilizator);
    }

    private async Task LoadAsync(Utilizator utilizator)
    {
        try
        {
            IsBusy = true;
            var p = await _app.Pacienti.GetByUtilizatorIdAsync(utilizator.Id);
            if (p is null) return;

            _pacientId = p.Id;
            await RefreshListAsync();
        }
        catch
        {
            // ignorat
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshListAsync()
    {
        Rezultate.Clear();
        UltimeleValori.Clear();

        var list = await _app.AnalizeRepo.GetByPacientAsync(_pacientId);
        foreach (var r in list)
            Rezultate.Add(r);

        var first = list.FirstOrDefault();
        if (first is not null)
        {
            foreach (var v in await _app.AnalizeRepo.GetValoriAsync(first.Id))
                UltimeleValori.Add(v);
        }
    }

    private async Task DeleteUntilAsync()
    {
        if (_pacientId == 0) return;

        var confirm = MessageBox.Show(
            $"Ștergi toate rezultatele analizelor până la {DataLimita:dd.MM.yyyy} (inclusiv)?\n\nAceastă acțiune este ireversibilă.",
            "Confirmare ștergere",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            IsBusy = true;
            StatusMesaj = string.Empty;

            int nr = await _app.AnalizeRepo.DeleteByPacientUntilAsync(_pacientId, DataLimita);
            await RefreshListAsync();

            StatusMesaj = nr == 0
                ? "Nu există rezultate în intervalul selectat."
                : $"{nr} rezultat(e) șters(e) cu succes.";
        }
        catch (Exception ex)
        {
            StatusMesaj = $"Eroare: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportPdfAsync()
    {
        if (_pacientId == 0 || Rezultate.Count == 0)
        {
            StatusMesaj = "Nu exista rezultate de exportat.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Salveaza rezultatele analizelor",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"MediClin_Analize_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
            AddExtension = true,
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMesaj = "Se genereaza PDF-ul...";

            var lines = new List<string>
            {
                "MediClin - Rezultate analize",
                $"Generat la: {DateTime.Now:dd.MM.yyyy HH:mm}",
                string.Empty
            };

            foreach (var rezultat in Rezultate.OrderByDescending(r => r.DataRecoltare))
            {
                lines.Add($"Data recoltare: {rezultat.DataRecoltare:dd.MM.yyyy}");
                lines.Add($"Laborator: {rezultat.Laborator ?? "Nespecificat"}");
                lines.Add($"Teste: {rezultat.NrValori} | Anormale: {rezultat.Anormale}");

                if (!string.IsNullOrWhiteSpace(rezultat.Interpretare))
                {
                    AddWrapped(lines, $"Interpretare: {rezultat.Interpretare}", 92);
                }

                var valori = await _app.AnalizeRepo.GetValoriAsync(rezultat.Id);
                foreach (var valoare in valori)
                {
                    var interval = FormatReferenceRange(valoare);
                    var status = string.IsNullOrWhiteSpace(valoare.Status) ? "normal" : valoare.Status;
                    AddWrapped(lines, $"- {valoare.TestNume}: {valoare.Valoare} {valoare.Unitate} | Ref: {interval} | Status: {status}", 96);
                }

                lines.Add(string.Empty);
            }

            await File.WriteAllBytesAsync(dialog.FileName, BuildSimplePdf(lines));
            StatusMesaj = $"PDF salvat: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMesaj = $"Eroare export PDF: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatReferenceRange(ValoareAnaliza valoare)
    {
        return (valoare.ValMinNormal, valoare.ValMaxNormal) switch
        {
            (not null, not null) => $"{valoare.ValMinNormal:0.##}-{valoare.ValMaxNormal:0.##}",
            (not null, null) => $">= {valoare.ValMinNormal:0.##}",
            (null, not null) => $"<= {valoare.ValMaxNormal:0.##}",
            _ => "-"
        };
    }

    private static void AddWrapped(List<string> lines, string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            lines.Add(text);
            return;
        }

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length + word.Length + 1 > maxLength)
            {
                lines.Add(current.ToString());
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }
    }

    private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
    {
        const int linesPerPage = 44;
        var pages = lines
            .Chunk(linesPerPage)
            .Select(chunk => chunk.ToList())
            .DefaultIfEmpty(new List<string> { "Nu exista date." })
            .ToList();

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            string.Empty,
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var pageObjectNumbers = new List<int>();
        foreach (var pageLines in pages)
        {
            var pageObjectNumber = objects.Count + 1;
            var contentObjectNumber = pageObjectNumber + 1;
            pageObjectNumbers.Add(pageObjectNumber);

            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add(BuildPdfStream(pageLines));
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(n => $"{n} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n");

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xrefOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();

        static void WriteAscii(Stream target, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            target.Write(bytes, 0, bytes.Length);
        }
    }

    private static string BuildPdfStream(IReadOnlyList<string> lines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 11 Tf");
        content.AppendLine("50 790 Td");
        content.AppendLine("14 TL");

        foreach (var line in lines)
        {
            content.Append('(').Append(EscapePdfText(line)).AppendLine(") Tj");
            content.AppendLine("T*");
        }

        content.Append("ET");
        var body = content.ToString();
        return $"<< /Length {Encoding.ASCII.GetByteCount(body)} >>\nstream\n{body}\nendstream";
    }

    private static string EscapePdfText(string value)
    {
        var normalized = RemoveDiacritics(value);
        return normalized
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static string RemoveDiacritics(string text)
    {
        var mapped = text
            .Replace('ă', 'a').Replace('Ă', 'A')
            .Replace('â', 'a').Replace('Â', 'A')
            .Replace('î', 'i').Replace('Î', 'I')
            .Replace('ș', 's').Replace('Ș', 'S')
            .Replace('ş', 's').Replace('Ş', 'S')
            .Replace('ț', 't').Replace('Ț', 'T')
            .Replace('ţ', 't').Replace('Ţ', 'T');

        var formD = mapped.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var character in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && character <= 127)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
