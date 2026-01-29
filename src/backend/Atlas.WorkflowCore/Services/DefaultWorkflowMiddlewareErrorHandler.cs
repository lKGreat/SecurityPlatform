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

    public Task HandleAsync(Exception exception)
    {
        _logger.LogError(exception, "工作流中间件错误: {Message}", exception.Message);
        
        // 默认处理：记录日志但不抛出异常，允许工作流继续执行
        return Task.CompletedTask;
    }
}
