using Atlas.Domain.Workflow.Entities;

namespace Atlas.Application.Workflow.Repositories.V2;

public interface IWorkflowVersionRepository
{
    Task<long> AddAsync(WorkflowVersion version, CancellationToken cancellationToken);

    Task<WorkflowVersion?> GetLatestAsync(long workflowId, CancellationToken cancellationToken);

    Task<WorkflowVersion?> GetByVersionAsync(long workflowId, string version, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowVersion>> ListByWorkflowIdAsync(long workflowId, CancellationToken cancellationToken);
}
