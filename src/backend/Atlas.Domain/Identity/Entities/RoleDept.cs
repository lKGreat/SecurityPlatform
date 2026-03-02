using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public class RoleDept : TenantEntity
{
    public RoleDept()
        : base(TenantId.Empty)
    {
    }

    public RoleDept(TenantId tenantId, long roleId, long deptId, long id)
        : base(tenantId)
    {
        Id = id;
        RoleId = roleId;
        DeptId = deptId;
    }

    public long RoleId { get; private set; }
    public long DeptId { get; private set; }
}
