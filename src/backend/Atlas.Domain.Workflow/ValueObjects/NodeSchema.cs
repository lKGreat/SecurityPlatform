using Atlas.Domain.Workflow.Enums;

namespace Atlas.Domain.Workflow.ValueObjects;

/// <summary>
/// 画布节点的结构定义，对应 Canvas JSON 中的单个 node 元素。
/// </summary>
public sealed class NodeSchema
{
    public NodeSchema()
    {
        Key = string.Empty;
        Title = string.Empty;
        Configs = new Dictionary<string, object?>();
        InputMappings = new Dictionary<string, string>();
    }

    /// <summary>唯一标识键，如 "llm_1"</summary>
    public string Key { get; set; }

    /// <summary>节点类型</summary>
    public NodeType Type { get; set; }

    /// <summary>节点显示名称</summary>
    public string Title { get; set; }

    /// <summary>节点在画布上的坐标与尺寸</summary>
    public NodeLayout? Layout { get; set; }

    /// <summary>节点配置参数（类型专属，JSON 序列化后存储）</summary>
    public Dictionary<string, object?> Configs { get; set; }

    /// <summary>输入映射：本节点输入字段 → 上游节点输出引用，如 "prompt" → "entry_1.userInput"</summary>
    public Dictionary<string, string> InputMappings { get; set; }
}
