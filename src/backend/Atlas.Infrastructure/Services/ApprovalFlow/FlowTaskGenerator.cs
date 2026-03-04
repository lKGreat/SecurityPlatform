using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.ApprovalFlow;

/// <summary>
/// 流程任务生成器（负责审批节点任务列表的构建，包含审批人解析、去重、缺失策略、自审策略和代理替换）
/// </summary>
public sealed class FlowTaskGenerator
{
    private readonly AssigneeResolver _assigneeResolver;
    private readonly DeduplicationService _deduplicationService;
    private readonly IApprovalUserQueryService _userQueryService;
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalAgentConfigRepository? _agentConfigRepository;
    private readonly IApprovalTimeoutReminderRepository? _timeoutReminderRepository;
    private readonly IBackgroundWorkQueue? _backgroundWorkQueue;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FlowTaskGenerator>? _logger;

    public FlowTaskGenerator(
        AssigneeResolver assigneeResolver,
        DeduplicationService deduplicationService,
        IApprovalUserQueryService userQueryService,
        IApprovalTaskRepository taskRepository,
        IIdGeneratorAccessor idGeneratorAccessor,
        IApprovalAgentConfigRepository? agentConfigRepository = null,
        IApprovalTimeoutReminderRepository? timeoutReminderRepository = null,
        IBackgroundWorkQueue? backgroundWorkQueue = null,
        TimeProvider? timeProvider = null,
        ILogger<FlowTaskGenerator>? logger = null)
    {
        _assigneeResolver = assigneeResolver;
        _deduplicationService = deduplicationService;
        _userQueryService = userQueryService;
        _taskRepository = taskRepository;
        _agentConfigRepository = agentConfigRepository;
        _timeoutReminderRepository = timeoutReminderRepository;
        _backgroundWorkQueue = backgroundWorkQueue;
        _idGeneratorAccessor = idGeneratorAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <summary>
    /// 为审批节点生成任务并持久化（含超时提醒和通知入队）
    /// </summary>
    public async Task GenerateTasksForNodeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        FlowNode node,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var context = executionContext ?? new FlowExecutionContext();
        var tasks = await ExpandTasksByAssigneeTypeAsync(
            tenantId,
            instance,
            definition,
            node,
            cancellationToken,
            context);

        if (tasks.Count > 0)
        {
            await _taskRepository.AddRangeAsync(tasks, cancellationToken);

            if (node.TimeoutEnabled && _timeoutReminderRepository != null)
            {
                await CreateTimeoutRemindersAsync(tenantId, instance, node, tasks, cancellationToken);
            }

            if (_backgroundWorkQueue != null)
            {
                var recipientUserIds = tasks
                    .Select(t => ExtractAssigneeUserId(t.AssigneeValue))
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                if (recipientUserIds.Count > 0)
                {
                    var capturedInstanceId = instance.Id;
                    _backgroundWorkQueue.Enqueue(async (sp, ct) =>
                    {
                        var notificationService = sp.GetService<IApprovalNotificationService>();
                        if (notificationService == null) return;

                        var instanceRepo = sp.GetRequiredService<IApprovalInstanceRepository>();
                        var inst = await instanceRepo.GetByIdAsync(tenantId, capturedInstanceId, ct);
                        if (inst != null)
                        {
                            await notificationService.NotifyAsync(
                                tenantId,
                                ApprovalNotificationEventType.TaskCreated,
                                inst,
                                null,
                                recipientUserIds,
                                ct);
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    /// 根据节点的分配策略解析用户并构建任务列表（不持久化）
    /// </summary>
    public async Task<List<ApprovalTask>> ExpandTasksByAssigneeTypeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        FlowNode node,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var tasks = new List<ApprovalTask>();
        var context = executionContext ?? new FlowExecutionContext();

        var assigneeType = node.AssigneeType;
        var assigneeValue = node.AssigneeValue ?? string.Empty;
        var approvalMode = node.ApprovalMode;
        var missingAssigneeStrategy = node.MissingAssigneeStrategy;

        var userIds = await _assigneeResolver.ResolveUserIdsAsync(
            tenantId,
            instance.InitiatorUserId,
            assigneeType,
            assigneeValue,
            instance.DataJson,
            cancellationToken);

        if (userIds.Count > 0)
        {
            userIds = (await _deduplicationService.ApplyDeduplicationAsync(
                tenantId,
                instance.Id,
                userIds,
                definition,
                node,
                cancellationToken)).ToList();
        }

        if (userIds.Count == 0)
        {
            switch (missingAssigneeStrategy)
            {
                case MissingAssigneeStrategy.NotAllowed:
                    throw new Core.Exceptions.BusinessException(
                        "MISSING_ASSIGNEE",
                        $"节点 {node.Id} 无法找到审批人，不允许发起流程");

                case MissingAssigneeStrategy.Skip:
                    return tasks;

                case MissingAssigneeStrategy.TransferToAdmin:
                    var adminUserIds = await _userQueryService.GetUserIdsByRoleCodeAsync(tenantId, "Admin", cancellationToken);
                    if (adminUserIds.Count > 0)
                    {
                        userIds.AddRange(adminUserIds);
                    }
                    else
                    {
                        return tasks;
                    }
                    break;
            }
        }

        var approveSelf = node.ApproveSelf ?? 0;
        if (approveSelf != 0 && userIds.Contains(instance.InitiatorUserId))
        {
            switch ((NodeApproveSelf)approveSelf)
            {
                case NodeApproveSelf.AutoSkip:
                    userIds.Remove(instance.InitiatorUserId);
                    if (userIds.Count == 0)
                    {
                        return tasks;
                    }
                    break;

                case NodeApproveSelf.TransferDirectSuperior:
                    userIds.Remove(instance.InitiatorUserId);
                    var directLeader = await _userQueryService.GetDirectLeaderUserIdAsync(
                        tenantId, instance.InitiatorUserId, cancellationToken);
                    if (directLeader.HasValue && !userIds.Contains(directLeader.Value))
                    {
                        userIds.Add(directLeader.Value);
                    }
                    break;

                case NodeApproveSelf.TransferDepartmentHead:
                    userIds.Remove(instance.InitiatorUserId);
                    var deptHead = await _userQueryService.GetDepartmentHeadUserIdAsync(
                        tenantId, instance.InitiatorUserId, cancellationToken);
                    if (deptHead.HasValue && !userIds.Contains(deptHead.Value))
                    {
                        userIds.Add(deptHead.Value);
                    }
                    break;
            }
        }

        userIds = await ApplyAgentReplacementAsync(tenantId, userIds, cancellationToken);

        if (context.DynamicAssignees.TryGetValue(node.Id, out var dynamicUserIds))
        {
            userIds = dynamicUserIds.Distinct().ToList();
        }

        int order = 1;
        foreach (var userId in userIds.Distinct())
        {
            var initialStatus = approvalMode == ApprovalMode.Sequential && order > 1
                ? ApprovalTaskStatus.Waiting
                : ApprovalTaskStatus.Pending;

            var task = new ApprovalTask(
                tenantId,
                instance.Id,
                node.Id,
                node.Label ?? "审批",
                AssigneeType.User,
                userId.ToString(),
                _idGeneratorAccessor.NextId(),
                order: order,
                initialStatus: initialStatus);

            if (approvalMode == ApprovalMode.Vote && node.Weight.HasValue)
            {
                task.SetWeight(node.Weight.Value);
            }

            tasks.Add(task);
            order++;
        }

        return tasks;
    }

    private async Task<List<long>> ApplyAgentReplacementAsync(
        TenantId tenantId,
        List<long> userIds,
        CancellationToken cancellationToken)
    {
        if (_agentConfigRepository == null || userIds.Count == 0) return userIds;

        var now = _timeProvider.GetUtcNow();
        var agentConfigMap = await _agentConfigRepository.GetActiveAgentsByUserIdsAsync(tenantId, userIds, now, cancellationToken);

        if (agentConfigMap.Count == 0) return userIds;

        var result = new List<long>(userIds.Count);
        foreach (var userId in userIds)
        {
            result.Add(agentConfigMap.TryGetValue(userId, out var agentConfig)
                ? agentConfig.AgentUserId
                : userId);
        }
        return result;
    }

    private async Task CreateTimeoutRemindersAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowNode node,
        List<ApprovalTask> tasks,
        CancellationToken cancellationToken)
    {
        if (!node.TimeoutEnabled || (node.TimeoutHours == null && node.TimeoutMinutes == null))
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var expectedCompleteTime = now
            .AddHours(node.TimeoutHours ?? 0)
            .AddMinutes(node.TimeoutMinutes ?? 0);

        var existingReminders = await _timeoutReminderRepository!.GetByInstanceAndNodeAsync(
            tenantId, instance.Id, node.Id, cancellationToken);
        var existingReminderTaskIds = existingReminders.Select(r => r.TaskId).ToHashSet();

        var reminders = new List<ApprovalTimeoutReminder>();
        foreach (var task in tasks)
        {
            if (existingReminderTaskIds.Contains(task.Id)) continue;

            var recipientUserId = ExtractAssigneeUserId(task.AssigneeValue);
            if (!recipientUserId.HasValue) continue;

            reminders.Add(new ApprovalTimeoutReminder(
                tenantId,
                instance.Id,
                task.Id,
                node.Id,
                Domain.Approval.Enums.ReminderType.NodeTimeout,
                recipientUserId.Value,
                expectedCompleteTime,
                _idGeneratorAccessor.NextId()));
        }

        if (reminders.Count > 0)
        {
            await _timeoutReminderRepository.AddRangeAsync(reminders, cancellationToken);
        }
    }

    internal static long? ExtractAssigneeUserId(string assigneeValue)
    {
        if (string.IsNullOrEmpty(assigneeValue)) return null;

        if (long.TryParse(assigneeValue, out var userId)) return userId;

        var parts = assigneeValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && long.TryParse(parts[0].Trim(), out var firstUserId))
        {
            return firstUserId;
        }

        return null;
    }
}
