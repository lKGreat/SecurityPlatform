using Atlas.Core.Abstractions;
using Atlas.Application.Approval.Repositories;
using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;
using Atlas.Domain.Approval.Enums;

namespace Atlas.Infrastructure.Services.ApprovalFlow;

/// <summary>
/// 流程网关分支处理器（并行/包容分支的 token 创建与分支推进）
/// </summary>
public sealed class FlowGatewayHandler
{
    private readonly IApprovalParallelTokenRepository _parallelTokenRepository;
    private readonly IIdGeneratorAccessor _idGeneratorAccessor;

    public FlowGatewayHandler(
        IApprovalParallelTokenRepository parallelTokenRepository,
        IIdGeneratorAccessor idGeneratorAccessor)
    {
        _parallelTokenRepository = parallelTokenRepository;
        _idGeneratorAccessor = idGeneratorAccessor;
    }

    /// <summary>
    /// 处理并行网关分支：创建 token 并推进所有分支
    /// </summary>
    public async Task HandleParallelSplitAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        string gatewayNodeId,
        IReadOnlyList<string> nextNodeIds,
        FlowDefinition definition,
        Func<TenantId, ApprovalProcessInstance, FlowDefinition, string, CancellationToken, FlowExecutionContext?, Task> processNextNodeAsync,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var context = executionContext ?? new FlowExecutionContext();
        await HandleSplitAsync(
            tenantId,
            instance,
            gatewayNodeId,
            nextNodeIds,
            definition,
            processNextNodeAsync,
            cancellationToken,
            context);
    }

    /// <summary>
    /// 处理包容网关分支：创建 token 并推进满足条件的分支
    /// </summary>
    public async Task HandleInclusiveSplitAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        string gatewayNodeId,
        IReadOnlyList<string> nextNodeIds,
        FlowDefinition definition,
        Func<TenantId, ApprovalProcessInstance, FlowDefinition, string, CancellationToken, FlowExecutionContext?, Task> processNextNodeAsync,
        CancellationToken cancellationToken,
        FlowExecutionContext? executionContext = null)
    {
        var context = executionContext ?? new FlowExecutionContext();
        await HandleSplitAsync(
            tenantId,
            instance,
            gatewayNodeId,
            nextNodeIds,
            definition,
            processNextNodeAsync,
            cancellationToken,
            context);
    }

    private async Task HandleSplitAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        string gatewayNodeId,
        IReadOnlyList<string> nextNodeIds,
        FlowDefinition definition,
        Func<TenantId, ApprovalProcessInstance, FlowDefinition, string, CancellationToken, FlowExecutionContext?, Task> processNextNodeAsync,
        CancellationToken cancellationToken,
        FlowExecutionContext context)
    {
        var tokens = nextNodeIds
            .Select(nextNodeId => new ApprovalParallelToken(
                tenantId,
                instance.Id,
                gatewayNodeId,
                nextNodeId,
                _idGeneratorAccessor.NextId()))
            .ToList();

        if (tokens.Count > 0)
        {
            await _parallelTokenRepository.AddRangeAsync(tokens, cancellationToken);
        }

        foreach (var nextNodeId in nextNodeIds)
        {
            await processNextNodeAsync(
                tenantId,
                instance,
                definition,
                nextNodeId,
                cancellationToken,
                context);
        }
    }

    /// <summary>
    /// 检查并行汇聚网关是否所有分支都已完成
    /// </summary>
    public async Task<bool> CheckParallelJoinCompletionAsync(
        TenantId tenantId,
        long instanceId,
        string gatewayNodeId,
        FlowDefinition definition,
        CancellationToken cancellationToken)
    {
        var incomingEdges = definition.GetIncomingEdges(gatewayNodeId);
        if (incomingEdges.Count <= 1)
        {
            return true;
        }

        var tokens = await _parallelTokenRepository.GetByInstanceAndGatewayAsync(tenantId, instanceId, gatewayNodeId, cancellationToken);
        var completedBranches = tokens.Where(t => t.Status == ParallelTokenStatus.Completed).Select(t => t.BranchNodeId).ToHashSet();
        var requiredBranches = incomingEdges.Select(e => e.Source).ToHashSet();

        return requiredBranches.All(branch => completedBranches.Contains(branch));
    }

    /// <summary>
    /// 标记并行分支为已完成（批量更新，避免 N+1）
    /// </summary>
    public async Task MarkParallelBranchCompletedAsync(
        TenantId tenantId,
        long instanceId,
        string gatewayNodeId,
        FlowDefinition definition,
        CancellationToken cancellationToken)
    {
        var incomingEdges = definition.GetIncomingEdges(gatewayNodeId);
        var tokens = await _parallelTokenRepository.GetByInstanceAndGatewayAsync(tenantId, instanceId, gatewayNodeId, cancellationToken);
        var tokensByBranch = tokens.Where(t => t.Status == ParallelTokenStatus.Active).ToDictionary(t => t.BranchNodeId);

        var tokensToUpdate = new List<ApprovalParallelToken>();
        foreach (var edge in incomingEdges)
        {
            if (tokensByBranch.TryGetValue(edge.Source, out var branchToken))
            {
                branchToken.MarkCompleted(DateTimeOffset.UtcNow);
                tokensToUpdate.Add(branchToken);
            }
        }

        if (tokensToUpdate.Count > 0)
        {
            await _parallelTokenRepository.UpdateRangeAsync(tokensToUpdate, cancellationToken);
        }
    }
}
