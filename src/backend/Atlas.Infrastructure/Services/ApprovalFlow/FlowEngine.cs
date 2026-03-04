using Atlas.Application.Approval.Abstractions;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParallelTokenStatus = Atlas.Domain.Approval.Entities.ParallelTokenStatus;

namespace Atlas.Infrastructure.Services.ApprovalFlow;

/// <summary>
/// 流程执行上下文（用于在节点链路中传递运行时数据）
/// </summary>
public sealed class FlowExecutionContext
{
    /// <summary>运行时动态分配审批人（nodeId -> userIds）</summary>
    public Dictionary<string, List<long>> DynamicAssignees { get; } = new();

    /// <summary>指定条件节点选择（强制走某个条件分支的 nodeKey）</summary>
    public string? SpecifyConditionNodeKey { get; set; }

    /// <summary>驳回源节点ID（用于 ReApproveStrategy 判断）</summary>
    public string? RejectSourceNodeId { get; set; }

    /// <summary>当前是否处于驳回重新审批流程</summary>
    public bool IsReApproveFlow { get; set; }

    /// <summary>重新审批策略</summary>
    public ReApproveStrategy? ReApproveStrategy { get; set; }
}

/// <summary>
/// 流程推进引擎（支持多节点、条件分支、会签/或签、并行网关、抄送节点）
/// </summary>
public sealed class FlowEngine
{
    private readonly IApprovalTaskRepository _taskRepository;
    private readonly IApprovalNodeExecutionRepository _nodeExecutionRepository;
    private readonly IApprovalParallelTokenRepository _parallelTokenRepository;
    private readonly IApprovalCopyRecordRepository _copyRecordRepository;
    private readonly ConditionEvaluator _conditionEvaluator;
    private readonly AssigneeResolver _assigneeResolver;
    private readonly IApprovalNotificationService? _notificationService;
    private readonly IApprovalTimeoutReminderRepository? _timeoutReminderRepository;
    private readonly ExternalCallbackService? _callbackService;
    private readonly IApprovalAiHandler? _aiHandler;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;
    private readonly IBackgroundWorkQueue? _backgroundWorkQueue;
    private readonly IServiceProvider? _serviceProvider;
    private readonly FlowGatewayHandler _gatewayHandler;
    private readonly FlowTaskGenerator _taskGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FlowEngine>? _logger;

    public FlowEngine(
        IApprovalTaskRepository taskRepository,
        IApprovalNodeExecutionRepository nodeExecutionRepository,
        IApprovalParallelTokenRepository parallelTokenRepository,
        IApprovalCopyRecordRepository copyRecordRepository,
        ConditionEvaluator conditionEvaluator,
        AssigneeResolver assigneeResolver,
        IApprovalUserQueryService userQueryService,
        DeduplicationService deduplicationService,
        IIdGeneratorAccessor idGeneratorAccessor,
        IApprovalNotificationService? notificationService = null,
        IApprovalTimeoutReminderRepository? timeoutReminderRepository = null,
        ExternalCallbackService? callbackService = null,
        IApprovalAiHandler? aiHandler = null,
        IBackgroundWorkQueue? backgroundWorkQueue = null,
        IServiceProvider? serviceProvider = null,
        FlowGatewayHandler? gatewayHandler = null,
        FlowTaskGenerator? taskGenerator = null,
        TimeProvider? timeProvider = null,
        ILogger<FlowEngine>? logger = null)
    {
        _taskRepository = taskRepository;
        _nodeExecutionRepository = nodeExecutionRepository;
        _parallelTokenRepository = parallelTokenRepository;
        _copyRecordRepository = copyRecordRepository;
        _conditionEvaluator = conditionEvaluator;
        _assigneeResolver = assigneeResolver;
        _notificationService = notificationService;
        _timeoutReminderRepository = timeoutReminderRepository;
        _callbackService = callbackService;
        _aiHandler = aiHandler;
        _idGeneratorAccessor = idGeneratorAccessor;
        _backgroundWorkQueue = backgroundWorkQueue;
        _serviceProvider = serviceProvider;
        _gatewayHandler = gatewayHandler ?? new FlowGatewayHandler(parallelTokenRepository, idGeneratorAccessor);
        _taskGenerator = taskGenerator ?? new FlowTaskGenerator(
            assigneeResolver,
            deduplicationService,
            userQueryService,
            taskRepository,
            idGeneratorAccessor,
            agentConfigRepository: null,
            timeoutReminderRepository,
            backgroundWorkQueue,
            timeProvider);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    /// <summary>
    /// 跳转到指定节点（取消当前所有任务，在目标节点创建新任务）
    /// </summary>
    public async Task JumpToNodeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        string targetNodeId,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var targetNode = definition.GetNodeById(targetNodeId);
        if (targetNode == null)
        {
            throw new Core.Exceptions.BusinessException("NODE_NOT_FOUND", $"目标节点 {targetNodeId} 不存在");
        }

        // 记录跳转前的当前节点（用于可能的恢复）
        // instance.CurrentNodeId 已经在外部被更新前记录了历史，这里不需要额外操作

        // 直接处理目标节点
        await ProcessNextNodeAsync(tenantId, instance, definition, targetNodeId, cancellationToken, executionContext);
    }

    /// <summary>
    /// 处理驳回路由逻辑（根据节点的 RejectStrategy 决定驳回行为）
    /// </summary>
    /// <returns>true 表示驳回已路由到目标节点（流程继续），false 表示应终止流程</returns>
    public async Task<bool> HandleRejectionAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        string currentNodeId,
        string? specifiedTargetNodeId,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var context = executionContext ?? new FlowExecutionContext();
        var currentNode = definition.GetNodeById(currentNodeId);
        if (currentNode == null) return false;

        var strategy = currentNode.RejectStrategy ?? RejectStrategy.ToPrevious;

        // 如果指定了目标节点（前端传入），优先使用
        if (!string.IsNullOrEmpty(specifiedTargetNodeId))
        {
            strategy = RejectStrategy.ToAnyNode;
        }

        // 在并行/包容分支内驳回时，先强制终止兄弟分支的活跃任务
        await CancelSiblingBranchTasksAsync(tenantId, instance, definition, currentNodeId, cancellationToken);

        switch (strategy)
        {
            case RejectStrategy.ToInitiator:
                // 跳转到发起人节点（开始节点的下一个审批节点）
                var startNode = definition.GetStartNode();
                if (startNode != null)
                {
                    var firstApprovalNodeId = FindFirstApprovalNodeAfterStart(definition, startNode.Id);
                    if (firstApprovalNodeId != null)
                    {
                        SetupReApproveContext(context, currentNode);
                        await JumpToNodeAsync(tenantId, instance, definition, firstApprovalNodeId, cancellationToken, context);
                        return true;
                    }
                }
                return false;

            case RejectStrategy.ToPrevious:
                // 退回上一个审批节点
                var prevNodeId = definition.FindParentApprovalNodeId(currentNodeId);
                if (prevNodeId != null)
                {
                    SetupReApproveContext(context, currentNode);
                    await JumpToNodeAsync(tenantId, instance, definition, prevNodeId, cancellationToken, context);
                    return true;
                }
                return false;

            case RejectStrategy.ToAnyNode:
                // 退回到指定节点
                var targetId = specifiedTargetNodeId;
                if (!string.IsNullOrEmpty(targetId))
                {
                    SetupReApproveContext(context, currentNode);
                    await JumpToNodeAsync(tenantId, instance, definition, targetId, cancellationToken, context);
                    return true;
                }
                return false;

            case RejectStrategy.TerminateApproval:
                // 终止流程
                return false;

            case RejectStrategy.ToParentNode:
                // 退回到模型父节点（上一个审批节点）
                var parentApprovalNodeId = definition.FindParentApprovalNodeId(currentNodeId);
                if (parentApprovalNodeId != null)
                {
                    SetupReApproveContext(context, currentNode);
                    await JumpToNodeAsync(tenantId, instance, definition, parentApprovalNodeId, cancellationToken, context);
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// 设置重新审批上下文（用于后续 AdvanceFlowAsync 判断 ReApproveStrategy）
    /// </summary>
    private static void SetupReApproveContext(FlowExecutionContext context, FlowNode rejectSourceNode)
    {
        context.RejectSourceNodeId = rejectSourceNode.Id;
        context.IsReApproveFlow = true;
        context.ReApproveStrategy = rejectSourceNode.ReApproveStrategy;
    }

    /// <summary>
    /// 查找 Start 节点之后的第一个审批节点
    /// </summary>
    private static string? FindFirstApprovalNodeAfterStart(FlowDefinition definition, string startNodeId)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(startNodeId);

        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            if (!visited.Add(nodeId)) continue;

            var outgoingEdges = definition.GetOutgoingEdges(nodeId);
            foreach (var edge in outgoingEdges)
            {
                var nextNode = definition.GetNodeById(edge.Target);
                if (nextNode == null) continue;

                if (nextNode.Type == "approve")
                    return nextNode.Id;

                queue.Enqueue(edge.Target);
            }
        }

        return null;
    }

    /// <summary>
    /// 取消并行/包容分支内兄弟分支的活跃任务
    /// </summary>
    private async Task CancelSiblingBranchTasksAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        string currentNodeId,
        CancellationToken cancellationToken)
    {
        var siblingNodeIds = definition.GetSiblingBranchNodeIds(currentNodeId);
        if (siblingNodeIds.Count == 0) return;

        var siblingTasks = await _taskRepository.GetByInstanceAndNodesAsync(
            tenantId, instance.Id, siblingNodeIds, cancellationToken);

        var tasksToCancel = siblingTasks
            .Where(t => t.Status == ApprovalTaskStatus.Pending || t.Status == ApprovalTaskStatus.Waiting)
            .ToList();

        if (tasksToCancel.Count > 0)
        {
            foreach (var task in tasksToCancel)
            {
                task.Cancel();
            }
            await _taskRepository.UpdateRangeAsync(tasksToCancel, cancellationToken);
        }
    }

    /// <summary>
    /// 推进流程到下一个节点
    /// </summary>
    public async Task AdvanceFlowAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        string currentNodeId,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var context = executionContext ?? new FlowExecutionContext();

        // 获取当前节点
        var currentNode = definition.GetNodeById(currentNodeId);
        if (currentNode == null)
        {
            return;
        }

        // 检查并行汇聚网关：如果是并行汇聚网关，需要等待所有分支完成
        if (definition.IsParallelJoinGateway(currentNodeId))
        {
            var canProceed = await _gatewayHandler.CheckParallelJoinCompletionAsync(tenantId, instance.Id, currentNodeId, definition, cancellationToken);
            if (!canProceed)
            {
                // 等待所有分支完成
                return;
            }
        }

        // 检查当前节点是否已完成（会签/或签逻辑）
        if (currentNode.Type == "approve")
        {
            var nodeTasks = await _taskRepository.GetByInstanceAndNodeAsync(tenantId, instance.Id, currentNodeId, cancellationToken);
            
            // 顺序会签：检查是否需要激活下一个任务
            if (currentNode.ApprovalMode == ApprovalMode.Sequential)
            {
                await ActivateNextSequentialTaskAsync(tenantId, instance.Id, currentNodeId, nodeTasks, cancellationToken);
            }

            var isCompleted = CheckNodeCompletion(currentNode, nodeTasks);

            if (!isCompleted)
            {
                // 节点未完成，等待更多审批
                return;
            }
        }

        // 标记当前节点为已完成
        var nodeExecution = await _nodeExecutionRepository.GetByInstanceAndNodeAsync(tenantId, instance.Id, currentNodeId, cancellationToken);
        if (nodeExecution != null)
        {
            nodeExecution.MarkCompleted(DateTimeOffset.UtcNow);
            await _nodeExecutionRepository.UpdateAsync(nodeExecution, cancellationToken);
        }

        // ── 驳回重新审批策略检查 ──
        // 如果当前处于驳回重审流程且策略为 BackToRejectNode，
        // 任务完成后跳转回驳回源节点继续执行，而非沿正常流向推进
        var ctx = context;
        if (ctx is { IsReApproveFlow: true, ReApproveStrategy: ReApproveStrategy.BackToRejectNode }
            && !string.IsNullOrEmpty(ctx.RejectSourceNodeId))
        {
            var rejectSourceNodeId = ctx.RejectSourceNodeId;

            // 重置重审上下文，避免无限循环
            ctx.IsReApproveFlow = false;
            ctx.ReApproveStrategy = null;
            ctx.RejectSourceNodeId = null;

            // 跳转回驳回源节点，从该节点继续正常推进
            await ProcessNextNodeAsync(tenantId, instance, definition, rejectSourceNodeId, cancellationToken, context);
            return;
        }

        // 如果是 Continue 策略或非重审流程，清除上下文后继续正常推进
        if (ctx is { IsReApproveFlow: true })
        {
            ctx.IsReApproveFlow = false;
            ctx.ReApproveStrategy = null;
            ctx.RejectSourceNodeId = null;
        }

        // 处理并行汇聚：标记当前分支的token为已完成
        if (definition.IsParallelJoinGateway(currentNodeId))
        {
            await _gatewayHandler.MarkParallelBranchCompletedAsync(tenantId, instance.Id, currentNodeId, definition, cancellationToken);
        }

        // 获取当前节点的所有出边
        var outgoingEdges = definition.GetOutgoingEdges(currentNodeId);

        if (outgoingEdges.Count == 0)
        {
            // 没有出边，流程结束
            instance.MarkCompleted(DateTimeOffset.UtcNow);
            instance.SetCurrentNode(null);
            
            // 触发流程完成回调（后台队列，失败不影响主流程）
            EnqueueCallback(tenantId, CallbackEventType.InstanceCompleted, instance.Id, null, currentNodeId);
            
            return;
        }

        // 处理出边，决定下一个节点
        var nextNodeIds = await EvaluateNextNodesAsync(tenantId, instance, definition, currentNodeId, outgoingEdges, cancellationToken);

        if (nextNodeIds.Count == 0)
        {
            // 没有符合条件的下一个节点，流程结束
            instance.MarkCompleted(DateTimeOffset.UtcNow);
            instance.SetCurrentNode(null);
            
            // 触发流程完成回调（后台队列，失败不影响主流程）
            EnqueueCallback(tenantId, CallbackEventType.InstanceCompleted, instance.Id, null, currentNodeId);
            
            return;
        }

        // 处理并行分支网关：创建token并推进所有分支
        if (definition.IsParallelSplitGateway(currentNodeId))
        {
            await _gatewayHandler.HandleParallelSplitAsync(
                tenantId,
                instance,
                currentNodeId,
                nextNodeIds,
                definition,
                ProcessNextNodeAsync,
                cancellationToken,
                context);
            return;
        }

        // 处理包容分支网关：创建token并推进满足条件的分支
        if (definition.IsInclusiveSplitGateway(currentNodeId))
        {
            await _gatewayHandler.HandleInclusiveSplitAsync(
                tenantId,
                instance,
                currentNodeId,
                nextNodeIds,
                definition,
                ProcessNextNodeAsync,
                cancellationToken,
                context);
            return;
        }

        // 为每个下一个节点生成任务
        foreach (var nextNodeId in nextNodeIds)
        {
            await ProcessNextNodeAsync(tenantId, instance, definition, nextNodeId, cancellationToken, context);
        }
    }

    /// <summary>
    /// 处理下一个节点
    /// </summary>
    private async Task ProcessNextNodeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        string nextNodeId,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var context = executionContext ?? new FlowExecutionContext();
        var nextNode = definition.GetNodeById(nextNodeId);
        if (nextNode == null)
        {
            return;
        }

        if (nextNode.Type == "end")
        {
            // 结束节点，流程完成
            instance.MarkCompleted(DateTimeOffset.UtcNow);
            instance.SetCurrentNode(null);
            
            // 触发流程完成回调（后台队列，失败不影响主流程）
            EnqueueCallback(tenantId, CallbackEventType.InstanceCompleted, instance.Id, null, nextNodeId);
            
            return;
        }

        if (nextNode.Type == "approve")
        {
            // AI 审批处理
            if (nextNode.CallAi && _aiHandler != null)
            {
                var aiNodeContext = new AiNodeContext
                {
                    NodeId = nextNode.Id,
                    NodeName = nextNode.Label,
                    NodeType = nextNode.Type,
                    AiConfig = nextNode.AiConfig,
                    TriggerType = nextNode.TriggerType
                };
                var aiResult = await _aiHandler.HandleAsync(tenantId, instance, aiNodeContext, cancellationToken);
                
                // 记录节点执行（AI开始）
                var execution = new ApprovalNodeExecution(
                    tenantId,
                    instance.Id,
                    nextNodeId,
                    ApprovalNodeExecutionStatus.Running,
                    _idGeneratorAccessor.NextId());
                await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
                instance.SetCurrentNode(nextNodeId);

                if (aiResult.Approved)
                {
                    // AI 自动通过
                    execution.MarkCompleted(DateTimeOffset.UtcNow);
                    await _nodeExecutionRepository.UpdateAsync(execution, cancellationToken);
                    
                    // 记录 AI 审批历史（模拟一个系统用户或 AI 用户）
                    // ...

                    // 继续推进
                    await AdvanceFlowAsync(tenantId, instance, definition, nextNodeId, cancellationToken, context);
                    return;
                }
                else if (!aiResult.NeedManualReview)
                {
                    // AI 自动拒绝
                    // ...
                    // 结束流程
                    return;
                }
                
                // 如果 AI 无法决定或需要转人工，则继续执行下面的人工审批逻辑
            }

            // 审批节点，生成任务
            await _taskGenerator.GenerateTasksForNodeAsync(tenantId, instance, definition, nextNode, cancellationToken, context);

            // 创建节点执行记录
            var executionManual = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Running,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(executionManual, cancellationToken);

            instance.SetCurrentNode(nextNodeId);
        }
        else if (nextNode.Type == "condition" || nextNode.Type == "externalCondition")
        {
            // 条件节点（内部或外部），直接推进（条件已在 EvaluateNextNodesAsync 中评估）
            var execution = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Running,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
            instance.SetCurrentNode(nextNodeId);
            await AdvanceFlowAsync(tenantId, instance, definition, nextNodeId, cancellationToken, context);
        }
        else if (nextNode.Type == "copy")
        {
            // 抄送节点：生成抄送记录（不阻塞流程）
            await GenerateCopyRecordsForNodeAsync(tenantId, instance, nextNode, cancellationToken);

            // 创建节点执行记录并标记为已完成
            var execution = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Completed,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(execution, cancellationToken);

            // 抄送节点不阻塞流程，继续推进
            var outgoingEdges = definition.GetOutgoingEdges(nextNodeId);
            if (outgoingEdges.Count > 0)
            {
                var nextAfterCopy = await EvaluateNextNodesAsync(tenantId, instance, definition, nextNodeId, outgoingEdges, cancellationToken);
                foreach (var nodeId in nextAfterCopy)
                {
                    await ProcessNextNodeAsync(tenantId, instance, definition, nodeId, cancellationToken, context);
                }
            }
        }
        else if (nextNode.Type == "exclusiveGateway" || nextNode.Type == "parallelGateway" || nextNode.Type == "inclusiveGateway")
        {
            // 网关节点：直接推进
            var execution = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Running,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
            instance.SetCurrentNode(nextNodeId);
            await AdvanceFlowAsync(tenantId, instance, definition, nextNodeId, cancellationToken, context);
        }
        else if (nextNode.Type == "routeGateway")
        {
            // 路由网关：直接跳转到目标节点
            var targetNodeId = definition.GetRouteTarget(nextNodeId);
            if (!string.IsNullOrEmpty(targetNodeId))
            {
                // 记录路由节点执行
                var execution = new ApprovalNodeExecution(
                    tenantId,
                    instance.Id,
                    nextNodeId,
                    ApprovalNodeExecutionStatus.Completed,
                    _idGeneratorAccessor.NextId());
                await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
                
                // 递归处理目标节点
                await ProcessNextNodeAsync(tenantId, instance, definition, targetNodeId, cancellationToken, context);
            }
        }
        else if (nextNode.Type == "callProcess")
        {
            // 子流程节点
            var execution = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Running,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
            instance.SetCurrentNode(nextNodeId);
            
            await HandleSubProcessAsync(tenantId, instance, definition, nextNode, cancellationToken);
        }
        else if (nextNode.Type == "timer")
        {
            // 定时器节点：创建 TimerJob 记录，到期后由后台 Job 推进
            var execution = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Running,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
            instance.SetCurrentNode(nextNodeId);
            
            await HandleTimerNodeAsync(tenantId, instance, nextNode, cancellationToken);
        }
        else if (nextNode.Type == "trigger")
        {
            // 触发器节点：创建 TriggerJob 记录
            var execution = new ApprovalNodeExecution(
                tenantId,
                instance.Id,
                nextNodeId,
                ApprovalNodeExecutionStatus.Running,
                _idGeneratorAccessor.NextId());
            await _nodeExecutionRepository.AddAsync(execution, cancellationToken);
            instance.SetCurrentNode(nextNodeId);
            
            await HandleTriggerNodeAsync(tenantId, instance, definition, nextNode, cancellationToken);
        }
    }

    /// <summary>
    /// 检查节点是否已完成（根据会签/或签模式）
    /// </summary>
    private static bool CheckNodeCompletion(FlowNode node, IReadOnlyList<ApprovalTask> tasks)
    {
        if (tasks.Count == 0)
        {
            return false;
        }

        var pendingTasks = tasks.Where(t => t.Status == ApprovalTaskStatus.Pending).ToList();
        var approvedTasks = tasks.Where(t => t.Status == ApprovalTaskStatus.Approved).ToList();
        var rejectedTasks = tasks.Where(t => t.Status == ApprovalTaskStatus.Rejected).ToList();

        // 如果有任何任务被驳回，节点失败
        if (rejectedTasks.Count > 0)
        {
            return false; // 需要外部处理驳回逻辑
        }

        switch (node.ApprovalMode)
        {
            case ApprovalMode.All:
                // 会签：所有任务必须同意
                return pendingTasks.Count == 0 && approvedTasks.Count == tasks.Count;

            case ApprovalMode.Any:
                // 或签：任一任务同意即可
                return approvedTasks.Count > 0;

            case ApprovalMode.Sequential:
                // 顺序会签：按顺序审批，当前激活的任务完成即可
                // 检查是否有等待中的任务（说明还有未完成的任务）
                var waitingTasks = tasks.Where(t => t.Status == ApprovalTaskStatus.Waiting).ToList();
                // 如果没有等待任务且所有已激活的任务都已完成，则节点完成
                return waitingTasks.Count == 0 && pendingTasks.Count == 0 && approvedTasks.Count > 0;

            case ApprovalMode.Vote:
                // 票签：按权重投票
                var totalWeight = tasks.Sum(t => t.Weight ?? 1);
                if (totalWeight == 0) return true; // 避免除以零
                var approvedWeight = approvedTasks.Sum(t => t.Weight ?? 1);
                var passRate = node.VotePassRate ?? 50; // 默认50%通过率
                return (approvedWeight * 100 / totalWeight) >= passRate;

            default:
                return false;
        }
    }

    /// <summary>
    /// 评估下一个节点（处理条件分支）
    /// </summary>
    private async Task<List<string>> EvaluateNextNodesAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        string currentNodeId,
        IReadOnlyList<FlowEdge> outgoingEdges,
        CancellationToken cancellationToken)
    {
        var nextNodeIds = new List<string>();
        var currentNode = definition.GetNodeById(currentNodeId);

        // 判断是否为排他网关（XOR）：只走一条符合条件的路径
        var isExclusiveGateway = currentNode != null && currentNode.Type == "exclusiveGateway";
        // 判断是否为包容网关（Inclusive）：走所有符合条件的路径
        var isInclusiveGateway = currentNode != null && currentNode.Type == "inclusiveGateway";

        // Bug fix: For exclusive gateways, unconditional edges (default path) must be the FALLBACK,
        // not the priority. Evaluate all conditional edges first; only use the default if none match.
        string? exclusiveDefaultTarget = null;
        var inclusiveTargets = new List<string>();

        foreach (var edge in outgoingEdges)
        {
            // 如果没有条件规则
            if (string.IsNullOrEmpty(edge.ConditionRule))
            {
                if (isExclusiveGateway)
                {
                    // 排他网关：记录默认路径，但不立即返回——先评估所有条件边
                    exclusiveDefaultTarget ??= edge.Target;
                    continue;
                }
                if (isInclusiveGateway)
                {
                    // 包容网关：默认路径作为备选，如果没有其他路径满足时使用？
                    // 通常包容网关的无条件路径是"总是执行"或者"默认路径"
                    // 这里假设无条件路径总是执行
                    inclusiveTargets.Add(edge.Target);
                    continue;
                }
                nextNodeIds.Add(edge.Target);
                continue;
            }

            // 评估条件规则
            var passed = await _conditionEvaluator.EvaluateAsync(
                tenantId,
                instance.Id,
                edge.ConditionRule,
                instance.DataJson,
                cancellationToken);

            if (passed)
            {
                if (isExclusiveGateway)
                {
                    // 排他网关：找到第一个符合条件的路径就返回
                    return new List<string> { edge.Target };
                }
                if (isInclusiveGateway)
                {
                    inclusiveTargets.Add(edge.Target);
                    continue;
                }
                nextNodeIds.Add(edge.Target);
            }
        }

        // 排他网关：所有条件边都不满足时，走默认（无条件）路径
        if (isExclusiveGateway && exclusiveDefaultTarget != null)
        {
            return new List<string> { exclusiveDefaultTarget };
        }

        // 包容网关：返回所有满足条件的路径
        if (isInclusiveGateway)
        {
            // 如果没有满足条件的路径，且有默认路径（这里假设 inclusiveTargets 已经包含了无条件路径）
            // 如果 inclusiveTargets 为空，说明没有路径满足，这可能导致流程卡死
            // 实际上包容网关至少应该有一条路径被激活，否则视为异常
            if (inclusiveTargets.Count == 0 && exclusiveDefaultTarget != null)
            {
                 return new List<string> { exclusiveDefaultTarget };
            }
            return inclusiveTargets;
        }

        return nextNodeIds;
    }



    /// <summary>
    /// 处理子流程节点
    /// </summary>
    private async Task HandleSubProcessAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        FlowNode node,
        CancellationToken cancellationToken)
    {
        if (!node.CallProcessId.HasValue)
        {
            _logger?.LogWarning("CallProcess node {NodeId} has no CallProcessId configured, skipping", node.Id);
            // 没有配置子流程定义，自动跳过
            await AdvanceFlowAsync(tenantId, instance, definition, node.Id, cancellationToken);
            return;
        }

        // 通过 IServiceProvider 解析 CommandService（避免循环依赖）
        if (_serviceProvider != null)
        {
            var commandService = _serviceProvider.GetService<IApprovalRuntimeCommandService>();
            if (commandService != null)
            {
                await commandService.StartSubProcessAsync(
                    tenantId,
                    instance.Id,
                    node.Id,
                    node.CallProcessId.Value,
                    node.CallAsync,
                    cancellationToken);
            }
        }

        // 异步子流程，主流程继续推进
        if (node.CallAsync)
        {
            await AdvanceFlowAsync(tenantId, instance, definition, node.Id, cancellationToken);
        }
        // 同步子流程：等待子流程结束后由 EndSubProcessAsync 回调推进
    }

    /// <summary>
    /// 子流程结束回调（由 CommandService 或子流程完成时调用）
    /// </summary>
    public async Task EndSubProcessAsync(
        TenantId tenantId,
        long parentInstanceId,
        string parentNodeId,
        CancellationToken cancellationToken)
    {
        if (_serviceProvider == null) return;

        var instanceRepository = _serviceProvider.GetRequiredService<IApprovalInstanceRepository>();
        var flowRepository = _serviceProvider.GetRequiredService<IApprovalFlowRepository>();

        var parentInstance = await instanceRepository.GetByIdAsync(tenantId, parentInstanceId, cancellationToken);
        if (parentInstance == null || parentInstance.Status != ApprovalInstanceStatus.Running) return;

        var flowDef = await flowRepository.GetByIdAsync(tenantId, parentInstance.DefinitionId, cancellationToken);
        if (flowDef == null) return;

        var definition = FlowDefinitionParser.Parse(flowDef.DefinitionJson);

        // 标记子流程节点为已完成
        var nodeExecution = await _nodeExecutionRepository.GetByInstanceAndNodeAsync(
            tenantId, parentInstanceId, parentNodeId, cancellationToken);
        if (nodeExecution != null)
        {
            nodeExecution.MarkCompleted(DateTimeOffset.UtcNow);
            await _nodeExecutionRepository.UpdateAsync(nodeExecution, cancellationToken);
        }

        // 继续推进父流程
        await AdvanceFlowAsync(tenantId, parentInstance, definition, parentNodeId, cancellationToken);
        await instanceRepository.UpdateAsync(parentInstance, cancellationToken);
    }

    /// <summary>
    /// 处理定时器节点：创建 TimerJob 记录
    /// </summary>
    private async Task HandleTimerNodeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowNode node,
        CancellationToken cancellationToken)
    {
        // 解析定时器配置获取延迟时间
        var scheduledAt = ParseTimerSchedule(node.TimerConfig);

        if (_serviceProvider != null)
        {
            var timerJobRepo = _serviceProvider.GetService<IApprovalTimerJobRepository>();
            if (timerJobRepo != null)
            {
                var timerJob = new ApprovalTimerJob(
                    tenantId,
                    instance.Id,
                    node.Id,
                    scheduledAt,
                    _idGeneratorAccessor.NextId());
                await timerJobRepo.AddAsync(timerJob, cancellationToken);
                return;
            }
        }

        // 如果没有 TimerJob 仓储（兜底），直接自动通过
        _logger?.LogWarning("IApprovalTimerJobRepository not available, auto-completing timer node {NodeId}", node.Id);
    }

    /// <summary>
    /// 处理触发器节点：创建 TriggerJob 记录
    /// </summary>
    private async Task HandleTriggerNodeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowDefinition definition,
        FlowNode node,
        CancellationToken cancellationToken)
    {
        var triggerType = node.TriggerType ?? "immediate";

        if (triggerType == "immediate")
        {
            // 立即执行：记录后直接推进
            if (_serviceProvider != null)
            {
                var triggerJobRepo = _serviceProvider.GetService<IApprovalTriggerJobRepository>();
                if (triggerJobRepo != null)
                {
                    var triggerJob = new ApprovalTriggerJob(
                        tenantId,
                        instance.Id,
                        node.Id,
                        triggerType,
                        _timeProvider.GetUtcNow(),
                        _idGeneratorAccessor.NextId());
                    triggerJob.MarkExecuted(_timeProvider.GetUtcNow());
                    await triggerJobRepo.AddAsync(triggerJob, cancellationToken);
                }
            }

            // 标记执行完成并继续推进
            var nodeExecution = await _nodeExecutionRepository.GetByInstanceAndNodeAsync(
                tenantId, instance.Id, node.Id, cancellationToken);
            if (nodeExecution != null)
            {
                nodeExecution.MarkCompleted(DateTimeOffset.UtcNow);
                await _nodeExecutionRepository.UpdateAsync(nodeExecution, cancellationToken);
            }
            await AdvanceFlowAsync(tenantId, instance, definition, node.Id, cancellationToken);
        }
        else
        {
            // 定时执行：创建 TriggerJob 等待后台 Job 处理
            var scheduledAt = ParseTimerSchedule(node.TimerConfig);
            if (_serviceProvider != null)
            {
                var triggerJobRepo = _serviceProvider.GetService<IApprovalTriggerJobRepository>();
                if (triggerJobRepo != null)
                {
                    var triggerJob = new ApprovalTriggerJob(
                        tenantId,
                        instance.Id,
                        node.Id,
                        triggerType,
                        scheduledAt,
                        _idGeneratorAccessor.NextId());
                    await triggerJobRepo.AddAsync(triggerJob, cancellationToken);
                }
            }
        }
    }

    /// <summary>
    /// 解析定时器配置（JSON）获取计划执行时间
    /// </summary>
    private DateTimeOffset ParseTimerSchedule(string? timerConfigJson)
    {
        if (string.IsNullOrEmpty(timerConfigJson))
        {
            return _timeProvider.GetUtcNow().AddHours(1); // 默认1小时后
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(timerConfigJson);
            var root = doc.RootElement;

            var delayHours = 0;
            var delayMinutes = 0;

            if (root.TryGetProperty("delayHours", out var h) && h.ValueKind == System.Text.Json.JsonValueKind.Number)
                delayHours = h.GetInt32();
            if (root.TryGetProperty("delayMinutes", out var m) && m.ValueKind == System.Text.Json.JsonValueKind.Number)
                delayMinutes = m.GetInt32();

            // 支持绝对时间
            if (root.TryGetProperty("scheduledAt", out var sa) && sa.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(sa.GetString(), out var absolute))
                    return absolute;
            }

            return _timeProvider.GetUtcNow().AddHours(delayHours).AddMinutes(delayMinutes);
        }
        catch
        {
            return _timeProvider.GetUtcNow().AddHours(1);
        }
    }

    /// <summary>
    /// 激活顺序会签的下一个任务
    /// </summary>
    private async Task ActivateNextSequentialTaskAsync(
        TenantId tenantId,
        long instanceId,
        string nodeId,
        IReadOnlyList<ApprovalTask> tasks,
        CancellationToken cancellationToken)
    {
        // 找到已完成的最高顺序号
        var completedMaxOrder = tasks
            .Where(t => t.Status == ApprovalTaskStatus.Approved)
            .Select(t => t.Order)
            .DefaultIfEmpty(0)
            .Max();

        // 找到下一个等待激活的任务
        var nextTask = tasks
            .Where(t => t.Status == ApprovalTaskStatus.Waiting && t.Order == completedMaxOrder + 1)
            .OrderBy(t => t.Order)
            .FirstOrDefault();

        if (nextTask != null)
        {
            nextTask.Activate();
            await _taskRepository.UpdateAsync(nextTask, cancellationToken);
        }
    }

    /// <summary>
    /// 为抄送节点生成抄送记录（支持所有审批人策略类型）
    /// </summary>
    private async Task GenerateCopyRecordsForNodeAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        FlowNode node,
        CancellationToken cancellationToken)
    {
        var recipientIds = new List<long>();
        var assigneeType = node.AssigneeType;
        var assigneeValue = node.AssigneeValue ?? string.Empty;

        recipientIds = await _assigneeResolver.ResolveUserIdsAsync(
            tenantId,
            instance.InitiatorUserId,
            assigneeType,
            assigneeValue,
            instance.DataJson,
            cancellationToken);

        // 创建抄送记录
        var copyRecords = recipientIds.Select(userId => new ApprovalCopyRecord(
            tenantId,
            instance.Id,
            node.Id,
            userId,
            _idGeneratorAccessor.NextId())).ToList();

        if (copyRecords.Count > 0)
        {
            await _copyRecordRepository.AddRangeAsync(copyRecords, cancellationToken);
        }
    }


    /// <summary>
    /// Enqueue a callback to the background work queue (replaces unsafe Task.Run pattern).
    /// Each callback executes in its own DI scope, avoiding ObjectDisposedException.
    /// </summary>
    private void EnqueueCallback(
        TenantId tenantId,
        CallbackEventType eventType,
        long instanceId,
        long? taskId,
        string? nodeId)
    {
        if (_backgroundWorkQueue == null || _callbackService == null)
        {
            return;
        }

        _backgroundWorkQueue.Enqueue(async (sp, ct) =>
        {
            var callbackService = sp.GetRequiredService<ExternalCallbackService>();
            var instanceRepo = sp.GetRequiredService<IApprovalInstanceRepository>();
            var instance = await instanceRepo.GetByIdAsync(tenantId, instanceId, ct);
            if (instance == null) return;

            ApprovalTask? task = null;
            if (taskId.HasValue)
            {
                var taskRepo = sp.GetRequiredService<IApprovalTaskRepository>();
                task = await taskRepo.GetByIdAsync(tenantId, taskId.Value, ct);
            }

            await callbackService.TriggerCallbackAsync(
                tenantId, eventType, instance, task, nodeId, ct);
        });
    }
}




