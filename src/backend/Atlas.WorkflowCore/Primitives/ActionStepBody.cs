using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// 操作步骤体 - 执行简单操作的步骤体
/// </summary>
public class ActionStepBody : StepBody
{
    /// <summary>
    /// 操作委托
    /// </summary>
    public Action<IStepExecutionContext>? Action { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Action?.Invoke(context);
        return ExecutionResult.Next();
    }
}
