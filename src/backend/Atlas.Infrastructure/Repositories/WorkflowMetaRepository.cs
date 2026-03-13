using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class WorkflowMetaRepository : RepositoryBase<WorkflowMeta>, IWorkflowMetaRepository
{
    public WorkflowMetaRepository(ISqlSugarClient db) : base(db) { }

    public async Task<WorkflowMeta?> FindActiveByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken)
    {
        return await Db.Queryable<WorkflowMeta>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Id == id && !x.IsDeleted)
            .FirstAsync(cancellationToken);
    }

    public async Task<(List<WorkflowMeta> Items, long Total)> GetPagedAsync(
        TenantId tenantId, string? keyword, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        var query = Db.Queryable<WorkflowMeta>()
            .Where(x => x.TenantIdValue == tenantId.Value && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim();
            query = query.Where(x => x.Name.Contains(normalized) || (x.Description != null && x.Description.Contains(normalized)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.UpdatedAt, OrderByType.Desc)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);
        return (items, total);
    }
}
