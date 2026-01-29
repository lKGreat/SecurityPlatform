namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 工作流步骤中间件接口
/// </summary>
public interface IWorkflowStepMiddleware
{
    /// <summary>
    /// 步骤执行前处理
    /// </summary>
    Task HandlePreStep(IStepExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 步骤执行后处理
    /// </summary>
    Task HandlePostStep(IStepExecutionContext context, CancellationToken cancellationToken = default);
}
