using Atlas.Core.Abstractions;
using Atlas.Core.Messaging;
using Atlas.Domain.Messaging;
using SqlSugar;

namespace Atlas.Infrastructure.Messaging;

/// <summary>
/// 基于 SQLite（SqlSugar）的持久化消息队列实现
/// </summary>
public sealed class SqliteMessageQueue : IMessageQueue
{
    private readonly ISqlSugarClient _db;
    private readonly IIdGeneratorAccessor _idGen;

    public SqliteMessageQueue(ISqlSugarClient db, IIdGeneratorAccessor idGen)
    {
        _db = db;
        _idGen = idGen;
    }

    public async Task EnqueueAsync(
        string queueName, string messageType, string payload, CancellationToken cancellationToken)
    {
        var message = new QueueMessage
        {
            Id = _idGen.Generator.NextId(),
            QueueName = queueName,
            MessageType = messageType,
            Payload = payload,
            Status = QueueMessageStatus.Pending,
            EnqueuedAt = DateTimeOffset.UtcNow
        };
        await _db.Insertable(message).ExecuteCommandAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QueueMessageItem>> DequeueAsync(
        string queueName, int count, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // 批量查询 Pending 消息（不在循环内逐条操作）
        var messages = await _db.Queryable<QueueMessage>()
            .Where(m => m.QueueName == queueName
                && (m.Status == QueueMessageStatus.Pending
                    || (m.Status == QueueMessageStatus.Failed && m.NextRetryAt <= now)))
            .OrderBy(m => m.EnqueuedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return [];
        }

        var ids = messages.Select(m => m.Id).ToList();

        // 批量更新状态为 Processing
        await _db.Updateable<QueueMessage>()
            .SetColumns(m => new QueueMessage
            {
                Status = QueueMessageStatus.Processing,
                ProcessingStartedAt = now
            })
            .Where(m => ids.Contains(m.Id))
            .ExecuteCommandAsync(cancellationToken);

        return messages.Select(m => new QueueMessageItem(m.Id, m.QueueName, m.MessageType, m.Payload, m.RetryCount)).ToList();
    }

    public async Task AcknowledgeAsync(long messageId, CancellationToken cancellationToken)
    {
        await _db.Updateable<QueueMessage>()
            .SetColumns(m => new QueueMessage
            {
                Status = QueueMessageStatus.Completed,
                CompletedAt = DateTimeOffset.UtcNow
            })
            .Where(m => m.Id == messageId)
            .ExecuteCommandAsync(cancellationToken);
    }

    public async Task RejectAsync(long messageId, bool requeue, string? errorMessage, CancellationToken cancellationToken)
    {
        var message = await _db.Queryable<QueueMessage>()
            .Where(m => m.Id == messageId)
            .FirstAsync(cancellationToken);

        if (message is null) return;

        if (!requeue || message.RetryCount >= message.MaxRetries)
        {
            await _db.Updateable<QueueMessage>()
                .SetColumns(m => new QueueMessage
                {
                    Status = QueueMessageStatus.DeadLettered,
                    ErrorMessage = errorMessage,
                    CompletedAt = DateTimeOffset.UtcNow
                })
                .Where(m => m.Id == messageId)
                .ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            var retryCount = message.RetryCount + 1;
            var backoffSeconds = (int)Math.Pow(2, retryCount);
            var nextRetry = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);

            await _db.Updateable<QueueMessage>()
                .SetColumns(m => new QueueMessage
                {
                    Status = QueueMessageStatus.Failed,
                    RetryCount = retryCount,
                    ErrorMessage = errorMessage,
                    NextRetryAt = nextRetry
                })
                .Where(m => m.Id == messageId)
                .ExecuteCommandAsync(cancellationToken);
        }
    }

    public async Task<QueueStats> GetStatsAsync(string? queueName, CancellationToken cancellationToken)
    {
        var query = _db.Queryable<QueueMessage>();
        if (!string.IsNullOrEmpty(queueName))
        {
            query = query.Where(m => m.QueueName == queueName);
        }

        var allMessages = await query.Select(m => new { m.Status, m.QueueName }).ToListAsync(cancellationToken);
        var name = queueName ?? "all";

        return new QueueStats(
            name,
            allMessages.Count(m => m.Status == QueueMessageStatus.Pending),
            allMessages.Count(m => m.Status == QueueMessageStatus.Processing),
            allMessages.Count(m => m.Status == QueueMessageStatus.Completed),
            allMessages.Count(m => m.Status == QueueMessageStatus.Failed),
            allMessages.Count(m => m.Status == QueueMessageStatus.DeadLettered));
    }
}
