using Atlas.Application.AiPlatform.Abstractions;
using Atlas.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Atlas.Infrastructure.Services.AiPlatform;

public sealed class LlmProviderFactory : ILlmProviderFactory
{
    private readonly IReadOnlyDictionary<string, ILlmProvider> _llmProviders;
    private readonly IReadOnlyDictionary<string, IEmbeddingProvider> _embeddingProviders;
    private readonly AiPlatformOptions _options;

    public LlmProviderFactory(
        IEnumerable<ILlmProvider> llmProviders,
        IEnumerable<IEmbeddingProvider> embeddingProviders,
        IOptions<AiPlatformOptions> options)
    {
        _llmProviders = llmProviders.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
        _embeddingProviders = embeddingProviders.ToDictionary(p => p.ProviderName, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
    }

    public ILlmProvider GetLlmProvider(string? providerName = null)
    {
        var name = ResolveProviderName(providerName, _options.DefaultProvider);
        if (_llmProviders.TryGetValue(name, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException($"LLM provider '{name}' is not registered.");
    }

    public IEmbeddingProvider GetEmbeddingProvider(string? providerName = null)
    {
        var fallback = _options.Embedding.Provider;
        var name = ResolveProviderName(providerName, fallback);
        if (_embeddingProviders.TryGetValue(name, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException($"Embedding provider '{name}' is not registered.");
    }

    private static string ResolveProviderName(string? explicitProvider, string fallbackProvider)
    {
        if (!string.IsNullOrWhiteSpace(explicitProvider))
        {
            return explicitProvider.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fallbackProvider))
        {
            return fallbackProvider.Trim();
        }

        throw new InvalidOperationException("No AI provider name specified and no default provider configured.");
    }
}
