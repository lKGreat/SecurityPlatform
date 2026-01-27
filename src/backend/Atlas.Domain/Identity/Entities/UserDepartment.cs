using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;

namespace Atlas.Domain.Identity.Entities;

public class UserDepartment : TenantEntity
{
    public UserDepartment()
        : base(TenantId.Empty)
    {
        IsPrimary = false;
    }

    public UserDepartment(TenantId tenantId, long userId, long departmentId, long id, bool isPrimary)
        : base(tenantId)
    {
        Id = id;
        UserId = userId;
        DepartmentId = departmentId;
        IsPrimary = isPrimary;
    }

    public long UserId { get; private set; }
    public long DepartmentId { get; private set; }
    public bool IsPrimary { get; private set; }
}
