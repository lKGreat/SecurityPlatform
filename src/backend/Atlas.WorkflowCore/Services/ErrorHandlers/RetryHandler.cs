using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.ErrorHandlers;

/// <summary>
/// 重试错误处理器
/// </summary>
public class RetryHandler : IWorkflowErrorHandler
{
    private readonly ILogger<RetryHandler> _logger;

    public RetryHandler(ILogger<RetryHandler> logger)
    {
        _logger = logger;
    }

    public WorkflowErrorHandling Type => WorkflowErrorHandling.Retry;

    public Task HandleAsync(
        WorkflowInstance workflow,
        WorkflowDefinition definition,
        ExecutionPointer pointer,
        WorkflowStep step,
        Exception exception,
        CancellationToken cancellationToken)
    {
        pointer.RetryCount++;
        pointer.Status = PointerStatus.Pending;
        pointer.Active = true;

        // 设置重试延迟（指数退避）
        var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, pointer.RetryCount));
        pointer.SleepUntil = DateTime.UtcNow.Add(retryDelay);

        _logger.LogWarning(exception,
            "步骤 {StepName} 执行失败，将在 {RetryDelay} 后重试 (重试次数: {RetryCount})",
            step.Name, retryDelay, pointer.RetryCount);

        return Task.CompletedTask;
    }
}
