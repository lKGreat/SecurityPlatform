using Atlas.Domain.Workflow.Entities;

namespace Atlas.Application.Workflow.Repositories.V2;

public interface IWorkflowExecutionRepository
{
    Task<long> AddAsync(WorkflowExecution execution, CancellationToken cancellationToken);

    Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken);

    Task<WorkflowExecution?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
