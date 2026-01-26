using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Audit.Entities;

public sealed class AuditRecord : TenantEntity
{
    public AuditRecord(TenantId tenantId, string action)
        : base(tenantId)
    {
        Action = action;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public string Action { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}