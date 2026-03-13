using Atlas.Domain.Workflow.Enums;

namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowRunResponse
{
    public long ExecutionId { get; set; }

    public ExecutionStatus Status { get; set; }

    public Dictionary<string, object?> Outputs { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public long CostMs { get; set; }
}
