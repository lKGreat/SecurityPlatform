using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 活动控制器接口 - 管理外部活动
/// </summary>
public interface IActivityController
{
    /// <summary>
    /// 获取待处理的活动列表
    /// </summary>
    /// <param name="activityName">活动名称（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>待处理活动列表</returns>
    Task<IEnumerable<WorkflowActivity>> GetPendingActivities(string? activityName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 释放活动令牌（返还给队列）
    /// </summary>
    /// <param name="token">活动令牌</param>
    /// <param name="workerId">工作者ID</param>
    Task ReleaseActivityToken(string token, string workerId);

    /// <summary>
    /// 提交活动成功结果
    /// </summary>
    /// <param name="token">活动令牌</param>
    /// <param name="data">结果数据</param>
    Task SubmitActivitySuccess(string token, object? data);

    /// <summary>
    /// 提交活动失败结果
    /// </summary>
    /// <param name="token">活动令牌</param>
    /// <param name="message">失败消息</param>
    Task SubmitActivityFailure(string token, string message);
}
