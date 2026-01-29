using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流中间件接口
/// </summary>
public interface IWorkflowMiddleware
{
    /// <summary>
    /// 中间件执行阶段
    /// </summary>
    WorkflowMiddlewarePhase Phase { get; }

    /// <summary>
    /// 处理工作流中间件
    /// </summary>
    /// <param name="workflow">工作流实例</param>
    /// <param name="next">下一个中间件委托</param>
    Task HandleAsync(WorkflowInstance workflow, WorkflowDelegate next);
}
