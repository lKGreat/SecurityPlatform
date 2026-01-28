using AutoMapper;
using System.Text.Json;
using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Models;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Exceptions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services;

/// <summary>
/// 审批流运行时命令服务实现（核心引擎逻辑）
/// </summary>
public sealed class ApprovalRuntimeCommandService : IApprovalRuntimeCommandService
{
    private readonly IApprovalFlowRepository _flowRepository;
    private readonly IApprovalInstanceRepository _instanceRepository;
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalHistoryRepository _historyRepository;
    private readonly IApprovalDepartmentLeaderRepository _deptLeaderRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly IMapper _mapper;

    public ApprovalRuntimeCommandService(
        IApprovalFlowRepository flowRepository,
        IApprovalInstanceRepository instanceRepository,
        IApprovalTaskRepository taskRepository,
        IApprovalHistoryRepository historyRepository,
        IApprovalDepartmentLeaderRepository deptLeaderRepository,
        IIdGenerator idGenerator,
        IMapper mapper)
    {
        _flowRepository = flowRepository;
        _instanceRepository = instanceRepository;
        _taskRepository = taskRepository;
        _historyRepository = historyRepository;
        _deptLeaderRepository = deptLeaderRepository;
        _idGenerator = idGenerator;
        _mapper = mapper;
    }

    public async Task<ApprovalInstanceResponse> StartAsync(
        TenantId tenantId,
        ApprovalStartRequest request,
        long initiatorUserId,
        CancellationToken cancellationToken)
    {
        // 获取已发布的流程定义
        var flowDef = await _flowRepository.GetByIdAsync(tenantId, request.DefinitionId, cancellationToken);
        if (flowDef == null)
        {
            throw new BusinessException("FLOW_NOT_FOUND", "审批流定义不存在");
        }

        if (flowDef.Status != ApprovalFlowStatus.Published)
        {
            throw new BusinessException("FLOW_NOT_PUBLISHED", "流程定义未发布");
        }

        // 创建实例
        var instance = new ApprovalProcessInstance(
            tenantId,
            request.DefinitionId,
            request.BusinessKey,
            initiatorUserId,
            _idGenerator.NextId(),
            request.DataJson);
        await _instanceRepository.AddAsync(instance, cancellationToken);

        // 记录实例启动事件
        var startEvent = new ApprovalHistoryEvent(
            tenantId,
            instance.Id,
            ApprovalHistoryEventType.InstanceStarted,
            null,
            null,
            initiatorUserId,
            _idGenerator.NextId());
        await _historyRepository.AddAsync(startEvent, cancellationToken);

        // 解析流程定义，生成第一批待审批任务
        await GenerateInitialTasks(tenantId, instance, flowDef, cancellationToken);

        return _mapper.Map<ApprovalInstanceResponse>(instance);
    }

    public async Task ApproveTaskAsync(
        TenantId tenantId,
        long taskId,
        long approverUserId,
        string? comment,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(tenantId, taskId, cancellationToken);
        if (task == null)
        {
            throw new BusinessException("TASK_NOT_FOUND", "审批任务不存在");
        }

        if (task.Status != ApprovalTaskStatus.Pending)
        {
            throw new BusinessException("TASK_NOT_PENDING", "任务不是待审批状态");
        }

        var instance = await _instanceRepository.GetByIdAsync(tenantId, task.InstanceId, cancellationToken);
        if (instance == null || instance.Status != ApprovalInstanceStatus.Running)
        {
            throw new BusinessException("INSTANCE_NOT_RUNNING", "流程实例不在运行状态");
        }

        // 记录任务同意事件
        var approveEvent = new ApprovalHistoryEvent(
            tenantId,
            instance.Id,
            ApprovalHistoryEventType.TaskApproved,
            task.NodeId,
            null,
            approverUserId,
            _idGenerator.NextId());
        await _historyRepository.AddAsync(approveEvent, cancellationToken);

        // 标记任务为同意
        task.Approve(approverUserId, comment, DateTimeOffset.UtcNow);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        // 推进流程（简化版：如果所有任务都同意，则进入下一节点或完成）
        await AdvanceFlow(tenantId, instance, task, cancellationToken);
    }

    public async Task RejectTaskAsync(
        TenantId tenantId,
        long taskId,
        long approverUserId,
        string? comment,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(tenantId, taskId, cancellationToken);
        if (task == null)
        {
            throw new BusinessException("TASK_NOT_FOUND", "审批任务不存在");
        }

        if (task.Status != ApprovalTaskStatus.Pending)
        {
            throw new BusinessException("TASK_NOT_PENDING", "任务不是待审批状态");
        }

        var instance = await _instanceRepository.GetByIdAsync(tenantId, task.InstanceId, cancellationToken);
        if (instance == null || instance.Status != ApprovalInstanceStatus.Running)
        {
            throw new BusinessException("INSTANCE_NOT_RUNNING", "流程实例不在运行状态");
        }

        // 记录任务驳回事件
        var rejectEvent = new ApprovalHistoryEvent(
            tenantId,
            instance.Id,
            ApprovalHistoryEventType.TaskRejected,
            task.NodeId,
            null,
            approverUserId,
            _idGenerator.NextId());
        await _historyRepository.AddAsync(rejectEvent, cancellationToken);

        // 标记任务为驳回
        task.Reject(approverUserId, comment, DateTimeOffset.UtcNow);
        await _taskRepository.UpdateAsync(task, cancellationToken);

        // 驳回后，流程实例变为驳回状态，取消所有待审批任务
        instance.MarkRejected(DateTimeOffset.UtcNow);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);

        var pendingTasks = await _taskRepository.GetByInstanceAndStatusAsync(
            tenantId,
            instance.Id,
            ApprovalTaskStatus.Pending,
            cancellationToken);
        foreach (var pendingTask in pendingTasks)
        {
            pendingTask.Cancel();
            await _taskRepository.UpdateAsync(pendingTask, cancellationToken);
        }

        // 记录流程驳回事件
        var instanceRejectEvent = new ApprovalHistoryEvent(
            tenantId,
            instance.Id,
            ApprovalHistoryEventType.InstanceRejected,
            null,
            null,
            approverUserId,
            _idGenerator.NextId());
        await _historyRepository.AddAsync(instanceRejectEvent, cancellationToken);
    }

    public async Task CancelInstanceAsync(
        TenantId tenantId,
        long instanceId,
        long cancelledByUserId,
        CancellationToken cancellationToken)
    {
        var instance = await _instanceRepository.GetByIdAsync(tenantId, instanceId, cancellationToken);
        if (instance == null)
        {
            throw new BusinessException("INSTANCE_NOT_FOUND", "流程实例不存在");
        }

        if (instance.Status != ApprovalInstanceStatus.Running)
        {
            throw new BusinessException("INSTANCE_NOT_RUNNING", "流程实例不在运行状态");
        }

        instance.MarkCanceled(DateTimeOffset.UtcNow);
        await _instanceRepository.UpdateAsync(instance, cancellationToken);

        // 取消所有待审批任务
        var pendingTasks = await _taskRepository.GetByInstanceAndStatusAsync(
            tenantId,
            instance.Id,
            ApprovalTaskStatus.Pending,
            cancellationToken);
        foreach (var task in pendingTasks)
        {
            task.Cancel();
            await _taskRepository.UpdateAsync(task, cancellationToken);
        }

        // 记录流程取消事件
        var cancelEvent = new ApprovalHistoryEvent(
            tenantId,
            instance.Id,
            ApprovalHistoryEventType.InstanceCanceled,
            null,
            null,
            cancelledByUserId,
            _idGenerator.NextId());
        await _historyRepository.AddAsync(cancelEvent, cancellationToken);
    }

    /// <summary>
    /// 生成初始待审批任务（从流程定义的第一个审批节点开始）
    /// </summary>
    private async Task GenerateInitialTasks(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        ApprovalFlowDefinition flowDef,
        CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(flowDef.DefinitionJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("nodes", out var nodesElement))
                return;

            // 找到第一个 approve 或 condition 节点
            var firstApprovalNode = nodesElement.EnumerateArray()
                .FirstOrDefault(n =>
                {
                    if (n.TryGetProperty("type", out var typeProp))
                    {
                        var nodeType = typeProp.GetString();
                        return nodeType == "approve" || nodeType == "condition";
                    }
                    return false;
                });

            if (firstApprovalNode.ValueKind == JsonValueKind.Undefined)
                return;

            if (!firstApprovalNode.TryGetProperty("id", out var nodeIdProp))
                return;

            var nodeId = nodeIdProp.GetString();
            if (string.IsNullOrEmpty(nodeId))
                return;

            // 提取节点配置
            var assigneeType = AssigneeType.User;
            var assigneeValue = string.Empty;
            var nodeTitle = "审批";

            if (firstApprovalNode.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("assigneeType", out var assigneeProp))
                    Enum.TryParse<AssigneeType>(assigneeProp.GetString(), out assigneeType);

                if (dataElement.TryGetProperty("assigneeValue", out var valueProp))
                    assigneeValue = valueProp.GetString() ?? string.Empty;

                if (dataElement.TryGetProperty("label", out var labelProp))
                    nodeTitle = labelProp.GetString() ?? "审批";
            }

            // 根据分配策略扩展任务
            var tasks = await ExpandTasksByAssigneeType(
                tenantId,
                instance.Id,
                nodeId,
                nodeTitle,
                assigneeType,
                assigneeValue,
                cancellationToken);

            if (tasks.Count > 0)
            {
                await _taskRepository.AddRangeAsync(tasks, cancellationToken);
                foreach (var task in tasks)
                {
                    var taskCreatedEvent = new ApprovalHistoryEvent(
                        tenantId,
                        instance.Id,
                        ApprovalHistoryEventType.TaskCreated,
                        null,
                        nodeId,
                        null,
                        _idGenerator.NextId());
                    await _historyRepository.AddAsync(taskCreatedEvent, cancellationToken);
                }
            }
        }
        catch (JsonException)
        {
            // 如果 JSON 解析失败，直接返回
        }
    }

    /// <summary>
    /// 根据分配策略扩展任务（一个策略可能对应多个审批人）
    /// </summary>
    private async Task<List<ApprovalTask>> ExpandTasksByAssigneeType(
        TenantId tenantId,
        long instanceId,
        string nodeId,
        string nodeTitle,
        AssigneeType assigneeType,
        string assigneeValue,
        CancellationToken cancellationToken)
    {
        var tasks = new List<ApprovalTask>();

        switch (assigneeType)
        {
            case AssigneeType.User:
                // 指定用户
                tasks.Add(new ApprovalTask(
                    tenantId,
                    instanceId,
                    nodeId,
                    nodeTitle,
                    AssigneeType.User,
                    assigneeValue,
                    _idGenerator.NextId()));
                break;

            case AssigneeType.Role:
                // 按角色（简化版：保存角色码，前端或后续处理时展开）
                tasks.Add(new ApprovalTask(
                    tenantId,
                    instanceId,
                    nodeId,
                    nodeTitle,
                    AssigneeType.Role,
                    assigneeValue,
                    _idGenerator.NextId()));
                break;

            case AssigneeType.DepartmentLeader:
                // 部门负责人
                if (long.TryParse(assigneeValue, out var deptId))
                {
                    var leaderId = await _deptLeaderRepository.GetLeaderUserIdAsync(tenantId, deptId, cancellationToken);
                    if (leaderId.HasValue)
                    {
                        tasks.Add(new ApprovalTask(
                            tenantId,
                            instanceId,
                            nodeId,
                            nodeTitle,
                            AssigneeType.User,
                            leaderId.Value.ToString(),
                            _idGenerator.NextId()));
                    }
                }
                break;
        }

        return tasks;
    }

    /// <summary>
    /// 推进流程（简化版：所有任务同意则完成）
    /// </summary>
    private async Task AdvanceFlow(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        ApprovalTask currentTask,
        CancellationToken cancellationToken)
    {
        // 检查同节点是否所有任务都已决策
        var nodeTasksWithoutDecision = await _taskRepository.GetByInstanceAndStatusAsync(
            tenantId,
            instance.Id,
            ApprovalTaskStatus.Pending,
            cancellationToken);

        if (nodeTasksWithoutDecision.Count == 0)
        {
            // 所有任务都已完成（同意或驳回）
            // 对于 MVP，简化为：所有任务同意 → 流程完成
            var allApproved = true;
            var allInstanceTasks = (await _taskRepository.GetPagedByInstanceAsync(
                tenantId,
                instance.Id,
                1,
                1000,
                cancellationToken: cancellationToken)).Items;

            foreach (var task in allInstanceTasks)
            {
                if (task.Status != ApprovalTaskStatus.Approved)
                {
                    allApproved = false;
                    break;
                }
            }

            if (allApproved)
            {
                instance.MarkCompleted(DateTimeOffset.UtcNow);
                await _instanceRepository.UpdateAsync(instance, cancellationToken);

                var completedEvent = new ApprovalHistoryEvent(
                    tenantId,
                    instance.Id,
                    ApprovalHistoryEventType.InstanceCompleted,
                    null,
                    null,
                    null,
                    _idGenerator.NextId());
                await _historyRepository.AddAsync(completedEvent, cancellationToken);
            }
        }
    }
}
