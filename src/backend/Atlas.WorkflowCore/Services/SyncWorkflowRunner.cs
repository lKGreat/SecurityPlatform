using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 同步工作流运行器实现
/// </summary>
public class SyncWorkflowRunner : ISyncWorkflowRunner
{
    private readonly IWorkflowHost _host;
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly ILogger<SyncWorkflowRunner> _logger;

    public SyncWorkflowRunner(
        IWorkflowHost host,
        IPersistenceProvider persistenceProvider,
        ILogger<SyncWorkflowRunner> logger)
    {
        _host = host;
        _persistenceProvider = persistenceProvider;
        _logger = logger;
    }

    public async Task<WorkflowInstance> RunWorkflow<TData>(
        string workflowId,
        int? version,
        TData data,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        // 启动工作流
        var instanceId = await _host.StartWorkflowAsync(workflowId, version, data, null, cancellationToken);

        // 等待工作流完成
        var timeoutTime = timeout.HasValue ? DateTime.UtcNow.Add(timeout.Value) : DateTime.MaxValue;
        var pollInterval = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            if (DateTime.UtcNow > timeoutTime)
            {
                _logger.LogWarning("工作流 {InstanceId} 执行超时", instanceId);
                throw new TimeoutException($"工作流 {instanceId} 执行超时");
            }

            var instance = await _persistenceProvider.GetWorkflowAsync(instanceId, cancellationToken);

            if (instance == null)
            {
                throw new InvalidOperationException($"工作流实例 {instanceId} 不存在");
            }

            if (instance.Status == WorkflowStatus.Complete || instance.Status == WorkflowStatus.Terminated)
            {
                _logger.LogInformation("工作流 {InstanceId} 已完成，状态: {Status}", instanceId, instance.Status);
                return instance;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }
    }
}
