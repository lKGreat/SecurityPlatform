namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowVersionItem
{
    public long Id { get; set; }

    public string Version { get; set; } = string.Empty;

    public string CommitId { get; set; } = string.Empty;

    public string ChangeLog { get; set; } = string.Empty;

    public DateTimeOffset PublishedAt { get; set; }
}
