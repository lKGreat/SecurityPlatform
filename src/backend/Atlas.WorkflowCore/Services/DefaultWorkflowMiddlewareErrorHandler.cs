using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 默认工作流中间件错误处理器
/// </summary>
public class DefaultWorkflowMiddlewareErrorHandler : IWorkflowMiddlewareErrorHandler
{
    private readonly ILogger<DefaultWorkflowMiddlewareErrorHandler> _logger;

    public DefaultWorkflowMiddlewareErrorHandler(ILogger<DefaultWorkflowMiddlewareErrorHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleError(
        WorkflowInstance workflow,
        WorkflowMiddlewarePhase phase,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception,
            "工作流中间件错误 (阶段: {Phase}, 工作流: {WorkflowId})",
            phase, workflow.Id);

        // 默认实现：记录错误但不中断工作流执行
        return Task.CompletedTask;
    }
}
