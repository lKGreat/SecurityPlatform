namespace Atlas.WorkflowCore.DSL.Models;

/// <summary>
/// 工作流定义源 - 抽象基类
/// </summary>
public abstract class DefinitionSource
{
    /// <summary>
    /// 工作流ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 数据类型（完全限定名）
    /// </summary>
    public string? DataType { get; set; }
}
