using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 活动控制器接口 - 管理外部活动
/// </summary>
public interface IActivityController
{
    /// <summary>
    /// 获取待处理的活动（长轮询）
    /// </summary>
    /// <param name="activityName">活动名称</param>
    /// <param name="workerId">工作者ID</param>
    /// <param name="timeout">超时时间（可选）</param>
    /// <returns>待处理活动，如果超时则返回null</returns>
    Task<PendingActivity?> GetPendingActivity(string activityName, string workerId, TimeSpan? timeout = null);

    /// <summary>
    /// 释放活动令牌（返还给队列）
    /// </summary>
    /// <param name="token">活动令牌</param>
    Task ReleaseActivityToken(string token);

    /// <summary>
    /// 提交活动成功结果
    /// </summary>
    /// <param name="token">活动令牌</param>
    /// <param name="result">结果数据</param>
    Task SubmitActivitySuccess(string token, object result);

    /// <summary>
    /// 提交活动失败结果
    /// </summary>
    /// <param name="token">活动令牌</param>
    /// <param name="result">失败结果</param>
    Task SubmitActivityFailure(string token, object result);
}
