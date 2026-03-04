using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Exceptions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;
using Atlas.Infrastructure.Services.ApprovalFlow;

namespace Atlas.Infrastructure.Services.ApprovalFlow.Operations;

/// <summary>
/// 流程推进操作处理器（管理员跳过当前节点，直接推进到下一个节点）
/// </summary>
public sealed class ProcessMoveAheadOperationHandler : IApprovalOperationHandler
{
    private readonly IApprovalInstanceRepository _instanceRepository;
    private readonly IApprovalFlowRepository _flowRepository;
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalNodeExecutionRepository _nodeExecutionRepository;
    private readonly IApprovalHistoryRepository _historyRepository;
    private readonly FlowEngine _flowEngine;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;

    public ApprovalOperationType SupportedOperationType => ApprovalOperationType.ProcessMoveAhead;

    public ProcessMoveAheadOperationHandler(
        IApprovalInstanceRepository instanceRepository,
        IApprovalFlowRepository flowRepository,
        IApprovalTaskRepository taskRepository,
        IApprovalNodeExecutionRepository nodeExecutionRepository,
        IApprovalHistoryRepository historyRepository,
        FlowEngine flowEngine,
        IIdGeneratorAccessor idGeneratorAccessor)
    {
        _instanceRepository = instanceRepository;
        _flowRepository = flowRepository;
        _taskRepository = taskRepository;
        _nodeExecutionRepository = nodeExecutionRepository;
        _historyRepository = historyRepository;
        _flowEngine = flowEngine;
        _idGeneratorAccessor = idGeneratorAccessor;
    }

    public async Task ExecuteAsync(
        TenantId tenantId,
        long instanceId,
        long? taskId,
        long operatorUserId,
        ApprovalOperationRequest request,
        CancellationToken cancellationToken)
    {
        var instance = await _instanceRepository.GetByIdAsync(tenantId, instanceId, cancellationToken);
        if (instance == null || instance.Status != ApprovalInstanceStatus.Running)
        {
            throw new BusinessException("INSTANCE_NOT_RUNNING", "流程实例不在运行状态");
        }

        var flowDef = await _flowRepository.GetByIdAsync(tenantId, instance.DefinitionId, cancellationToken);
        if (flowDef == null)
        {
            throw new BusinessException("FLOW_NOT_FOUND", "流程定义不存在");
        }

        var flowDefinition = FlowDefinitionParser.Parse(flowDef.DefinitionJson);
        
        // 获取当前节点
        var currentNodeId = instance.CurrentNodeId;
        if (string.IsNullOrEmpty(currentNodeId))
        {
            throw new BusinessException("NO_CURRENT_NODE", "流程实例没有当前节点");
        }

        // 取消当前节点的所有待审批任务
        var pendingTasks = await _taskRepository.GetByInstanceAndStatusAsync(tenantId, instanceId, ApprovalTaskStatus.Pending, cancellationToken);
        var currentNodeTasks = pendingTasks.Where(t => t.NodeId == currentNodeId).ToList();
        if (currentNodeTasks.Count > 0)
        {
            foreach (var task in currentNodeTasks)
            {
                task.Cancel();
            }
            await _taskRepository.UpdateRangeAsync(currentNodeTasks, cancellationToken);
        }

        // 标记当前节点为已完成
        var nodeExecution = await _nodeExecutionRepository.GetByInstanceAndNodeAsync(tenantId, instanceId, currentNodeId, cancellationToken);
        if (nodeExecution != null)
        {
            nodeExecution.MarkCompleted(DateTimeOffset.UtcNow);
            await _nodeExecutionRepository.UpdateAsync(nodeExecution, cancellationToken);
        }

        // 推进流程到下一个节点
        await _flowEngine.AdvanceFlowAsync(tenantId, instance, flowDefinition, currentNodeId, cancellationToken);

        // 更新实例
        await _instanceRepository.UpdateAsync(instance, cancellationToken);

        // 记录历史事件
        var moveAheadEvent = new ApprovalHistoryEvent(
            tenantId,
            instanceId,
            ApprovalHistoryEventType.NodeAdvanced,
            currentNodeId,
            instance.CurrentNodeId,
            operatorUserId,
            _idGeneratorAccessor.NextId());
        await _historyRepository.AddAsync(moveAheadEvent, cancellationToken);
    }
}





