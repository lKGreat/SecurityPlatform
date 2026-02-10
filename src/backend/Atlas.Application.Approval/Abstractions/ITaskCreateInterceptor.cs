using Atlas.Core.Tenancy;
using Atlas.Domain.Approval.Entities;

namespace Atlas.Application.Approval.Abstractions;

/// <summary>
/// 任务创建拦截器接口（允许在任务创建前后执行自定义逻辑）
/// 
/// 对标 FlowLong 的 TaskCreateInterceptor 接口。
/// 实现此接口并注册到 DI 容器即可在任务创建前后执行自定义逻辑。
/// </summary>
public interface ITaskCreateInterceptor
{
    /// <summary>
    /// 任务创建前置处理（可用于修改任务属性、校验、日志等）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="instance">流程实例</param>
    /// <param name="tasks">即将创建的任务列表（可修改）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BeforeCreateAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        List<ApprovalTask> tasks,
        CancellationToken cancellationToken)
    {
        // 默认不处理
        return Task.CompletedTask;
    }

    /// <summary>
    /// 任务创建后置处理（可用于发送通知、触发事件等）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="instance">流程实例</param>
    /// <param name="tasks">已创建的任务列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AfterCreateAsync(
        TenantId tenantId,
        ApprovalProcessInstance instance,
        IReadOnlyList<ApprovalTask> tasks,
        CancellationToken cancellationToken);
}
