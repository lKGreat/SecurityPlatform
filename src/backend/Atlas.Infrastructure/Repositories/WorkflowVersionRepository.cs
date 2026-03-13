using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class WorkflowVersionRepository : RepositoryBase<WorkflowVersion>, IWorkflowVersionRepository
{
    public WorkflowVersionRepository(ISqlSugarClient db) : base(db) { }

    public async Task<IReadOnlyList<WorkflowVersion>> ListByWorkflowIdAsync(
        TenantId tenantId, long workflowId, CancellationToken cancellationToken)
    {
        return await Db.Queryable<WorkflowVersion>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.WorkflowId == workflowId)
            .OrderBy(x => x.VersionNumber, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowVersion?> GetLatestAsync(TenantId tenantId, long workflowId, CancellationToken cancellationToken)
    {
        return await Db.Queryable<WorkflowVersion>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.WorkflowId == workflowId)
            .OrderBy(x => x.VersionNumber, OrderByType.Desc)
            .FirstAsync(cancellationToken);
    }
}
