using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// When 步骤 - 结果分支容器
/// </summary>
public class When : ContainerStepBody
{
    /// <summary>
    /// 匹配值
    /// </summary>
    public object? MatchValue { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        // 容器步骤由引擎处理子步骤
        return ExecutionResult.Next();
    }
}
