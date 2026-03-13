using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Domain.Workflow.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories.Workflow;

public sealed class NodeExecutionRepository : INodeExecutionRepository
{
    private readonly ISqlSugarClient _db;

    public NodeExecutionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<long> AddAsync(NodeExecution nodeExecution, CancellationToken cancellationToken)
    {
        await _db.Insertable(nodeExecution).ExecuteCommandAsync(cancellationToken);
        return nodeExecution.Id;
    }

    public async Task UpdateAsync(NodeExecution nodeExecution, CancellationToken cancellationToken)
    {
        await _db.Updateable(nodeExecution).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NodeExecution>> ListByExecutionIdAsync(long executionId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<NodeExecution>()
            .Where(x => x.ExecutionId == executionId)
            .OrderBy(x => x.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<NodeExecution?> GetByExecutionAndNodeKeyAsync(long executionId, string nodeKey, CancellationToken cancellationToken)
    {
        return await _db.Queryable<NodeExecution>()
            .Where(x => x.ExecutionId == executionId && x.NodeKey == nodeKey)
            .OrderBy(x => x.StartedAt, OrderByType.Desc)
            .FirstAsync(cancellationToken);
    }

    public async Task AddBatchAsync(IEnumerable<NodeExecution> nodeExecutions, CancellationToken cancellationToken)
    {
        var list = nodeExecutions.ToList();
        if (list.Count == 0) return;
        await _db.Insertable(list).ExecuteCommandAsync(cancellationToken);
    }

    public async Task UpdateBatchAsync(IEnumerable<NodeExecution> nodeExecutions, CancellationToken cancellationToken)
    {
        var list = nodeExecutions.ToList();
        if (list.Count == 0) return;
        await _db.Updateable(list).ExecuteCommandAsync(cancellationToken);
    }
}
