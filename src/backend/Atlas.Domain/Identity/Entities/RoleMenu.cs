using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public class RoleMenu : TenantEntity
{
    public RoleMenu()
        : base(TenantId.Empty)
    {
    }

    public RoleMenu(TenantId tenantId, long roleId, long menuId, long id)
        : base(tenantId)
    {
        Id = id;
        RoleId = roleId;
        MenuId = menuId;
    }

    public long RoleId { get; private set; }
    public long MenuId { get; private set; }
}
