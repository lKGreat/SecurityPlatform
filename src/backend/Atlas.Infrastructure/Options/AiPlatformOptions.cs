namespace Atlas.Infrastructure.Options;

public sealed class AiPlatformOptions
{
    public string DefaultProvider { get; init; } = "openai";

    public Dictionary<string, AiProviderOption> Providers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public EmbeddingOption Embedding { get; init; } = new();
}

public sealed class AiProviderOption
{
    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public string DefaultModel { get; init; } = string.Empty;

    public bool SupportsEmbedding { get; init; } = true;
}

public sealed class EmbeddingOption
{
    public string Provider { get; init; } = "openai";

    public string Model { get; init; } = "text-embedding-3-small";

    public int Dimensions { get; init; } = 1536;
}
