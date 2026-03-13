using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Enums;

namespace Atlas.Domain.Workflow.Entities;

/// <summary>
/// 节点执行记录，记录工作流中单个节点的执行详情。
/// </summary>
public sealed class NodeExecution : TenantEntity
{
    public NodeExecution()
        : base(TenantId.Empty)
    {
        NodeKey = string.Empty;
        NodeTitle = string.Empty;
    }

    public NodeExecution(TenantId tenantId, long id, long executionId, string nodeKey, NodeType nodeType, string nodeTitle)
        : base(tenantId)
    {
        SetId(id);
        ExecutionId = executionId;
        NodeKey = nodeKey;
        NodeType = nodeType;
        NodeTitle = nodeTitle;
        Status = ExecutionStatus.Pending;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public long ExecutionId { get; private set; }

    public string NodeKey { get; private set; }

    public NodeType NodeType { get; private set; }

    public string NodeTitle { get; private set; }

    public string? InputJson { get; private set; }

    public string? OutputJson { get; private set; }

    public string? ErrorMessage { get; private set; }

    public ExecutionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public long CostMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : 0;

    /// <summary>LLM 节点消耗的 Token 数量</summary>
    public int? TokensUsed { get; private set; }

    /// <summary>节点执行次数（用于 Loop 内的节点）</summary>
    public int IterationIndex { get; private set; }

    public void Start(string inputJson)
    {
        Status = ExecutionStatus.Running;
        InputJson = inputJson;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(string outputJson, int? tokensUsed = null)
    {
        Status = ExecutionStatus.Success;
        OutputJson = outputJson;
        TokensUsed = tokensUsed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void SetIterationIndex(int index)
    {
        IterationIndex = index;
    }
}
