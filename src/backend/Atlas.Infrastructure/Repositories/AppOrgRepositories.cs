using Atlas.Application.Platform.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.Platform.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class AppDepartmentRepository : IAppDepartmentRepository
{
    private readonly ISqlSugarClient _db;
    public AppDepartmentRepository(ISqlSugarClient db) => _db = db;

    public async Task<IReadOnlyList<AppDepartment>> QueryByAppIdAsync(TenantId tenantId, long appId, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppDepartment>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<AppDepartment> Items, int TotalCount)> QueryPageAsync(
        TenantId tenantId, long appId, int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<AppDepartment>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));
        var total = await query.CountAsync(cancellationToken);
        var list = await query.OrderBy(x => x.SortOrder).ToPageListAsync(pageIndex, pageSize, cancellationToken);
        return (list, total);
    }

    public async Task<AppDepartment?> FindByIdAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppDepartment>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .FirstAsync(cancellationToken);

    public async Task<IReadOnlyList<AppDepartment>> QueryByIdsAsync(TenantId tenantId, long appId, IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppDepartment>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AppDepartment entity, CancellationToken cancellationToken = default)
        => await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);

    public async Task UpdateAsync(AppDepartment entity, CancellationToken cancellationToken = default)
        => await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);

    public async Task DeleteAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken = default)
        => await _db.Deleteable<AppDepartment>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
}

public sealed class AppPositionRepository : IAppPositionRepository
{
    private readonly ISqlSugarClient _db;
    public AppPositionRepository(ISqlSugarClient db) => _db = db;

    public async Task<IReadOnlyList<AppPosition>> QueryByAppIdAsync(TenantId tenantId, long appId, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppPosition>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<AppPosition> Items, int TotalCount)> QueryPageAsync(
        TenantId tenantId, long appId, int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<AppPosition>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));
        var total = await query.CountAsync(cancellationToken);
        var list = await query.OrderBy(x => x.SortOrder).ToPageListAsync(pageIndex, pageSize, cancellationToken);
        return (list, total);
    }

    public async Task<AppPosition?> FindByIdAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppPosition>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .FirstAsync(cancellationToken);

    public async Task AddAsync(AppPosition entity, CancellationToken cancellationToken = default)
        => await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);

    public async Task UpdateAsync(AppPosition entity, CancellationToken cancellationToken = default)
        => await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);

    public async Task DeleteAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken = default)
        => await _db.Deleteable<AppPosition>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
}

public sealed class AppProjectRepository : IAppProjectRepository
{
    private readonly ISqlSugarClient _db;
    public AppProjectRepository(ISqlSugarClient db) => _db = db;

    public async Task<IReadOnlyList<AppProject>> QueryByAppIdAsync(TenantId tenantId, long appId, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppProject>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<AppProject> Items, int TotalCount)> QueryPageAsync(
        TenantId tenantId, long appId, int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken = default)
    {
        var query = _db.Queryable<AppProject>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId);
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));
        var total = await query.CountAsync(cancellationToken);
        var list = await query.OrderBy(x => x.Id, OrderByType.Desc).ToPageListAsync(pageIndex, pageSize, cancellationToken);
        return (list, total);
    }

    public async Task<AppProject?> FindByIdAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken = default)
        => await _db.Queryable<AppProject>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .FirstAsync(cancellationToken);

    public async Task AddAsync(AppProject entity, CancellationToken cancellationToken = default)
        => await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);

    public async Task UpdateAsync(AppProject entity, CancellationToken cancellationToken = default)
        => await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);

    public async Task DeleteAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken = default)
        => await _db.Deleteable<AppProject>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.AppId == appId && x.Id == id)
            .ExecuteCommandAsync(cancellationToken);
}
