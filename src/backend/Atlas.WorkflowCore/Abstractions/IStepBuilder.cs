using System.Linq.Expressions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

public interface IStepBuilder<TData>
{
    // 步骤配置
    IStepBuilder<TData> Name(string name);
    IStepBuilder<TData> Id(string id);

    // 输入输出映射
    IStepBuilder<TData> Input<TInput>(Expression<Func<TData, TInput>> value);
    IStepBuilder<TData> Input<TInput>(string name, TInput value);
    IStepBuilder<TData> Input<TInput>(string name, Func<TData, TInput> value);

    IStepBuilder<TData> Output<TInput>(Expression<Func<TData, TInput>> value, Expression<Func<IStepExecutionContext, TInput>> assign);
    IStepBuilder<TData> Output<TInput>(string name, Expression<Func<IStepExecutionContext, TInput>> assign);

    // 流程控制
    IStepBuilder<TData> Then<TStep>(Action<IStepBuilder<TData>>? stepSetup = null)
        where TStep : IStepBody;
    IStepBuilder<TData> Then(Func<IStepExecutionContext, ExecutionResult> body);
    IStepBuilder<TData> Then(string name, Func<IStepExecutionContext, ExecutionResult> body);

    // 错误处理和补偿
    IStepBuilder<TData> OnError(WorkflowErrorHandling behavior, TimeSpan? retryInterval = null);
    IStepBuilder<TData> CompensateWith<TStep>(Action<IStepBuilder<TData>>? stepSetup = null) where TStep : IStepBody;
    IStepBuilder<TData> CompensateWith(Func<IStepExecutionContext, ExecutionResult> body);
    IStepBuilder<TData> CompensateWithSequence(Action<IWorkflowBuilder<TData>> builder);

    // 取消条件
    IStepBuilder<TData> CancelCondition(Expression<Func<TData, bool>> cancelCondition, bool proceedAfterCancel = false);

    IWorkflowBuilder<TData> End(string? name = null);
}
