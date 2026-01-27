using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public class UserRole : TenantEntity
{
    public UserRole()
        : base(TenantId.Empty)
    {
    }

    public UserRole(TenantId tenantId, long userId, long roleId, long id)
        : base(tenantId)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
    }

    public long UserId { get; private set; }
    public long RoleId { get; private set; }
}
