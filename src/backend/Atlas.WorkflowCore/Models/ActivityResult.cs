namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 活动结果
/// </summary>
public class ActivityResult
{
    /// <summary>
    /// 活动状态
    /// </summary>
    public ActivityResultStatus Status { get; set; }

    /// <summary>
    /// 结果数据
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// 订阅ID
    /// </summary>
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// 活动令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// 活动结果状态
/// </summary>
public enum ActivityResultStatus
{
    /// <summary>
    /// 成功
    /// </summary>
    Success = 0,

    /// <summary>
    /// 失败
    /// </summary>
    Failure = 1,

    /// <summary>
    /// 超时
    /// </summary>
    Timeout = 2
}
