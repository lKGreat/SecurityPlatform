using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.ErrorHandlers;

/// <summary>
/// 终止错误处理器
/// </summary>
public class TerminateHandler : IWorkflowErrorHandler
{
    private readonly ILogger<TerminateHandler> _logger;

    public TerminateHandler(ILogger<TerminateHandler> logger)
    {
        _logger = logger;
    }

    public WorkflowErrorHandling Type => WorkflowErrorHandling.Terminate;

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

        workflow.Status = WorkflowStatus.Terminated;
        workflow.CompleteTime = DateTime.UtcNow;

        // 停用所有执行指针
        foreach (var ep in workflow.ExecutionPointers)
        {
            if (ep.Active)
            {
                ep.Active = false;
                ep.EndTime = DateTime.UtcNow;
            }
        }

        _logger.LogError(exception,
            "步骤 {StepName} 执行失败，工作流已终止: {WorkflowId}",
            step.Name, workflow.Id);

        return Task.CompletedTask;
    }
}
