using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Abstractions.Persistence;
using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 工作流清理器实现
/// </summary>
public class WorkflowPurger : IWorkflowPurger
{
    private readonly IPersistenceProvider _persistenceProvider;
    private readonly ILogger<WorkflowPurger> _logger;

    public WorkflowPurger(
        IPersistenceProvider persistenceProvider,
        ILogger<WorkflowPurger> logger)
    {
        _persistenceProvider = persistenceProvider;
        _logger = logger;
    }

    public async Task PurgeWorkflows(WorkflowStatus status, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始清理工作流: 状态={Status}, 早于={OlderThan}", status, olderThan);

        try
        {
            // 获取符合条件的工作流实例ID
            var workflowIds = await _persistenceProvider.GetRunnableInstances(olderThan);

            int purgedCount = 0;
            foreach (var workflowId in workflowIds)
            {
                try
                {
                    // 获取工作流实例详情
                    var workflow = await _persistenceProvider.GetWorkflowAsync(workflowId, cancellationToken);
                    if (workflow == null)
                        continue;

                    // 检查是否符合清理条件
                    if (workflow.Status == status && workflow.CompleteTime.HasValue && workflow.CompleteTime.Value < olderThan)
                    {
                        // 删除工作流及其相关数据
                        await _persistenceProvider.TerminateWorkflowAsync(workflow.Id, cancellationToken);
                        purgedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理工作流失败: {WorkflowId}", workflowId);
                }
            }

            _logger.LogInformation("已清理 {Count} 个工作流实例", purgedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作流清理过程失败");
            throw;
        }
    }
}
