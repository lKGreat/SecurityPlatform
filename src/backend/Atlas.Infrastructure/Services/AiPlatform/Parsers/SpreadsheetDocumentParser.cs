using System.Globalization;
using System.Text;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using ClosedXML.Excel;

namespace Atlas.Infrastructure.Services.AiPlatform.Parsers;

public sealed class SpreadsheetDocumentParser : IDocumentParser
{
    public bool CanParse(string? contentType, string extension)
    {
        var normalized = extension.ToLowerInvariant();
        return normalized is ".csv" or ".xlsx" or ".xls"
               || string.Equals(contentType, "text/csv", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => await ParseCsvAsync(fileStream, fileName, ct),
            ".xlsx" or ".xls" => ParseExcel(fileStream, fileName, ct),
            _ => throw new NotSupportedException($"Unsupported spreadsheet extension: {extension}")
        };
    }

    private static async Task<ParsedDocument> ParseCsvAsync(Stream stream, string fileName, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct);
        return new ParsedDocument(
            text,
            Path.GetFileNameWithoutExtension(fileName),
            1,
            new Dictionary<string, string>
            {
                ["fileName"] = fileName,
                ["parser"] = nameof(SpreadsheetDocumentParser),
                ["format"] = "csv"
            });
    }

    private static ParsedDocument ParseExcel(Stream stream, string fileName, CancellationToken ct)
    {
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        var builder = new StringBuilder();
        var totalRows = 0;

        foreach (var sheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();
            builder.AppendLine($"# Sheet: {sheet.Name}");
            var range = sheet.RangeUsed();
            if (range is null)
            {
                continue;
            }

            foreach (var row in range.RowsUsed())
            {
                var values = row.CellsUsed().Select(cell => cell.GetFormattedString(CultureInfo.InvariantCulture));
                builder.AppendLine(string.Join('\t', values));
                totalRows++;
            }

            builder.AppendLine();
        }

        return new ParsedDocument(
            builder.ToString().Trim(),
            Path.GetFileNameWithoutExtension(fileName),
            Math.Max(totalRows, 1),
            new Dictionary<string, string>
            {
                ["fileName"] = fileName,
                ["parser"] = nameof(SpreadsheetDocumentParser),
                ["format"] = "excel"
            });
    }
}
