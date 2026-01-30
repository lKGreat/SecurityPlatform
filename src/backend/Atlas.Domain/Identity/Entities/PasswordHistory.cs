using System;
using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public sealed class PasswordHistory : TenantEntity
{
    public PasswordHistory()
        : base(TenantId.Empty)
    {
        UserId = 0;
        PasswordHash = string.Empty;
        CreatedAt = DateTimeOffset.MinValue;
    }

    public PasswordHistory(TenantId tenantId, long userId, string passwordHash, long id, DateTimeOffset createdAt)
        : base(tenantId)
    {
        Id = id;
        UserId = userId;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    public long UserId { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
