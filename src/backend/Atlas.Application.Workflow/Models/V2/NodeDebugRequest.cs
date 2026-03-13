namespace Atlas.Application.Workflow.Models.V2;

public sealed class NodeDebugRequest
{
    /// <summary>节点 key</summary>
    public string NodeKey { get; set; } = string.Empty;

    /// <summary>节点输入数据</summary>
    public Dictionary<string, object?> Inputs { get; set; } = new();
}
