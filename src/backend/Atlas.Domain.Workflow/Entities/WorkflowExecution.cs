using Atlas.Core.Abstractions;
using Atlas.Core.Tenancy;
using Atlas.Domain.Workflow.Enums;

namespace Atlas.Domain.Workflow.Entities;

/// <summary>
/// 工作流执行实例，记录一次工作流运行的全生命周期。
/// </summary>
public sealed class WorkflowExecution : TenantEntity
{
    public WorkflowExecution()
        : base(TenantId.Empty)
    {
        InputJson = string.Empty;
    }

    public WorkflowExecution(TenantId tenantId, long id, long workflowId, string? workflowVersion, string inputJson)
        : base(tenantId)
    {
        SetId(id);
        WorkflowId = workflowId;
        WorkflowVersion = workflowVersion;
        InputJson = inputJson;
        Status = ExecutionStatus.Pending;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public long WorkflowId { get; private set; }

    /// <summary>执行时使用的版本（null 表示使用草稿）</summary>
    public string? WorkflowVersion { get; private set; }

    public string InputJson { get; private set; }

    public string? OutputJson { get; private set; }

    public string? ErrorMessage { get; private set; }

    public ExecutionStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public long CostMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : 0;

    /// <summary>中断类型（用于 QuestionAnswer 等中断节点）</summary>
    public InterruptType InterruptType { get; private set; }

    /// <summary>中断时等待数据的上下文（序列化为 JSON）</summary>
    public string? InterruptContextJson { get; private set; }

    public void Start()
    {
        Status = ExecutionStatus.Running;
    }

    public void Complete(string outputJson)
    {
        Status = ExecutionStatus.Success;
        OutputJson = outputJson;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Interrupt(InterruptType type, string contextJson)
    {
        Status = ExecutionStatus.Interrupted;
        InterruptType = type;
        InterruptContextJson = contextJson;
    }

    public void Resume()
    {
        Status = ExecutionStatus.Running;
        InterruptType = InterruptType.None;
        InterruptContextJson = null;
    }
}
