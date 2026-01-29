namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 计划命令 - 用于延迟执行的命令
/// </summary>
public class ScheduledCommand
{
    /// <summary>
    /// 处理工作流命令名称
    /// </summary>
    public const string ProcessWorkflow = "ProcessWorkflow";

    /// <summary>
    /// 命令ID
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// 命令名称
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// 命令数据
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime ExecuteTime { get; set; }

    /// <summary>
    /// 工作流ID
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>
    /// 执行指针ID
    /// </summary>
    public string ExecutionPointerId { get; set; } = string.Empty;

    /// <summary>
    /// 步骤ID
    /// </summary>
    public int StepId { get; set; }
}
