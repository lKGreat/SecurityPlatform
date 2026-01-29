using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// 调度执行步骤 - 延迟后执行子步骤
/// </summary>
public class Schedule : ContainerStepBody
{
    /// <summary>
    /// 延迟时间
    /// </summary>
    public TimeSpan Interval { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        // 延迟后执行子步骤
        return ExecutionResult.Sleep(Interval, null);
    }
}
