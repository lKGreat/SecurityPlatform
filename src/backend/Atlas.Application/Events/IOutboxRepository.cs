using Atlas.Domain.Events;

namespace Atlas.Application.Events;

/// <summary>
/// Outbox 消息仓储接口。
/// 负责持久化集成事件并支持后台处理器查询/更新消息状态。
/// </summary>
public interface IOutboxRepository
{
    /// <summary>写入一条 Outbox 消息</summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);

    /// <summary>批量获取待处理的消息（Status=Pending 且 NextRetryAt &lt;= now，或 Status=Failed 且 NextRetryAt &lt;= now）</summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);

    /// <summary>获取死信消息（分页）</summary>
    Task<(IReadOnlyList<OutboxMessage> Items, int Total)> GetDeadLetteredAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken);

    /// <summary>更新消息状态</summary>
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken);

    /// <summary>批量更新消息状态（锁定为 Processing，避免并发重复处理）</summary>
    Task<IReadOnlyList<OutboxMessage>> LockPendingAsync(int batchSize, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>按 ID 获取单条消息</summary>
    Task<OutboxMessage?> FindByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>获取各状态消息数量</summary>
    Task<Dictionary<OutboxMessageStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken);

    /// <summary>将所有死信消息重置为 Pending（批量重试）</summary>
    Task ResetDeadLettersAsync(CancellationToken cancellationToken);
}
