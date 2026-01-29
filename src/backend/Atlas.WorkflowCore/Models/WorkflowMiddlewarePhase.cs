namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 工作流中间件执行阶段
/// </summary>
public enum WorkflowMiddlewarePhase
{
    /// <summary>
    /// 工作流执行前
    /// </summary>
    PreWorkflow = 0,

    /// <summary>
    /// 工作流执行中
    /// </summary>
    ExecuteWorkflow = 1,

    /// <summary>
    /// 工作流执行后
    /// </summary>
    PostWorkflow = 2
}
