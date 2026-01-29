using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流中间件错误处理器接口
/// </summary>
public interface IWorkflowMiddlewareErrorHandler
{
    /// <summary>
    /// 处理中间件执行错误
    /// </summary>
    Task HandleAsync(Exception exception);
}
