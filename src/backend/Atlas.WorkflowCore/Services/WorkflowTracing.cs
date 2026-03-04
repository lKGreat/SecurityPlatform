using Atlas.WorkflowCore.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.WorkflowCore.Services;

/// <summary>
/// 工作流追踪 - OpenTelemetry 集成（简化版本）
/// </summary>
/// <remarks>
/// 此类为 OpenTelemetry 集成预留，当前为简化实现。
/// 要启用完整追踪，需要添加 OpenTelemetry NuGet 包：
/// - OpenTelemetry
/// - OpenTelemetry.Api
/// - OpenTelemetry.Instrumentation.AspNetCore
/// </remarks>
public static class WorkflowTracing
{
    private static ILogger? _logger;

    /// <summary>
    /// 初始化追踪活动
    /// </summary>
    public static void Initialize(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 启动主机追踪
    /// </summary>
    public static void StartHost()
    {
        _logger?.LogDebug("[Tracing] WorkflowHost started");
        // 当前能力边界：仅输出结构化日志，不创建 Activity；完整 OpenTelemetry 接入见任务 OBS-145（版本：v1.5）。
    }

    /// <summary>
    /// 丰富工作流追踪信息
    /// </summary>
    public static void Enrich(WorkflowInstance workflow)
    {
        _logger?.LogDebug("[Tracing] Workflow {WorkflowId} enriched", workflow.Id);
        // 当前能力边界：Span Tag 尚未启用，避免在无 Activity 上写入无效元数据；任务：OBS-145，版本：v1.5。
        // Activity.Current?.SetTag("workflow.id", workflow.Id);
        // Activity.Current?.SetTag("workflow.definition", workflow.WorkflowDefinitionId);
        // Activity.Current?.SetTag("workflow.version", workflow.Version);
        // Activity.Current?.SetTag("workflow.status", workflow.Status);
    }

    /// <summary>
    /// 丰富步骤追踪信息
    /// </summary>
    public static void Enrich(WorkflowStep step)
    {
        _logger?.LogDebug("[Tracing] Step {StepName} enriched", step.Name);
        // 当前能力边界：Span Tag 尚未启用，避免在无 Activity 上写入无效元数据；任务：OBS-145，版本：v1.5。
        // Activity.Current?.SetTag("step.id", step.Id);
        // Activity.Current?.SetTag("step.name", step.Name);
        // Activity.Current?.SetTag("step.type", step.BodyType.Name);
    }

    /// <summary>
    /// 丰富执行结果追踪信息
    /// </summary>
    public static void Enrich(ExecutionResult result)
    {
        _logger?.LogDebug("[Tracing] ExecutionResult enriched - Proceed: {Proceed}", result.Proceed);
        // 当前能力边界：Span Tag 尚未启用，避免在无 Activity 上写入无效元数据；任务：OBS-145，版本：v1.5。
        // Activity.Current?.SetTag("result.proceed", result.Proceed);
        // Activity.Current?.SetTag("result.outcome", result.OutcomeValue);
    }

    /// <summary>
    /// 丰富执行器结果追踪信息
    /// </summary>
    public static void Enrich(WorkflowExecutorResult result)
    {
        _logger?.LogDebug("[Tracing] WorkflowExecutorResult enriched - Errors: {ErrorCount}, Subscriptions: {SubscriptionCount}", 
            result.Errors.Count, result.Subscriptions.Count);
        // 当前能力边界：Span Tag 尚未启用，避免在无 Activity 上写入无效元数据；任务：OBS-145，版本：v1.5。
        // Activity.Current?.SetTag("result.errors.count", result.Errors.Count);
        // Activity.Current?.SetTag("result.subscriptions.count", result.Subscriptions.Count);
    }
}
