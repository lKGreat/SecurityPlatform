using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Assets.Entities;

public sealed class Asset : TenantEntity
{
    public Asset(TenantId tenantId, string name)
        : base(tenantId)
    {
        Name = name;
    }

    public string Name { get; private set; }
}