namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 工作流错误处理策略枚举
/// </summary>
public enum WorkflowErrorHandling
{
    /// <summary>
    /// 重试执行
    /// </summary>
    Retry = 0,

    /// <summary>
    /// 挂起工作流
    /// </summary>
    Suspend = 1,

    /// <summary>
    /// 终止工作流
    /// </summary>
    Terminate = 2,

    /// <summary>
    /// 执行补偿逻辑
    /// </summary>
    Compensate = 3
}
