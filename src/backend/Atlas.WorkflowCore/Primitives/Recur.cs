using System.Text.Json;
using Atlas.WorkflowCore.Abstractions;
using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Primitives;

/// <summary>
/// 循环调度步骤 - 按间隔循环执行子步骤直到条件满足
/// </summary>
public class Recur : ContainerStepBody
{
    /// <summary>
    /// 循环间隔
    /// </summary>
    public TimeSpan Interval { get; set; }

    /// <summary>
    /// 停止条件
    /// </summary>
    public bool StopCondition { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (StopCondition)
        {
            return ExecutionResult.Next();
        }

        // 从持久化数据恢复状态
        SchedulePersistenceData? persistenceData = null;
        
        if (context.PersistenceData != null)
        {
            var jsonString = context.PersistenceData.ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                persistenceData = JsonSerializer.Deserialize<SchedulePersistenceData>(jsonString);
            }
        }

        persistenceData ??= new SchedulePersistenceData();
        persistenceData.ExecutionCount++;

        // 保存持久化数据并休眠
        var jsonData = JsonSerializer.Serialize(persistenceData);
        return ExecutionResult.Sleep(Interval, jsonData);
    }
}
