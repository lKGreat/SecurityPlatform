using Atlas.Domain.Workflow.Entities;

namespace Atlas.Application.Workflow.Repositories.V2;

public interface INodeExecutionRepository
{
    Task<long> AddAsync(NodeExecution nodeExecution, CancellationToken cancellationToken);

    Task UpdateAsync(NodeExecution nodeExecution, CancellationToken cancellationToken);

    Task<IReadOnlyList<NodeExecution>> ListByExecutionIdAsync(long executionId, CancellationToken cancellationToken);

    Task<NodeExecution?> GetByExecutionAndNodeKeyAsync(long executionId, string nodeKey, CancellationToken cancellationToken);

    Task AddBatchAsync(IEnumerable<NodeExecution> nodeExecutions, CancellationToken cancellationToken);

    Task UpdateBatchAsync(IEnumerable<NodeExecution> nodeExecutions, CancellationToken cancellationToken);
}
