namespace Atlas.WorkflowCore.DSL.Models.v1;

/// <summary>
/// V1 版本映射定义（输入/输出映射）
/// </summary>
public class MappingSourceV1
{
    /// <summary>
    /// 目标属性名
    /// </summary>
    public string Property { get; set; } = string.Empty;

    /// <summary>
    /// 源值或表达式
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 表达式字符串（用于动态求值）
    /// </summary>
    public string? Expression { get; set; }
}
