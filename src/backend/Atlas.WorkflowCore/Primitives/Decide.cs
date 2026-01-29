using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// 决策步骤 - 根据表达式值决定下一步
/// </summary>
public class Decide : StepBody
{
    /// <summary>
    /// 决策表达式结果
    /// </summary>
    public object? Expression { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        return ExecutionResult.Outcome(Expression);
    }
}
