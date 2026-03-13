namespace Atlas.Domain.Workflow.ValueObjects;

/// <summary>
/// 画布连线，表示节点之间的有向边。
/// </summary>
public sealed class ConnectionSchema
{
    public ConnectionSchema()
    {
        FromNode = string.Empty;
        ToNode = string.Empty;
    }

    public string FromNode { get; set; }

    /// <summary>源端口（If 节点使用 "true"/"false"，Loop 节点使用 "body"/"done"）</summary>
    public string? FromPort { get; set; }

    public string ToNode { get; set; }

    public string? ToPort { get; set; }
}
