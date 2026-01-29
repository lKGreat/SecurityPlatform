using Atlas.WorkflowCore.Abstractions;
using System.Linq.Expressions;

namespace Atlas.WorkflowCore.Models;

public class ExpressionStepParameter<TSource, TValue> : IStepParameter
{
    private readonly Expression<Func<TSource, TValue>> _expression;

    public ExpressionStepParameter(Expression<Func<TSource, TValue>> expression)
    {
        _expression = expression;
    }

    public object? Resolve(object? data)
    {
        if (data is TSource source)
        {
            var compiled = _expression.Compile();
            return compiled(source);
        }
        return null;
    }

    public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
    {
        // ExpressionStepParameter主要用于简单值解析，不支持直接映射到步骤属性
        // 如果需要步骤属性映射，应使用MemberMapParameter
    }

    public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
    {
        // ExpressionStepParameter主要用于简单值解析，不支持直接映射到步骤属性
        // 如果需要步骤属性映射，应使用MemberMapParameter
    }
}

public class ConstantStepParameter<TValue> : IStepParameter
{
    private readonly string _name;
    private readonly TValue _value;

    public ConstantStepParameter(string name, TValue value)
    {
        _name = name;
        _value = value;
    }

    public object? Resolve(object? data)
    {
        return _value;
    }

    public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
    {
        // 常量参数不支持映射到步骤属性
    }

    public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
    {
        // 常量参数不支持映射到步骤属性
    }
}

public class FuncStepParameter<TSource, TValue> : IStepParameter
{
    private readonly string _name;
    private readonly Func<TSource, TValue> _func;

    public FuncStepParameter(string name, Func<TSource, TValue> func)
    {
        _name = name;
        _func = func;
    }

    public object? Resolve(object? data)
    {
        if (data is TSource source)
        {
            return _func(source);
        }
        return null;
    }

    public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
    {
        // FuncStepParameter主要用于简单值解析，不支持直接映射到步骤属性
    }

    public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
    {
        // FuncStepParameter主要用于简单值解析，不支持直接映射到步骤属性
    }
}

public class ActionStepParameter<TData> : IStepParameter
{
    private readonly Action<TData> _action;

    public ActionStepParameter(Action<TData> action)
    {
        _action = action;
    }

    public object? Resolve(object? data)
    {
        if (data is TData typedData)
        {
            _action(typedData);
        }
        return null;
    }

    public void AssignInput(object data, IStepBody body, IStepExecutionContext context)
    {
        // ActionStepParameter主要用于数据操作，不支持直接映射到步骤属性
        // 如果需要步骤属性映射，应使用ActionParameter<TStepBody, TData>
        if (data is TData typedData)
        {
            _action(typedData);
        }
    }

    public void AssignOutput(object data, IStepBody body, IStepExecutionContext context)
    {
        // ActionStepParameter主要用于数据操作，不支持直接映射到步骤属性
        // 如果需要步骤属性映射，应使用ActionParameter<TStepBody, TData>
        if (data is TData typedData)
        {
            _action(typedData);
        }
    }
}
