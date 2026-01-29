namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 工作流引擎选项配置
/// </summary>
public class WorkflowOptions
{
    /// <summary>
    /// 轮询间隔（默认10秒）
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 空闲时间（默认30秒）
    /// </summary>
    public TimeSpan IdleTime { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 最大并发工作流数（默认0表示无限制）
    /// </summary>
    public int MaxConcurrentWorkflows { get; set; } = 0;

    /// <summary>
    /// 是否启用搜索索引（默认false）
    /// </summary>
    public bool EnableIndex { get; set; } = false;

    /// <summary>
    /// 是否启用分布式锁（默认true）
    /// </summary>
    public bool EnableDistributedLock { get; set; } = true;

    /// <summary>
    /// 默认错误处理策略
    /// </summary>
    public WorkflowErrorHandling DefaultErrorBehavior { get; set; } = WorkflowErrorHandling.Retry;

    /// <summary>
    /// 默认重试间隔
    /// </summary>
    public TimeSpan? DefaultErrorRetryInterval { get; set; } = TimeSpan.FromSeconds(30);
}
