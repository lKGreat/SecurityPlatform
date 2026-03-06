using Atlas.Core.Tenancy;
using Atlas.Domain.Integration;

namespace Atlas.Application.Integration;

public interface IIntegrationApiKeyRepository
{
    /// <summary>按租户查找所有有效（IsActive=true 且未过期）的 API Key 记录</summary>
    Task<IReadOnlyList<IntegrationApiKey>> GetActiveByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>更新最后使用时间</summary>
    Task UpdateLastUsedAtAsync(long id, DateTimeOffset usedAt, CancellationToken cancellationToken);
}
