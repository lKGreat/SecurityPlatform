namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowPublishRequest
{
    /// <summary>变更说明</summary>
    public string ChangeLog { get; set; } = string.Empty;
}
