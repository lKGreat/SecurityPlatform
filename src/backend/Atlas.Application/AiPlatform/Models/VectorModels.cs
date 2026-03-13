namespace Atlas.Application.AiPlatform.Models;

public sealed record VectorRecord(
    string Id,
    float[] Vector,
    string Content,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record VectorSearchResult(
    string Id,
    string Content,
    float Score,
    IReadOnlyDictionary<string, string>? Metadata = null);
