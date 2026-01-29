using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// Saga 容器步骤 - 支持补偿的事务性步骤容器
/// </summary>
public class SagaContainer : ContainerStepBody
{
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        // Saga 容器由引擎处理子步骤和补偿逻辑
        return ExecutionResult.Next();
    }
}
