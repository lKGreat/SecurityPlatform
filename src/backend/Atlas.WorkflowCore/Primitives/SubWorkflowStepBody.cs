using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// 子工作流步骤体 - 执行子工作流
/// </summary>
public class SubWorkflowStepBody : StepBody
{
    /// <summary>
    /// 子工作流ID
    /// </summary>
    public string ChildWorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// 子工作流版本
    /// </summary>
    public int? ChildWorkflowVersion { get; set; }

    /// <summary>
    /// 子工作流数据
    /// </summary>
    public object? ChildWorkflowData { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        // TODO: 实现子工作流启动逻辑
        // 1. 启动子工作流
        // 2. 等待子工作流完成
        // 3. 返回结果

        return ExecutionResult.Next();
    }
}
