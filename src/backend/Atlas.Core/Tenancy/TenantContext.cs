namespace Atlas.Core.Tenancy;

public sealed class TenantContext : ITenantProvider
{
    public TenantContext(TenantId tenantId)
    {
        TenantId = tenantId;
    }

    public TenantId TenantId { get; }

    public TenantId GetTenantId() => TenantId;
}