namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowSaveRequest
{
    /// <summary>画布 JSON（序列化后的 CanvasSchema）</summary>
    public string CanvasJson { get; set; } = string.Empty;
}
