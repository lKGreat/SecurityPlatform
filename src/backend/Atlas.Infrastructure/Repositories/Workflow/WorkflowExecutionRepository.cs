using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Domain.Workflow.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories.Workflow;

public sealed class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly ISqlSugarClient _db;

    public WorkflowExecutionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<long> AddAsync(WorkflowExecution execution, CancellationToken cancellationToken)
    {
        await _db.Insertable(execution).ExecuteCommandAsync(cancellationToken);
        return execution.Id;
    }

    public async Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken)
    {
        await _db.Updateable(execution).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<WorkflowExecution?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowExecution>()
            .Where(x => x.Id == id)
            .FirstAsync(cancellationToken);
    }
}
