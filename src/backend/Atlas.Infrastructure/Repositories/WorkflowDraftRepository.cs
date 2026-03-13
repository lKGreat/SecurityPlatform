using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class WorkflowDraftRepository : RepositoryBase<WorkflowDraft>, IWorkflowDraftRepository
{
    public WorkflowDraftRepository(ISqlSugarClient db) : base(db) { }

    public async Task<WorkflowDraft?> FindByWorkflowIdAsync(TenantId tenantId, long workflowId, CancellationToken cancellationToken)
    {
        return await Db.Queryable<WorkflowDraft>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.WorkflowId == workflowId)
            .FirstAsync(cancellationToken);
    }
}
