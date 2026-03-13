namespace Atlas.Application.AiPlatform.Models;

public sealed record ParsedDocument(
    string Text,
    string? Title,
    int PageCount,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record TextChunk(
    int Index,
    string Content,
    int StartOffset,
    int EndOffset);

public sealed record ChunkingOptions(
    int ChunkSize = 500,
    int Overlap = 50);
