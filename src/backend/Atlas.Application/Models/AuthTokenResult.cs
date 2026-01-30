namespace Atlas.Application.Models;

public sealed record AuthTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    long SessionId);
