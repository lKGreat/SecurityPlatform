using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Builders;

/// <summary>
/// 容器步骤构建器实现
/// </summary>
/// <typeparam name="TData">工作流数据类型</typeparam>
/// <typeparam name="TReturnStep">返回的步骤构建器类型</typeparam>
public class ContainerStepBuilder<TData, TReturnStep> : IContainerStepBuilder<TData, TReturnStep>
    where TData : new()
{
    private readonly IWorkflowBuilder<TData> _workflowBuilder;
    private readonly WorkflowStep _containerStep;
    private readonly TReturnStep _returnStep;

    public ContainerStepBuilder(
        IWorkflowBuilder<TData> workflowBuilder,
        WorkflowStep containerStep,
        TReturnStep returnStep)
    {
        _workflowBuilder = workflowBuilder;
        _containerStep = containerStep;
        _returnStep = returnStep;
    }

    public TReturnStep Do(Action<IWorkflowBuilder<TData>> builder)
    {
        var branchBuilder = (_workflowBuilder as WorkflowBuilder<TData>)!.CreateBranch();
        builder(branchBuilder);
        (_workflowBuilder as WorkflowBuilder<TData>)!.AttachBranch(branchBuilder);

        if (branchBuilder.Steps.Count > 0)
        {
            _containerStep.Children.Add(branchBuilder.Steps[0].Id);
        }

        return _returnStep;
    }
}
