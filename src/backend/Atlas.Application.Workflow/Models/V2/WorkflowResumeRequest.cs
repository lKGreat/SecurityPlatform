namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowResumeRequest
{
    /// <summary>用户回答或表单数据，key=字段名，value=值</summary>
    public Dictionary<string, object?> Data { get; set; } = new();
}
