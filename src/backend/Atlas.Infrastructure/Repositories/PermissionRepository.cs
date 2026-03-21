using Atlas.Application.Identity.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class PermissionRepository : RepositoryBase<Permission>, IPermissionRepository
{
    public PermissionRepository(ISqlSugarClient db) : base(db) { }

    public async Task<Permission?> FindByIdPlatformOnlyAsync(TenantId tenantId, long id, CancellationToken cancellationToken)
    {
        return await Db.Queryable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Id == id && x.AppId == null)
            .FirstAsync(cancellationToken);
    }

    public async Task<Permission?> FindByCodeAsync(TenantId tenantId, string code, long? appId, CancellationToken cancellationToken)
    {
        var query = Db.Queryable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Code == code);
        query = appId.HasValue
            ? query.Where(x => x.AppId == appId.Value)
            : query.Where(x => x.AppId == null);
        return await query.FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Permission>> QueryByIdsPlatformOnlyAsync(TenantId tenantId, IReadOnlyList<long> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];
        return await Db.Queryable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value && ids.Contains(x.Id) && x.AppId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Permission> Items, int TotalCount)> QueryPageAsync(
        TenantId tenantId,
        int pageIndex,
        int pageSize,
        string? keyword,
        string? type,
        CancellationToken cancellationToken,
        long? appId = null,
        bool platformOnly = false)
    {
        var query = Db.Queryable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value);
        if (platformOnly)
        {
            query = query.Where(x => x.AppId == null);
        }
        else if (appId.HasValue)
        {
            query = query.Where(x => x.AppId == appId.Value);
        }
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalized = type.Trim();
            query = query.Where(x => x.Type == normalized);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var list = await query
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);

        return (list, totalCount);
    }

    public async Task<IReadOnlyList<Permission>> QueryAllAsync(TenantId tenantId, CancellationToken cancellationToken, bool platformOnly = false)
    {
        var query = Db.Queryable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value);
        if (platformOnly)
        {
            query = query.Where(x => x.AppId == null);
        }
        return await query
            .OrderBy(x => x.Code, OrderByType.Asc)
            .ToListAsync(cancellationToken);
    }

    public override async Task DeleteAsync(TenantId tenantId, long id, CancellationToken cancellationToken)
    {
        await Db.Deleteable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task<Permission?> FindByIdAndAppIdAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken)
    {
        return await Db.Queryable<Permission>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .FirstAsync(cancellationToken);
    }
}
