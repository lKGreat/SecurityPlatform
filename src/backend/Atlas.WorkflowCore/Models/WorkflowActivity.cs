namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 工作流活动 - 代表待处理的外部活动
/// </summary>
public class WorkflowActivity
{
    /// <summary>
    /// 活动令牌（唯一标识）
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
    /// 工作流实例ID
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// 执行指针ID
    /// </summary>
    public string ExecutionPointerId { get; set; } = string.Empty;

    /// <summary>
    /// 步骤ID
    /// </summary>
    public int StepId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 锁定时间
    /// </summary>
    public DateTime? LockTime { get; set; }

    /// <summary>
    /// 锁定的工作者ID
    /// </summary>
    public string? WorkerId { get; set; }
}
