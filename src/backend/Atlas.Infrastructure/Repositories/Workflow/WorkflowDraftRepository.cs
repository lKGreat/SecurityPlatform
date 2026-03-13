using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Domain.Workflow.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories.Workflow;

public sealed class WorkflowDraftRepository : IWorkflowDraftRepository
{
    private readonly ISqlSugarClient _db;

    public WorkflowDraftRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<long> AddAsync(WorkflowDraft draft, CancellationToken cancellationToken)
    {
        await _db.Insertable(draft).ExecuteCommandAsync(cancellationToken);
        return draft.Id;
    }

    public async Task UpdateAsync(WorkflowDraft draft, CancellationToken cancellationToken)
    {
        await _db.Updateable(draft).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<WorkflowDraft?> GetByWorkflowIdAsync(long workflowId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowDraft>()
            .Where(x => x.WorkflowId == workflowId)
            .FirstAsync(cancellationToken);
    }
}
