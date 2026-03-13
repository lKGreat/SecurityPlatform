using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Enums;

namespace Atlas.Domain.AiPlatform.Entities;

/// <summary>
/// V2 工作流节点执行记录——每个节点每次执行产生一条。
/// </summary>
public sealed class WorkflowNodeExecution : TenantEntity
{
    /// <summary>SqlSugar 无参构造。</summary>
    public WorkflowNodeExecution() : base(TenantId.Empty)
    {
        NodeKey = string.Empty;
    }

    public WorkflowNodeExecution(
        TenantId tenantId,
        long executionId,
        string nodeKey,
        WorkflowNodeType nodeType,
        long id)
        : base(tenantId)
    {
        Id = id;
        ExecutionId = executionId;
        NodeKey = nodeKey;
        NodeType = nodeType;
        Status = ExecutionStatus.Pending;
    }

    public long ExecutionId { get; private set; }
    public string NodeKey { get; private set; }
    public WorkflowNodeType NodeType { get; private set; }
    public ExecutionStatus Status { get; private set; }
    public string? InputsJson { get; private set; }
    public string? OutputsJson { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public long? DurationMs { get; private set; }

    public void Start(string? inputsJson)
    {
        Status = ExecutionStatus.Running;
        InputsJson = inputsJson;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(string? outputsJson, long durationMs)
    {
        Status = ExecutionStatus.Completed;
        OutputsJson = outputsJson;
        DurationMs = durationMs;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
