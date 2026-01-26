using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Alert.Entities;

public sealed class AlertRecord : TenantEntity
{
    public AlertRecord(TenantId tenantId, string title)
        : base(tenantId)
    {
        Title = title;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Title { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}