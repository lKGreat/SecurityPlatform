using Atlas.Domain.Workflow.Entities;

namespace Atlas.Application.Workflow.Repositories.V2;

public interface IWorkflowMetaRepository
{
    Task<long> AddAsync(WorkflowMeta meta, CancellationToken cancellationToken);

    Task UpdateAsync(WorkflowMeta meta, CancellationToken cancellationToken);

    Task DeleteAsync(long id, CancellationToken cancellationToken);

    Task<WorkflowMeta?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<WorkflowMeta> Items, int TotalCount)> QueryPageAsync(int pageIndex, int pageSize, string? keyword, CancellationToken cancellationToken);
}
