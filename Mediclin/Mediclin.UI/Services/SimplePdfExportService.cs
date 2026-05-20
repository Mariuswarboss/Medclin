using System.Globalization;
using System.IO;
using System.Text;

namespace Mediclin.UI.Services;

public static class SimplePdfExportService
{
    public static async Task SaveTextPdfAsync(string fileName, IReadOnlyList<string> lines)
    {
        await File.WriteAllBytesAsync(fileName, BuildSimplePdf(lines));
    }

    public static void AddWrapped(List<string> lines, string text, int maxLength = 96)
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
        return normalized.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
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

    private static void WriteAscii(Stream target, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        target.Write(bytes, 0, bytes.Length);
    }
}
