using Atlas.Application.Events;
using Atlas.Domain.Events;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

/// <summary>
/// Outbox 消息仓储 - SqlSugar/SQLite 实现。
/// </summary>
public sealed class OutboxRepository : IOutboxRepository
{
    private readonly ISqlSugarClient _db;

    public OutboxRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _db.Insertable(message).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.Queryable<OutboxMessage>()
            .Where(m => (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.Failed)
                && (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<OutboxMessage> Items, int Total)> GetDeadLetteredAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        var total = await _db.Queryable<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.DeadLettered)
            .CountAsync(cancellationToken);

        var items = await _db.Queryable<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.DeadLettered)
            .OrderByDescending(m => m.CreatedAt)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);

        return (items, total);
    }

    public async Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        await _db.Updateable(message)
            .Where(m => m.Id == message.Id)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> LockPendingAsync(
        int batchSize, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // 先取候选 ID（SQLite 不支持 UPDATE...LIMIT，需两步实现原子锁）
        var candidateIds = await _db.Queryable<OutboxMessage>()
            .Where(m => (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.Failed)
                && (m.NextRetryAt == null || m.NextRetryAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            return [];
        }

        // 原子更新：WHERE 条件包含原始状态，确保只有仍处于待处理状态的行被锁定。
        // 并发时多个实例竞争同一批 ID，但 SQLite 的行锁保证每行只被一个更新成功写入。
        var updatedCount = await _db.Updateable<OutboxMessage>()
            .SetColumns(m => new OutboxMessage { Status = OutboxMessageStatus.Processing })
            .Where(m => candidateIds.Contains(m.Id)
                && (m.Status == OutboxMessageStatus.Pending || m.Status == OutboxMessageStatus.Failed))
            .ExecuteCommandAsync(cancellationToken);

        if (updatedCount == 0)
        {
            return [];
        }

        // 只返回本次实际被锁定（更新成功）的消息，过滤掉被其他竞争者抢先锁定的行。
        var locked = await _db.Queryable<OutboxMessage>()
            .Where(m => candidateIds.Contains(m.Id) && m.Status == OutboxMessageStatus.Processing)
            .ToListAsync(cancellationToken);

        return locked;
    }

    public async Task<OutboxMessage?> FindByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _db.Queryable<OutboxMessage>()
            .Where(m => m.Id == id)
            .FirstAsync(cancellationToken);
    }

    public async Task<Dictionary<OutboxMessageStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken)
    {
        var counts = await _db.Queryable<OutboxMessage>()
            .GroupBy(m => m.Status)
            .Select(m => new { m.Status, Count = SqlFunc.AggregateCount(m.Id) })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    public async Task ResetDeadLettersAsync(CancellationToken cancellationToken)
    {
        await _db.Updateable<OutboxMessage>()
            .SetColumns(m => new OutboxMessage
            {
                Status = OutboxMessageStatus.Pending,
                RetryCount = 0,
                NextRetryAt = null,
                ErrorMessage = null
            })
            .Where(m => m.Status == OutboxMessageStatus.DeadLettered)
            .ExecuteCommandAsync(cancellationToken);
    }
}
