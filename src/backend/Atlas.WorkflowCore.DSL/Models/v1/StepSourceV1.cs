namespace Atlas.WorkflowCore.DSL.Models.v1;

/// <summary>
/// V1 版本步骤定义源
/// </summary>
public class StepSourceV1
{
    /// <summary>
    /// 步骤ID（可选）
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 步骤名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 步骤类型（完全限定名）
    /// </summary>
    public string? StepType { get; set; }

    /// <summary>
    /// 下一步骤ID（用于简单的顺序流程）
    /// </summary>
    public string? NextStepId { get; set; }

    /// <summary>
    /// 输入映射
    /// </summary>
    public Dictionary<string, object>? Inputs { get; set; }

    /// <summary>
    /// 输出映射
    /// </summary>
    public Dictionary<string, string>? Outputs { get; set; }

    /// <summary>
    /// 结果分支（用于条件流程）
    /// </summary>
    public Dictionary<string, object>? SelectNextStep { get; set; }

    /// <summary>
    /// 子步骤（用于容器步骤）
    /// </summary>
    public List<StepSourceV1>? Do { get; set; }

    /// <summary>
    /// 补偿步骤
    /// </summary>
    public List<StepSourceV1>? CompensateWith { get; set; }

    /// <summary>
    /// 错误处理行为
    /// </summary>
    public string? OnError { get; set; }

    /// <summary>
    /// 重试间隔（毫秒）
    /// </summary>
    public int? RetryIntervalMs { get; set; }

    /// <summary>
    /// 取消条件表达式
    /// </summary>
    public string? CancelCondition { get; set; }

    /// <summary>
    /// When 分支列表（用于 Decide/OutcomeSwitch）
    /// </summary>
    public List<WhenSourceV1>? When { get; set; }
}
