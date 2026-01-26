using Atlas.Core.Tenancy;

namespace Atlas.Core.Abstractions;

public abstract class TenantEntity : EntityBase, ITenantScoped
{
    protected TenantEntity(TenantId tenantId)
    {
        TenantId = tenantId;
    }

    public TenantId TenantId { get; }
}