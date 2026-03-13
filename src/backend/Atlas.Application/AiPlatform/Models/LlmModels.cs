namespace Atlas.Application.AiPlatform.Models;

public sealed record ChatMessage(string Role, string Content);

public sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    float? Temperature = null,
    int? MaxTokens = null,
    string? Provider = null);

public sealed record ChatCompletionResult(
    string Content,
    string? Model = null,
    string? Provider = null,
    string? FinishReason = null,
    int? PromptTokens = null,
    int? CompletionTokens = null,
    int? TotalTokens = null);

public sealed record ChatCompletionChunk(
    string ContentDelta,
    bool IsCompleted = false,
    string? FinishReason = null);

public sealed record EmbeddingRequest(
    string Model,
    IReadOnlyList<string> Inputs,
    string? Provider = null,
    int? Dimensions = null);

public sealed record EmbeddingResult(
    IReadOnlyList<float[]> Vectors,
    string? Model = null,
    string? Provider = null,
    int? PromptTokens = null,
    int? TotalTokens = null);
