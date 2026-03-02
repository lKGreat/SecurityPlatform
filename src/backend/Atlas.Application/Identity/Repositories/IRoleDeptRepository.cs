using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Application.Identity.Repositories;

public interface IRoleDeptRepository
{
    Task<IReadOnlyList<RoleDept>> QueryByRoleIdsAsync(
        TenantId tenantId,
        IReadOnlyList<long> roleIds,
        CancellationToken cancellationToken);

    Task DeleteByRoleIdAsync(TenantId tenantId, long roleId, CancellationToken cancellationToken);

    Task AddRangeAsync(IReadOnlyList<RoleDept> roleDepts, CancellationToken cancellationToken);
}
