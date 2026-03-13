using Atlas.Domain.Workflow.Entities;

namespace Atlas.Application.Workflow.Repositories.V2;

public interface IWorkflowDraftRepository
{
    Task<long> AddAsync(WorkflowDraft draft, CancellationToken cancellationToken);

    Task UpdateAsync(WorkflowDraft draft, CancellationToken cancellationToken);

    Task<WorkflowDraft?> GetByWorkflowIdAsync(long workflowId, CancellationToken cancellationToken);
}
