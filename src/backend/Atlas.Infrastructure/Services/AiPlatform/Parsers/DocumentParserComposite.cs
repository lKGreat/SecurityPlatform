using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;

namespace Atlas.Infrastructure.Services.AiPlatform.Parsers;

public sealed class DocumentParserComposite : IDocumentParser
{
    private readonly IReadOnlyList<IDocumentParser> _parsers;

    public DocumentParserComposite(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers
            .Where(p => p is not DocumentParserComposite)
            .ToArray();
    }

    public bool CanParse(string? contentType, string extension)
    {
        return _parsers.Any(p => p.CanParse(contentType, extension));
    }

    public Task<ParsedDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        var parser = _parsers.FirstOrDefault(p => p.CanParse(null, extension));
        if (parser is null)
        {
            throw new NotSupportedException($"No document parser available for extension '{extension}'.");
        }

        return parser.ParseAsync(fileStream, fileName, ct);
    }
}
