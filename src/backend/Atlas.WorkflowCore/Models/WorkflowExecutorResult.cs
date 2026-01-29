namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 工作流执行器结果
/// </summary>
public class WorkflowExecutorResult
{
    /// <summary>
    /// 事件订阅列表
    /// </summary>
    public List<EventSubscription> Subscriptions { get; set; } = new();

    /// <summary>
    /// 执行错误列表
    /// </summary>
    public List<ExecutionError> Errors { get; set; } = new();

    /// <summary>
    /// 重试执行指针列表
    /// </summary>
    public List<ExecutionPointer> RetryPointers { get; set; } = new();
}
