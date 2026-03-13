using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class WorkflowExecutionRepository : RepositoryBase<WorkflowExecution>, IWorkflowExecutionRepository
{
    public WorkflowExecutionRepository(ISqlSugarClient db) : base(db) { }

    public new async Task<WorkflowExecution?> FindByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken)
    {
        return await Db.Queryable<WorkflowExecution>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Id == id)
            .FirstAsync(cancellationToken);
    }
}
