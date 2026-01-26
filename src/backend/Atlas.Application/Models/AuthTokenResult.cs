namespace Atlas.Application.Models;

public sealed record AuthTokenResult(string AccessToken, DateTimeOffset ExpiresAt);