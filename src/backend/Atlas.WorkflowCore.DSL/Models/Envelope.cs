namespace Atlas.WorkflowCore.DSL.Models;

/// <summary>
/// DSL 定义封装 - 包含版本信息
/// </summary>
public class Envelope
{
    /// <summary>
    /// DSL 版本
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 定义源（反序列化为具体版本）
    /// </summary>
    public DefinitionSource? Definition { get; set; }
}
