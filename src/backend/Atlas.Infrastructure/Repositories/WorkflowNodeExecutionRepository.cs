using Atlas.Application.AiPlatform.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class WorkflowNodeExecutionRepository : IWorkflowNodeExecutionRepository
{
    private readonly ISqlSugarClient _db;

    public WorkflowNodeExecutionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkflowNodeExecution>> ListByExecutionIdAsync(
        TenantId tenantId, long executionId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowNodeExecution>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.ExecutionId == executionId)
            .OrderBy(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowNodeExecution?> FindByNodeKeyAsync(
        TenantId tenantId, long executionId, string nodeKey, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowNodeExecution>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.ExecutionId == executionId && x.NodeKey == nodeKey)
            .FirstAsync(cancellationToken);
    }

    public Task AddAsync(WorkflowNodeExecution entity, CancellationToken cancellationToken)
    {
        return _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
    }

    public Task BatchAddAsync(IReadOnlyList<WorkflowNodeExecution> entities, CancellationToken cancellationToken)
    {
        if (entities.Count == 0) return Task.CompletedTask;
        return _db.Insertable(entities.ToList()).ExecuteCommandAsync(cancellationToken);
    }

    public Task UpdateAsync(WorkflowNodeExecution entity, CancellationToken cancellationToken)
    {
        return _db.Updateable(entity)
            .Where(x => x.Id == entity.Id && x.TenantIdValue == entity.TenantIdValue)
            .ExecuteCommandAsync(cancellationToken);
    }
}
