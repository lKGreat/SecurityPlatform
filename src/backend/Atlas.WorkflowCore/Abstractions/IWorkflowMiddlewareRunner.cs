using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流中间件运行器接口
/// </summary>
public interface IWorkflowMiddlewareRunner
{
    /// <summary>
    /// 运行指定阶段的工作流中间件
    /// </summary>
    Task RunPreMiddleware(WorkflowInstance workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// 运行执行阶段的工作流中间件
    /// </summary>
    Task RunExecuteMiddleware(WorkflowInstance workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// 运行执行后的工作流中间件
    /// </summary>
    Task RunPostMiddleware(WorkflowInstance workflow, CancellationToken cancellationToken = default);
}
