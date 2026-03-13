using System.Text;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using UglyToad.PdfPig;

namespace Atlas.Infrastructure.Services.AiPlatform.Parsers;

public sealed class PdfDocumentParser : IDocumentParser
{
    public bool CanParse(string? contentType, string extension)
    {
        return string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ParsedDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        fileStream.Position = 0;
        using var memory = new MemoryStream();
        fileStream.CopyTo(memory);
        memory.Position = 0;

        using var pdf = PdfDocument.Open(memory);
        var builder = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            builder.AppendLine(page.Text);
        }

        var text = builder.ToString().Trim();
        var metadata = new Dictionary<string, string>
        {
            ["fileName"] = fileName,
            ["parser"] = nameof(PdfDocumentParser)
        };

        return Task.FromResult(
            new ParsedDocument(
                text,
                Path.GetFileNameWithoutExtension(fileName),
                pdf.NumberOfPages,
                metadata));
    }
}
