using System;
using Atlas.WorkflowCore.Abstractions;

namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 操作参数 - 支持Action委托进行输入输出映射
/// </summary>
public class ActionParameter<TStepBody, TData> : IStepParameter
    where TStepBody : IStepBody
{
    private readonly Action<TStepBody, TData, IStepExecutionContext> _action;

    public ActionParameter(Action<TStepBody, TData, IStepExecutionContext> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public ActionParameter(Action<TStepBody, TData> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _action = new Action<TStepBody, TData, IStepExecutionContext>((body, data, context) =>
        {
            action(body, data);
        });
    }

    private void Assign(object data, IStepBody step, IStepExecutionContext context)
    {
        if (step is TStepBody stepBody && data is TData typedData)
        {
            _action.Invoke(stepBody, typedData, context);
        }
    }

    public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
    {
        Assign(data, body, context);
    }

    public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
    {
        Assign(data, body, context);
    }

    public object? Resolve(object? data)
    {
        // 向后兼容：Action参数不返回值
        return null;
    }
}
