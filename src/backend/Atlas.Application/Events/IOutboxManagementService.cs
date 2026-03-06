using Atlas.Domain.Events;

namespace Atlas.Application.Events;

/// <summary>
/// Outbox 管理服务：提供死信查询、手动重试等管理能力。
/// </summary>
public interface IOutboxManagementService
{
    /// <summary>获取死信消息（分页）</summary>
    Task<(IReadOnlyList<OutboxMessage> Items, int Total)> GetDeadLetteredAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken);

    /// <summary>手动重试单条死信消息</summary>
    Task RetryDeadLetterAsync(long messageId, CancellationToken cancellationToken);

    /// <summary>批量重试所有死信消息（重置为 Pending）</summary>
    Task RetryAllDeadLettersAsync(CancellationToken cancellationToken);

    /// <summary>获取各状态消息统计</summary>
    Task<OutboxStats> GetStatsAsync(CancellationToken cancellationToken);
}

public sealed record OutboxStats(
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int FailedCount,
    int DeadLetteredCount);
