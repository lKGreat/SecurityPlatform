namespace Atlas.Domain.Events;

/// <summary>
/// Outbox 消息实体，用于"至少一次"投递集成事件。
/// 写入时与业务操作同事务，由后台服务轮询投递。
/// </summary>
public sealed class OutboxMessage
{
    public long Id { get; set; }

    /// <summary>关联的领域/集成事件 ID</summary>
    public Guid EventId { get; set; }

    /// <summary>事件类型（如 "approval.instance.completed"）</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>事件 Payload（JSON）</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>租户 ID（Guid.Empty 表示系统级）</summary>
    public Guid TenantId { get; set; }

    /// <summary>消息状态</summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>成功处理时间</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>下次重试时间（指数退避）</summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    /// <summary>已重试次数</summary>
    public int RetryCount { get; set; }

    /// <summary>最大重试次数（默认 5）</summary>
    public int MaxRetries { get; set; } = 5;

    /// <summary>最后一次失败的错误信息</summary>
    public string? ErrorMessage { get; set; }
}

public enum OutboxMessageStatus
{
    /// <summary>等待投递</summary>
    Pending = 0,

    /// <summary>正在处理中</summary>
    Processing = 1,

    /// <summary>投递成功</summary>
    Completed = 2,

    /// <summary>投递失败（等待重试）</summary>
    Failed = 3,

    /// <summary>超出重试阈值，进入死信</summary>
    DeadLettered = 4
}
