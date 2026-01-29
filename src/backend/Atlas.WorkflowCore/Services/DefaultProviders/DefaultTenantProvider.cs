using Atlas.Core.Tenancy;

namespace Atlas.WorkflowCore.Services.DefaultProviders;

/// <summary>
/// 默认租户提供者 - 返回空租户ID，用于单租户场景或开发测试
/// </summary>
public class DefaultTenantProvider : ITenantProvider
{
    /// <summary>
    /// 返回固定的空租户ID
    /// </summary>
    public TenantId GetTenantId() => new TenantId(Guid.Empty);
}
