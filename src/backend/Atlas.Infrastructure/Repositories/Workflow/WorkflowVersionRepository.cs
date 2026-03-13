using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Domain.Workflow.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories.Workflow;

public sealed class WorkflowVersionRepository : IWorkflowVersionRepository
{
    private readonly ISqlSugarClient _db;

    public WorkflowVersionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<long> AddAsync(WorkflowVersion version, CancellationToken cancellationToken)
    {
        await _db.Insertable(version).ExecuteCommandAsync(cancellationToken);
        return version.Id;
    }

    public async Task<WorkflowVersion?> GetLatestAsync(long workflowId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowVersion>()
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.PublishedAt, OrderByType.Desc)
            .FirstAsync(cancellationToken);
    }

    public async Task<WorkflowVersion?> GetByVersionAsync(long workflowId, string version, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowVersion>()
            .Where(x => x.WorkflowId == workflowId && x.Version == version)
            .FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVersion>> ListByWorkflowIdAsync(long workflowId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowVersion>()
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.PublishedAt, OrderByType.Desc)
            .ToListAsync(cancellationToken);
    }
}
