using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.ErrorHandlers;

/// <summary>
/// 挂起错误处理器
/// </summary>
public class SuspendHandler : IWorkflowErrorHandler
{
    private readonly ILogger<SuspendHandler> _logger;

    public SuspendHandler(ILogger<SuspendHandler> logger)
    {
        _logger = logger;
    }

    public WorkflowErrorHandling Type => WorkflowErrorHandling.Suspend;

    public Task HandleAsync(
        WorkflowInstance workflow,
        WorkflowDefinition definition,
        ExecutionPointer pointer,
        WorkflowStep step,
        Exception exception,
        CancellationToken cancellationToken)
    {
        pointer.Status = PointerStatus.Failed;
        pointer.Active = false;
        pointer.EndTime = DateTime.UtcNow;

        workflow.Status = WorkflowStatus.Suspended;

        _logger.LogError(exception,
            "步骤 {StepName} 执行失败，工作流已挂起: {WorkflowId}",
            step.Name, workflow.Id);

        return Task.CompletedTask;
    }
}
