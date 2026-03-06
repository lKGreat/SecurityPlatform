using Atlas.Application.Events;
using Atlas.Domain.Events;

namespace Atlas.Infrastructure.Events;

/// <summary>
/// Outbox 管理服务实现：死信查询、重试、统计。
/// </summary>
public sealed class OutboxManagementService : IOutboxManagementService
{
    private readonly IOutboxRepository _repository;

    public OutboxManagementService(IOutboxRepository repository)
    {
        _repository = repository;
    }

    public Task<(IReadOnlyList<OutboxMessage> Items, int Total)> GetDeadLetteredAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        return _repository.GetDeadLetteredAsync(pageIndex, pageSize, cancellationToken);
    }

    public async Task RetryDeadLetterAsync(long messageId, CancellationToken cancellationToken)
    {
        var message = await _repository.FindByIdAsync(messageId, cancellationToken);
        if (message is null || message.Status != OutboxMessageStatus.DeadLettered)
        {
            return;
        }

        message.Status = OutboxMessageStatus.Pending;
        message.RetryCount = 0;
        message.NextRetryAt = null;
        message.ErrorMessage = null;

        await _repository.UpdateAsync(message, cancellationToken);
    }

    public Task RetryAllDeadLettersAsync(CancellationToken cancellationToken)
    {
        return _repository.ResetDeadLettersAsync(cancellationToken);
    }

    public async Task<OutboxStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        var counts = await _repository.GetStatusCountsAsync(cancellationToken);

        return new OutboxStats(
            PendingCount: counts.GetValueOrDefault(OutboxMessageStatus.Pending),
            ProcessingCount: counts.GetValueOrDefault(OutboxMessageStatus.Processing),
            CompletedCount: counts.GetValueOrDefault(OutboxMessageStatus.Completed),
            FailedCount: counts.GetValueOrDefault(OutboxMessageStatus.Failed),
            DeadLetteredCount: counts.GetValueOrDefault(OutboxMessageStatus.DeadLettered));
    }
}
