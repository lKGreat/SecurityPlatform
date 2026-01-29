namespace Atlas.WorkflowCore.Models;

public class EventSubscription
{
    public string Id { get; set; } = string.Empty;

    public string WorkflowId { get; set; } = string.Empty;

    public int StepId { get; set; }

    public string ExecutionPointerId { get; set; } = string.Empty;

    public string EventName { get; set; } = string.Empty;

    public string EventKey { get; set; } = string.Empty;

    public DateTime SubscribeAsOf { get; set; }

    public string? SubscriptionData { get; set; }

    public string? EventKeySlug { get; set; }

    /// <summary>
    /// 外部令牌（用于Activity）
    /// </summary>
    public string? ExternalToken { get; set; }

    /// <summary>
    /// 外部工作节点ID（用于Activity）
    /// </summary>
    public string? ExternalWorkerId { get; set; }

    /// <summary>
    /// 外部令牌过期时间（用于Activity）
    /// </summary>
    public DateTime? ExternalTokenExpiry { get; set; }
}
