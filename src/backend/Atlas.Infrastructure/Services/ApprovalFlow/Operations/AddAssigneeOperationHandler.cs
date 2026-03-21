using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Exceptions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services.ApprovalFlow.Operations;

/// <summary>
/// 加签操作处理器（支持并行加签、前加签、后加签三种模式）
///
/// 加签模式说明：
/// - 并行加签 (AddSignType=0，默认)：在当前节点创建并行审批任务
/// - 前加签 (AddSignType=1)：挂起当前任务，加签人先审，完成后恢复原任务
/// - 后加签 (AddSignType=2)：当前任务正常审批，完成后引擎自动创建加签任务
/// </summary>
public sealed class AddAssigneeOperationHandler : IApprovalOperationHandler
{
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalTaskAssigneeChangeRepository _assigneeChangeRepository;
    private readonly IApprovalHistoryRepository _historyRepository;
    private readonly IApprovalDepartmentLeaderRepository _deptLeaderRepository;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;

    public ApprovalOperationType SupportedOperationType => ApprovalOperationType.AddAssignee;

    public AddAssigneeOperationHandler(
        IApprovalTaskRepository taskRepository,
        IApprovalTaskAssigneeChangeRepository assigneeChangeRepository,
        IApprovalHistoryRepository historyRepository,
        IApprovalDepartmentLeaderRepository deptLeaderRepository,
        IIdGeneratorAccessor idGeneratorAccessor)
    {
        _taskRepository = taskRepository;
        _assigneeChangeRepository = assigneeChangeRepository;
        _historyRepository = historyRepository;
        _deptLeaderRepository = deptLeaderRepository;
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

        if (request.AdditionalAssigneeValues == null || request.AdditionalAssigneeValues.Count == 0)
        {
            throw new BusinessException("ASSIGNEE_REQUIRED", "ApprovalOpAssigneeRequired");
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

        // Authorization: only the current task assignee can add assignees
        if (task.AssigneeType != AssigneeType.User || task.AssigneeValue != operatorUserId.ToString())
        {
            throw new BusinessException("FORBIDDEN", "ApprovalOpOnlyCurrentHandler");
        }

        var addSignType = request.AddSignType; // 0=parallel, 1=before, 2=after

        switch (addSignType)
        {
            case 1:
                await ExecuteBeforeAddSignAsync(tenantId, instanceId, task, operatorUserId, request, cancellationToken);
                break;
            case 2:
                await ExecuteAfterAddSignAsync(tenantId, instanceId, task, operatorUserId, request, cancellationToken);
                break;
            default:
                await ExecuteParallelAddSignAsync(tenantId, instanceId, task, operatorUserId, request, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// 并行加签：在当前节点创建并行审批任务（默认模式）
    /// </summary>
    private async Task ExecuteParallelAddSignAsync(
        TenantId tenantId,
        long instanceId,
        ApprovalTask currentTask,
        long operatorUserId,
        ApprovalOperationRequest request,
        CancellationToken cancellationToken)
    {
        // 获取同节点的所有任务，用于去重
        var nodeTasks = await _taskRepository.GetByInstanceAndNodeAsync(
            tenantId, instanceId, currentTask.NodeId, cancellationToken);
        var existingAssigneeValues = nodeTasks.Select(t => t.AssigneeValue).ToHashSet();

        var newTasks = new List<ApprovalTask>();
        var changes = new List<ApprovalTaskAssigneeChange>();

        foreach (var assigneeValue in request.AdditionalAssigneeValues!)
        {
            if (existingAssigneeValues.Contains(assigneeValue))
            {
                continue; // 跳过已存在的审批人
            }

            var newTask = new ApprovalTask(
                tenantId,
                instanceId,
                currentTask.NodeId,
                currentTask.Title,
                AssigneeType.User,
                assigneeValue,
                _idGeneratorAccessor.NextId());
            newTasks.Add(newTask);

            changes.Add(new ApprovalTaskAssigneeChange(
                tenantId,
                instanceId,
                currentTask.NodeId,
                assigneeValue,
                AssigneeChangeType.Add,
                operatorUserId,
                _idGeneratorAccessor.NextId(),
                newTask.Id,
                request.Comment));
        }

        // 批量添加任务和变更记录
        if (newTasks.Count > 0)
        {
            await _taskRepository.AddRangeAsync(newTasks, cancellationToken);
            await _assigneeChangeRepository.AddRangeAsync(changes, cancellationToken);
        }

        await RecordHistoryAsync(tenantId, instanceId, currentTask.NodeId, operatorUserId, "并行加签", cancellationToken);
    }

    /// <summary>
    /// 前加签：挂起当前任务 → 加签人先审 → 完成后恢复原任务
    /// </summary>
    private async Task ExecuteBeforeAddSignAsync(
        TenantId tenantId,
        long instanceId,
        ApprovalTask currentTask,
        long operatorUserId,
        ApprovalOperationRequest request,
        CancellationToken cancellationToken)
    {
        // 挂起当前任务（标记为 Waiting 状态）
        currentTask.Suspend();
        await _taskRepository.UpdateAsync(currentTask, cancellationToken);

        // 为加签人创建任务，设置 parentTaskId 指向原任务
        var newTasks = new List<ApprovalTask>();
        var changes = new List<ApprovalTaskAssigneeChange>();

        foreach (var assigneeValue in request.AdditionalAssigneeValues!)
        {
            var newTask = new ApprovalTask(
                tenantId,
                instanceId,
                currentTask.NodeId,
                $"[前加签] {currentTask.Title}",
                AssigneeType.User,
                assigneeValue,
                _idGeneratorAccessor.NextId());
            newTask.SetParentTaskId(currentTask.Id);
            newTask.SetTaskType(12); // 前加签任务类型标识
            newTasks.Add(newTask);

            changes.Add(new ApprovalTaskAssigneeChange(
                tenantId,
                instanceId,
                currentTask.NodeId,
                assigneeValue,
                AssigneeChangeType.Add,
                operatorUserId,
                _idGeneratorAccessor.NextId(),
                newTask.Id,
                $"[前加签] {request.Comment}"));
        }

        if (newTasks.Count > 0)
        {
            await _taskRepository.AddRangeAsync(newTasks, cancellationToken);
            await _assigneeChangeRepository.AddRangeAsync(changes, cancellationToken);
        }

        await RecordHistoryAsync(tenantId, instanceId, currentTask.NodeId, operatorUserId, "前加签", cancellationToken);
    }

    /// <summary>
    /// 后加签：当前任务正常审批 → 完成后引擎创建加签任务
    ///
    /// 实现方式：将加签信息记录到 AssigneeChange 表中，类型标记为 AddAfter。
    /// 当 FlowEngine 推进流程时，检查当前节点是否有待执行的后加签记录，
    /// 如果有则先创建加签任务，等加签任务全部完成后才继续推进。
    /// </summary>
    private async Task ExecuteAfterAddSignAsync(
        TenantId tenantId,
        long instanceId,
        ApprovalTask currentTask,
        long operatorUserId,
        ApprovalOperationRequest request,
        CancellationToken cancellationToken)
    {
        var changes = new List<ApprovalTaskAssigneeChange>();

        foreach (var assigneeValue in request.AdditionalAssigneeValues!)
        {
            changes.Add(new ApprovalTaskAssigneeChange(
                tenantId,
                instanceId,
                currentTask.NodeId,
                assigneeValue,
                AssigneeChangeType.AddAfter,
                operatorUserId,
                _idGeneratorAccessor.NextId(),
                null, // 任务尚未创建，在原任务完成后由引擎创建
                $"[后加签] {request.Comment}"));
        }

        if (changes.Count > 0)
        {
            await _assigneeChangeRepository.AddRangeAsync(changes, cancellationToken);
        }

        await RecordHistoryAsync(tenantId, instanceId, currentTask.NodeId, operatorUserId, "后加签", cancellationToken);
    }

    /// <summary>
    /// 记录加签历史事件
    /// </summary>
    private async Task RecordHistoryAsync(
        TenantId tenantId,
        long instanceId,
        string nodeId,
        long operatorUserId,
        string signTypeLabel,
        CancellationToken cancellationToken)
    {
        var historyEvent = new ApprovalHistoryEvent(
            tenantId,
            instanceId,
            ApprovalHistoryEventType.AssigneeAdded,
            null,
            nodeId,
            operatorUserId,
            _idGeneratorAccessor.NextId());
        await _historyRepository.AddAsync(historyEvent, cancellationToken);
    }
}
