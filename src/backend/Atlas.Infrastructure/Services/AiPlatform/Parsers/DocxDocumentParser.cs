using System.Text;
using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Application.AiPlatform.Models;
using DocumentFormat.OpenXml.Packaging;

namespace Atlas.Infrastructure.Services.AiPlatform.Parsers;

public sealed class DocxDocumentParser : IDocumentParser
{
    public bool CanParse(string? contentType, string extension)
    {
        return string.Equals(extension, ".docx", StringComparison.OrdinalIgnoreCase)
               || string.Equals(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase);
    }

    public Task<ParsedDocument> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        fileStream.Position = 0;
        using var memory = new MemoryStream();
        fileStream.CopyTo(memory);
        memory.Position = 0;

        using var wordDoc = WordprocessingDocument.Open(memory, false);
        var body = wordDoc.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return Task.FromResult(new ParsedDocument(string.Empty, Path.GetFileNameWithoutExtension(fileName), 0));
        }

        var builder = new StringBuilder();
        foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            ct.ThrowIfCancellationRequested();
            var text = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(text))
            {
                builder.AppendLine(text.Trim());
            }
        }

        var metadata = new Dictionary<string, string>
        {
            ["fileName"] = fileName,
            ["parser"] = nameof(DocxDocumentParser)
        };

        return Task.FromResult(
            new ParsedDocument(
                builder.ToString().Trim(),
                Path.GetFileNameWithoutExtension(fileName),
                1,
                metadata));
    }
}
