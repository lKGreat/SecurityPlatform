using Atlas.WorkflowCore.Models;

namespace Atlas.WorkflowCore.Abstractions.Persistence;

/// <summary>
/// 计划命令仓储接口
/// </summary>
public interface IScheduledCommandRepository
{
    /// <summary>
    /// 是否支持计划命令
    /// </summary>
    bool SupportsScheduledCommands { get; }

    /// <summary>
    /// 调度命令
    /// </summary>
    /// <param name="command">计划命令</param>
    Task ScheduleCommand(ScheduledCommand command);

    /// <summary>
    /// 调度命令（异步版本）
    /// </summary>
    /// <param name="command">计划命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ScheduleCommandAsync(ScheduledCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 处理到期的计划命令
    /// </summary>
    /// <param name="asOf">截止时间</param>
    /// <param name="action">处理动作</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ProcessCommands(DateTimeOffset asOf, Func<ScheduledCommand, Task> action, CancellationToken cancellationToken = default);
}
