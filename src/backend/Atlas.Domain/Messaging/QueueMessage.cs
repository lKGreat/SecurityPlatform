namespace Atlas.Domain.Messaging;

/// <summary>
/// 持久化消息队列中的消息记录
/// </summary>
public sealed class QueueMessage
{
    public long Id { get; set; }
    public string QueueName { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public QueueMessageStatus Status { get; set; } = QueueMessageStatus.Pending;
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset EnqueuedAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}

public enum QueueMessageStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    DeadLettered = 4
}
