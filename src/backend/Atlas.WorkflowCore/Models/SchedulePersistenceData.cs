namespace Atlas.WorkflowCore.Models;

/// <summary>
/// 调度持久化数据
/// </summary>
public class SchedulePersistenceData
{
    /// <summary>
    /// 下次执行时间
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// 执行次数
    /// </summary>
    public int ExecutionCount { get; set; }
}
