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
/// 打回修改操作处理器
/// </summary>
public sealed class BackToModifyOperationHandler : IApprovalOperationHandler
{
    private readonly IApprovalInstanceRepository _instanceRepository;
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalHistoryRepository _historyRepository;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;

    public ApprovalOperationType SupportedOperationType => ApprovalOperationType.BackToModify;

    public BackToModifyOperationHandler(
        IApprovalInstanceRepository instanceRepository,
        IApprovalTaskRepository taskRepository,
        IApprovalHistoryRepository historyRepository,
        IIdGeneratorAccessor idGeneratorAccessor)
    {
        _instanceRepository = instanceRepository;
        _taskRepository = taskRepository;
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
        if (!taskId.HasValue)
        {
            throw new BusinessException("TASK_ID_REQUIRED", "ApprovalOpTaskIdRequired");
        }

        var task = await _taskRepository.GetByIdAsync(tenantId, taskId.Value, cancellationToken);
        if (task == null)
        {
            throw new BusinessException("TASK_NOT_FOUND", "ApprovalTaskNotFound");
        }

        if (task.InstanceId != instanceId)
        {
            throw new BusinessException("TASK_INSTANCE_MISMATCH", "ApprovalOpTaskInstanceMismatch");
        }

        if (task.Status != ApprovalTaskStatus.Pending)
        {
            throw new BusinessException("TASK_NOT_PENDING", "ApprovalOpOnlyPendingTask");
        }

        if (task.AssigneeType != AssigneeType.User || task.AssigneeValue != operatorUserId.ToString())
        {
            throw new BusinessException("FORBIDDEN", "ApprovalOpOnlyCurrentHandler");
        }

        var instance = await _instanceRepository.GetByIdAsync(tenantId, instanceId, cancellationToken);
        if (instance == null || instance.Status != ApprovalInstanceStatus.Running)
        {
            throw new BusinessException("INSTANCE_NOT_RUNNING", "ApprovalInstanceNotRunning");
        }

        // 标记任务为驳回
        task.Reject(operatorUserId, request.Comment ?? "打回修改", DateTimeOffset.UtcNow);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        // 取消所有待审批任务
        var pendingTasks = await _taskRepository.GetByInstanceAndStatusAsync(tenantId, instanceId, ApprovalTaskStatus.Pending, cancellationToken);
        if (pendingTasks.Count > 0)
        {
            foreach (var pendingTask in pendingTasks)
            {
                pendingTask.Cancel();
            }
            await _taskRepository.UpdateRangeAsync(pendingTasks, cancellationToken);
        }

        // 流程状态改为驳回（打回修改）
        instance.MarkRejected(DateTimeOffset.UtcNow);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);

        // 记录打回事件
        var backToModifyEvent = new ApprovalHistoryEvent(
            tenantId,
            instanceId,
            ApprovalHistoryEventType.TaskRejected,
            task.NodeId,
            null,
            operatorUserId,
            _idGeneratorAccessor.NextId());
        await _historyRepository.AddAsync(backToModifyEvent, cancellationToken);
    }
}





