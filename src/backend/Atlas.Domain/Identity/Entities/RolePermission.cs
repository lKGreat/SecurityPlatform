using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public class RolePermission : TenantEntity
{
    public RolePermission()
        : base(TenantId.Empty)
    {
    }

    public RolePermission(TenantId tenantId, long roleId, long permissionId, long id)
        : base(tenantId)
    {
        Id = id;
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }
}
