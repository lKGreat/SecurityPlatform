using Atlas.Application.Identity.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class RoleDeptRepository : IRoleDeptRepository
{
    private readonly ISqlSugarClient _db;

    public RoleDeptRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleDept>> QueryByRoleIdsAsync(
        TenantId tenantId,
        IReadOnlyList<long> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return Array.Empty<RoleDept>();
        }

        var ids = roleIds.Distinct().ToArray();
        var list = await _db.Queryable<RoleDept>()
            .Where(x => x.TenantIdValue == tenantId.Value && SqlFunc.ContainsArray(ids, x.RoleId))
            .ToListAsync(cancellationToken);
        return list;
    }

    public Task DeleteByRoleIdAsync(TenantId tenantId, long roleId, CancellationToken cancellationToken)
    {
        return _db.Deleteable<RoleDept>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.RoleId == roleId)
            .ExecuteCommandAsync(cancellationToken);
    }

    public Task AddRangeAsync(IReadOnlyList<RoleDept> roleDepts, CancellationToken cancellationToken)
    {
        if (roleDepts.Count == 0)
        {
            return Task.CompletedTask;
        }

        return _db.Insertable(roleDepts.ToList()).ExecuteCommandAsync(cancellationToken);
    }
}
