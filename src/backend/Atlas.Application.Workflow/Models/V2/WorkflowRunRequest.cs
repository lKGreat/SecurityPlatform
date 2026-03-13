namespace Atlas.Application.Workflow.Models.V2;

public sealed class WorkflowRunRequest
{
    /// <summary>入口参数，key = Entry 节点输出字段名，value = 值</summary>
    public Dictionary<string, object?> Inputs { get; set; } = new();

    /// <summary>使用指定版本执行（null 表示使用最新草稿）</summary>
    public string? Version { get; set; }
}
