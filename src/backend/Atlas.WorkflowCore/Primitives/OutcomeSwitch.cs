using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// 结果开关步骤 - 多结果分支选择
/// </summary>
public class OutcomeSwitch : ContainerStepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        // 容器步骤由引擎处理子步骤
        return ExecutionResult.Next();
    }
}
