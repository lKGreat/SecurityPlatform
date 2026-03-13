using Atlas.Application.Workflow.Repositories.V2;
using Atlas.Domain.Workflow.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories.Workflow;

public sealed class WorkflowMetaRepository : IWorkflowMetaRepository
{
    private readonly ISqlSugarClient _db;

    public WorkflowMetaRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<long> AddAsync(WorkflowMeta meta, CancellationToken cancellationToken)
    {
        await _db.Insertable(meta).ExecuteCommandAsync(cancellationToken);
        return meta.Id;
    }

    public async Task UpdateAsync(WorkflowMeta meta, CancellationToken cancellationToken)
    {
        await _db.Updateable(meta).ExecuteCommandAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await _db.Deleteable<WorkflowMeta>().Where(x => x.Id == id).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<WorkflowMeta?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _db.Queryable<WorkflowMeta>()
            .Where(x => x.Id == id)
            .FirstAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<WorkflowMeta> Items, int TotalCount)> QueryPageAsync(
        int pageIndex,
        int pageSize,
        string? keyword,
        CancellationToken cancellationToken)
    {
        var query = _db.Queryable<WorkflowMeta>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.Name.Contains(keyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var list = await query
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);

        return (list, totalCount);
    }
}
