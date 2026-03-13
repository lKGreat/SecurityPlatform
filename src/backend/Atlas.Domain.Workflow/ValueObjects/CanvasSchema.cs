namespace Atlas.Domain.Workflow.ValueObjects;

/// <summary>
/// 工作流画布 JSON Schema，对应数据库中存储的 canvas_json 字段。
/// 格式：{ "nodes": [...], "connections": [...] }
/// </summary>
public sealed class CanvasSchema
{
    public CanvasSchema()
    {
        Nodes = new List<NodeSchema>();
        Connections = new List<ConnectionSchema>();
    }

    public List<NodeSchema> Nodes { get; set; }

    public List<ConnectionSchema> Connections { get; set; }
}
