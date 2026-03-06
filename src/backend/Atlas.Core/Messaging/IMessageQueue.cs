namespace Atlas.Core.Messaging;

/// <summary>
/// 持久化消息队列接口（基于 SQLite，无外部 MQ 依赖）
/// </summary>
public interface IMessageQueue
{
    /// <summary>入队</summary>
    Task EnqueueAsync(string queueName, string messageType, string payload, CancellationToken cancellationToken);

    /// <summary>批量出队（锁定消息，供消费者处理）</summary>
    Task<IReadOnlyList<QueueMessageItem>> DequeueAsync(string queueName, int count, CancellationToken cancellationToken);

    /// <summary>确认消费成功</summary>
    Task AcknowledgeAsync(long messageId, CancellationToken cancellationToken);

    /// <summary>拒绝消息（可选重入队）</summary>
    Task RejectAsync(long messageId, bool requeue, string? errorMessage, CancellationToken cancellationToken);

    /// <summary>获取队列统计信息</summary>
    Task<QueueStats> GetStatsAsync(string? queueName, CancellationToken cancellationToken);
}

public sealed record QueueMessageItem(
    long Id,
    string QueueName,
    string MessageType,
    string Payload,
    int RetryCount);

public sealed record QueueStats(
    string QueueName,
    int Pending,
    int Processing,
    int Completed,
    int Failed,
    int DeadLettered);
