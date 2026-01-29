using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流中间件运行器接口
/// </summary>
public interface IWorkflowMiddlewareRunner
{
    /// <summary>
    /// 运行Pre阶段的工作流中间件
    /// </summary>
    Task RunPreMiddleware(WorkflowInstance workflow, WorkflowDefinition def);

    /// <summary>
    /// 运行Execute阶段的工作流中间件
    /// </summary>
    Task RunExecuteMiddleware(WorkflowInstance workflow, WorkflowDefinition def);

    /// <summary>
    /// 运行Post阶段的工作流中间件
    /// </summary>
    Task RunPostMiddleware(WorkflowInstance workflow, WorkflowDefinition def);
}
