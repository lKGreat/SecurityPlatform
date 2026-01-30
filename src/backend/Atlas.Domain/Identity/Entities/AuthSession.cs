using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public sealed class AuthSession : TenantEntity
{
#pragma warning disable CS8618
    public AuthSession()
        : base(TenantId.Empty)
    {
        ClientType = string.Empty;
        ClientPlatform = string.Empty;
        ClientChannel = string.Empty;
        ClientAgent = string.Empty;
        IpAddress = string.Empty;
        UserAgent = string.Empty;
        CreatedAt = DateTimeOffset.UtcNow;
        LastSeenAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow;
    }
#pragma warning restore CS8618

    public AuthSession(
        TenantId tenantId,
        long userId,
        string clientType,
        string clientPlatform,
        string clientChannel,
        string clientAgent,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        long id)
        : base(tenantId)
    {
        Id = id;
        UserId = userId;
        ClientType = clientType;
        ClientPlatform = clientPlatform;
        ClientChannel = clientChannel;
        ClientAgent = clientAgent;
        IpAddress = ipAddress ?? string.Empty;
        UserAgent = userAgent ?? string.Empty;
        CreatedAt = now;
        LastSeenAt = now;
        ExpiresAt = expiresAt;
    }

    public long UserId { get; private set; }
    public string ClientType { get; private set; }
    public string ClientPlatform { get; private set; }
    public string ClientChannel { get; private set; }
    public string ClientAgent { get; private set; }
    public string IpAddress { get; private set; }
    public string UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public void MarkSeen(DateTimeOffset now)
    {
        LastSeenAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt = now;
    }
}
