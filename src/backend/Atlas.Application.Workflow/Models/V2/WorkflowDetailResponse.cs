using Atlas.Domain.Workflow.Enums;

namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowDetailResponse
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public WorkflowMode Mode { get; set; }

    public WorkflowLifecycleStatus Status { get; set; }

    public string? LatestVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>草稿画布 JSON</summary>
    public string? CanvasJson { get; set; }

    /// <summary>草稿 CommitId</summary>
    public string? CommitId { get; set; }
}
