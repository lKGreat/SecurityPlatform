namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 待处理活动
/// </summary>
public class PendingActivity
{
    /// <summary>
    /// 活动令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 活动名称
    /// </summary>
    public string ActivityName { get; set; } = string.Empty;

    /// <summary>
    /// 活动参数
    /// </summary>
    public object? Parameters { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime TokenExpiry { get; set; }
}
