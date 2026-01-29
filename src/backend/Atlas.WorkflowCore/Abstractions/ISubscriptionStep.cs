using System.Linq.Expressions;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 订阅步骤接口
/// </summary>
public interface ISubscriptionStep
{
    /// <summary>
    /// 事件名称
    /// </summary>
    string? EventName { get; set; }

    /// <summary>
    /// 事件键表达式
    /// </summary>
    LambdaExpression? EventKey { get; set; }
}

/// <summary>
/// 订阅步骤接口（泛型版本）
/// </summary>
/// <typeparam name="TStepBody">步骤体类型</typeparam>
public interface ISubscriptionStep<TStepBody> : ISubscriptionStep
    where TStepBody : ISubscriptionBody
{
}
