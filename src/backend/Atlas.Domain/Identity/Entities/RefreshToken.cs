using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public sealed class RefreshToken : TenantEntity
{
#pragma warning disable CS8618
    public RefreshToken()
        : base(TenantId.Empty)
    {
        TokenHash = string.Empty;
        IssuedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow;
    }
#pragma warning restore CS8618

    public RefreshToken(
        TenantId tenantId,
        long userId,
        long sessionId,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt,
        long id)
        : base(tenantId)
    {
        Id = id;
        UserId = userId;
        SessionId = sessionId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    public long UserId { get; private set; }
    public long SessionId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public long? ReplacedById { get; private set; }

    public void Revoke(DateTimeOffset now, long? replacedById)
    {
        RevokedAt = now;
        ReplacedById = replacedById;
    }
}
