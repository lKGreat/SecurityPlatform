using System.Text;
using System.Text.Json;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;

namespace Atlas.Infrastructure.Services.AiPlatform.Parsers;

public sealed class JsonDocumentParser : IDocumentParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public bool CanParse(string? contentType, string extension)
    {
        return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, "application/json", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ParsedDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct);
        var pretty = raw;

        try
        {
            var node = JsonSerializer.Deserialize<JsonElement>(raw);
            pretty = JsonSerializer.Serialize(node, JsonOptions);
        }
        catch (JsonException)
        {
            // Keep original text if payload is not strict JSON.
        }

        return new ParsedDocument(
            pretty,
            Path.GetFileNameWithoutExtension(fileName),
            1,
            new Dictionary<string, string>
            {
                ["fileName"] = fileName,
                ["parser"] = nameof(JsonDocumentParser)
            });
    }
}
