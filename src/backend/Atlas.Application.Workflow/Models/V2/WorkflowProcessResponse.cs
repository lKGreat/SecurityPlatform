using Atlas.Domain.Workflow.Enums;

namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowProcessResponse
{
    public long ExecutionId { get; set; }

    public ExecutionStatus Status { get; set; }

    public long CostMs { get; set; }

    public string? ErrorMessage { get; set; }

    public List<NodeExecutionItem> Nodes { get; set; } = new();
}

public sealed class NodeExecutionItem
{
    public long Id { get; set; }

    public string NodeKey { get; set; } = string.Empty;

    public NodeType NodeType { get; set; }

    public string NodeTitle { get; set; } = string.Empty;

    public ExecutionStatus Status { get; set; }

    public long CostMs { get; set; }

    public int? TokensUsed { get; set; }

    public int IterationIndex { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
