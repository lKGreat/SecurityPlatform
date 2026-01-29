namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 订阅步骤体接口 - 用于需要接收事件数据的步骤
/// </summary>
public interface ISubscriptionBody : IStepBody
{
    /// <summary>
    /// 事件数据（由引擎在事件到达时注入）
    /// </summary>
    object? EventData { get; set; }
}
