using System.Text;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;

namespace Atlas.Infrastructure.Services.AiPlatform.Parsers;

public sealed class TxtDocumentParser : IDocumentParser
{
    public bool CanParse(string? contentType, string extension)
    {
        return string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, "text/plain", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct);
        return new ParsedDocument(
            text,
            Path.GetFileNameWithoutExtension(fileName),
            1,
            new Dictionary<string, string>
            {
                ["fileName"] = fileName,
                ["parser"] = nameof(TxtDocumentParser)
            });
    }
}
