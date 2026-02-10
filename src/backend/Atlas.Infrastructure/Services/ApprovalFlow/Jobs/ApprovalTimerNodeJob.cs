using Atlas.Application.Approval.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;
using Atlas.Infrastructure.Services.ApprovalFlow;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Atlas.Infrastructure.Services.ApprovalFlow.Jobs;

/// <summary>
/// 定时器节点任务（后台定时扫描到期的 Timer 节点并推进流程）
/// </summary>
public sealed class ApprovalTimerNodeJob
{
    private readonly ISqlSugarClient _db;
    private readonly IApprovalInstanceRepository _instanceRepository;
    private readonly IApprovalFlowRepository _flowRepository;
    private readonly IApprovalNodeExecutionRepository _nodeExecutionRepository;
    private readonly FlowEngine _flowEngine;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ApprovalTimerNodeJob> _logger;

    public ApprovalTimerNodeJob(
        ISqlSugarClient db,
        IApprovalInstanceRepository instanceRepository,
        IApprovalFlowRepository flowRepository,
        IApprovalNodeExecutionRepository nodeExecutionRepository,
        FlowEngine flowEngine,
        TimeProvider timeProvider,
        ILogger<ApprovalTimerNodeJob> logger)
    {
        _db = db;
        _instanceRepository = instanceRepository;
        _flowRepository = flowRepository;
        _nodeExecutionRepository = nodeExecutionRepository;
        _flowEngine = flowEngine;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        // 查询到期任务
        var jobs = await _db.Queryable<ApprovalTimerJob>()
            .Where(j => j.Status == 0 && j.ScheduledAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            try
            {
                var instance = await _instanceRepository.GetByIdAsync(job.TenantId, job.InstanceId, cancellationToken);
                if (instance == null || instance.Status != ApprovalInstanceStatus.Running)
                {
                    job.MarkCancelled(now);
                    await _db.Updateable(job).ExecuteCommandAsync(cancellationToken);
                    continue;
                }

                var flowDef = await _flowRepository.GetByIdAsync(job.TenantId, instance.DefinitionId, cancellationToken);
                if (flowDef == null) continue;

                var flowDefinition = FlowDefinitionParser.Parse(flowDef.DefinitionJson);

                // 标记定时器节点执行完成
                var nodeExecution = await _nodeExecutionRepository.GetByInstanceAndNodeAsync(
                    job.TenantId, job.InstanceId, job.NodeId, cancellationToken);
                if (nodeExecution != null)
                {
                    nodeExecution.MarkCompleted(now);
                    await _nodeExecutionRepository.UpdateAsync(nodeExecution, cancellationToken);
                }

                // 推进流程到下一个节点
                await _flowEngine.AdvanceFlowAsync(job.TenantId, instance, flowDefinition, job.NodeId, cancellationToken);
                await _instanceRepository.UpdateAsync(instance, cancellationToken);

                job.MarkExecuted(now);
                await _db.Updateable(job).ExecuteCommandAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行定时器节点任务失败: {JobId}", job.Id);
            }
        }
    }
}
