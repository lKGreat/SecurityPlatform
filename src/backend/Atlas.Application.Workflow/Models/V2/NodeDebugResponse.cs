using Atlas.Domain.Workflow.Enums;

namespace Atlas.Application.Workflow.Models.V2;

public sealed class NodeDebugResponse
{
    public string NodeKey { get; set; } = string.Empty;

    public ExecutionStatus Status { get; set; }

    public Dictionary<string, object?> Inputs { get; set; } = new();

    public Dictionary<string, object?> Outputs { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public long CostMs { get; set; }

    public int? TokensUsed { get; set; }
}
