using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流中间件接口
/// </summary>
public interface IWorkflowMiddleware
{
    /// <summary>
    /// 工作流执行前处理
    /// </summary>
    Task HandlePreWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// 工作流执行中处理
    /// </summary>
    Task HandleExecuteWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// 工作流执行后处理
    /// </summary>
    Task HandlePostWorkflow(WorkflowInstance workflow, CancellationToken cancellationToken = default);
}
