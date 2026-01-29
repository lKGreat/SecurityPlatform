namespace Atlas.WorkflowCore.DSL.Models.v1;

/// <summary>
/// V1 版本 When 分支定义
/// </summary>
public class WhenSourceV1
{
    /// <summary>
    /// 分支条件值或表达式
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 分支标签
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// 该分支的子步骤
    /// </summary>
    public List<StepSourceV1>? Do { get; set; }
}
