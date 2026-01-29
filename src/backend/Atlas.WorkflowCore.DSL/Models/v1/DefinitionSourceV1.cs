namespace Atlas.WorkflowCore.DSL.Models.v1;

/// <summary>
/// V1 版本工作流定义源
/// </summary>
public class DefinitionSourceV1 : DefinitionSource
{
    /// <summary>
    /// 步骤列表
    /// </summary>
    public List<StepSourceV1> Steps { get; set; } = new();

    /// <summary>
    /// 默认错误行为
    /// </summary>
    public string? DefaultErrorBehavior { get; set; }

    /// <summary>
    /// 默认错误重试间隔（毫秒）
    /// </summary>
    public int? DefaultErrorRetryIntervalMs { get; set; }
}
