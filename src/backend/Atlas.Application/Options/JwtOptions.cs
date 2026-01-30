namespace Atlas.Application.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int ExpiresMinutes { get; init; } = 15;
    public int RefreshExpiresMinutes { get; init; } = 720;
    public int SessionExpiresMinutes { get; init; } = 720;
}
