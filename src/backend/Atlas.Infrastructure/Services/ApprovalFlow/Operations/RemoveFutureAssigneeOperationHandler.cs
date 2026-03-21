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
/// 未来节点减签操作处理器（移除流程中尚未到达的节点的审批人）
/// </summary>
public sealed class RemoveFutureAssigneeOperationHandler : IApprovalOperationHandler
{
    private readonly IApprovalInstanceRepository _instanceRepository;
    private readonly IApprovalFlowRepository _flowRepository;
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalTaskAssigneeChangeRepository _assigneeChangeRepository;
    private readonly IApprovalHistoryRepository _historyRepository;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;

    public ApprovalOperationType SupportedOperationType => ApprovalOperationType.RemoveFutureAssignee;

    public RemoveFutureAssigneeOperationHandler(
        IApprovalInstanceRepository instanceRepository,
        IApprovalFlowRepository flowRepository,
        IApprovalTaskRepository taskRepository,
        IApprovalTaskAssigneeChangeRepository assigneeChangeRepository,
        IApprovalHistoryRepository historyRepository,
        IIdGeneratorAccessor idGeneratorAccessor)
    {
        _instanceRepository = instanceRepository;
        _flowRepository = flowRepository;
        _taskRepository = taskRepository;
        _assigneeChangeRepository = assigneeChangeRepository;
        _historyRepository = historyRepository;
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
        if (string.IsNullOrEmpty(request.TargetNodeId))
        {
            throw new BusinessException("TARGET_NODE_REQUIRED", "ApprovalOpFutureRemoveNodeRequired");
        }

        if (string.IsNullOrEmpty(request.TargetAssigneeValue))
        {
            throw new BusinessException("TARGET_ASSIGNEE_REQUIRED", "ApprovalOpFutureRemoveAssigneeRequired");
        }

        var instance = await _instanceRepository.GetByIdAsync(tenantId, instanceId, cancellationToken);
        if (instance == null || instance.Status != ApprovalInstanceStatus.Running)
        {
            throw new BusinessException("INSTANCE_NOT_RUNNING", "ApprovalInstanceNotRunning");
        }

        var flowDef = await _flowRepository.GetByIdAsync(tenantId, instance.DefinitionId, cancellationToken);
        if (flowDef == null)
        {
            throw new BusinessException("FLOW_NOT_FOUND", "ApprovalFlowDefNotFoundShort");
        }

        var flowDefinition = FlowDefinitionParser.Parse(flowDef.DefinitionJson);
        var targetNode = flowDefinition.GetNodeById(request.TargetNodeId);
        if (targetNode == null)
        {
            throw new BusinessException("NODE_NOT_FOUND", "ApprovalOpTargetNodeNotFound");
        }

        // 检查目标节点是否已经执行过
        var existingTasks = await _taskRepository.GetByInstanceAndNodeAsync(tenantId, instanceId, request.TargetNodeId, cancellationToken);
        if (existingTasks.Count > 0)
        {
            throw new BusinessException("NODE_ALREADY_EXECUTED", "ApprovalOpFutureRemoveNodeExecuted");
        }

        // 记录未来节点减签
        var change = new ApprovalTaskAssigneeChange(
            tenantId,
            instanceId,
            request.TargetNodeId,
            request.TargetAssigneeValue,
            AssigneeChangeType.RemoveFuture,
            operatorUserId,
            _idGeneratorAccessor.NextId(),
            null,
            request.Comment);
        await _assigneeChangeRepository.AddAsync(change, cancellationToken);

        // 记录历史事件
        var removeFutureEvent = new ApprovalHistoryEvent(
            tenantId,
            instanceId,
            ApprovalHistoryEventType.NodeAdvanced,
            instance.CurrentNodeId,
            request.TargetNodeId,
            operatorUserId,
            _idGeneratorAccessor.NextId());
        await _historyRepository.AddAsync(removeFutureEvent, cancellationToken);
    }
}





