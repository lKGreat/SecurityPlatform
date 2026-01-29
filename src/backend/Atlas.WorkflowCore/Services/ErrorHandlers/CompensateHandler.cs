using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services.ErrorHandlers;

/// <summary>
/// 补偿错误处理器
/// </summary>
public class CompensateHandler : IWorkflowErrorHandler
{
    private readonly IExecutionPointerFactory _pointerFactory;
    private readonly ILogger<CompensateHandler> _logger;

    public CompensateHandler(
        IExecutionPointerFactory pointerFactory,
        ILogger<CompensateHandler> logger)
    {
        _pointerFactory = pointerFactory;
        _logger = logger;
    }

    public WorkflowErrorHandling Type => WorkflowErrorHandling.Compensate;

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

        // TODO: 实现完整的补偿逻辑
        // 1. 查找步骤的 CompensationStepId
        // 2. 创建补偿执行指针
        // 3. 添加到工作流实例

        _logger.LogError(exception,
            "步骤 {StepName} 执行失败，触发补偿逻辑 (工作流: {WorkflowId})",
            step.Name, workflow.Id);

        return Task.CompletedTask;
    }
}
