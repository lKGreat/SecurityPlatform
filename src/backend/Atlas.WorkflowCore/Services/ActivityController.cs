using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 活动控制器实现
/// </summary>
public class ActivityController : IActivityController
{
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly IQueueProvider _queueProvider;
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(
        IPersistenceProvider persistenceProvider,
        IQueueProvider queueProvider,
        ILogger<ActivityController> logger)
    {
        _persistenceProvider = persistenceProvider;
        _queueProvider = queueProvider;
        _logger = logger;
    }

    public async Task<IEnumerable<WorkflowActivity>> GetPendingActivities(string? activityName = null, CancellationToken cancellationToken = default)
    {
        // TODO: 实现从持久化提供者获取待处理活动
        _logger.LogDebug("获取待处理活动: {ActivityName}", activityName ?? "全部");
        return Array.Empty<WorkflowActivity>();
    }

    public Task ReleaseActivityToken(string token, string workerId)
    {
        // TODO: 实现释放活动令牌
        _logger.LogDebug("释放活动令牌: {Token} (工作者: {WorkerId})", token, workerId);
        return Task.CompletedTask;
    }

    public async Task SubmitActivitySuccess(string token, object? data)
    {
        // TODO: 实现提交活动成功结果
        // 1. 根据 token 查找对应的工作流实例和执行指针
        // 2. 更新执行指针状态和数据
        // 3. 将工作流入队执行

        _logger.LogInformation("活动成功: {Token}", token);
        await Task.CompletedTask;
    }

    public async Task SubmitActivityFailure(string token, string message)
    {
        // TODO: 实现提交活动失败结果
        // 1. 根据 token 查找对应的工作流实例和执行指针
        // 2. 更新执行指针状态为失败
        // 3. 触发错误处理逻辑

        _logger.LogWarning("活动失败: {Token} - {Message}", token, message);
        await Task.CompletedTask;
    }
}
