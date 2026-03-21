using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Application.Identity.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> FindByIdAsync(TenantId tenantId, long id, CancellationToken cancellationToken);
    /// <summary>仅查询平台级权限（AppId IS NULL），防止跨级越权访问应用级权限。</summary>
    Task<Permission?> FindByIdPlatformOnlyAsync(TenantId tenantId, long id, CancellationToken cancellationToken);
    Task<Permission?> FindByCodeAsync(TenantId tenantId, string code, long? appId, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Permission> Items, int TotalCount)> QueryPageAsync(
        TenantId tenantId,
        int pageIndex,
        int pageSize,
        string? keyword,
        string? type,
        CancellationToken cancellationToken,
        long? appId = null,
        bool platformOnly = false);
    Task<IReadOnlyList<Permission>> QueryByIdsAsync(TenantId tenantId, IReadOnlyList<long> ids, CancellationToken cancellationToken);
    /// <summary>仅查询平台级权限（AppId IS NULL），防止平台角色绑定应用级权限导致跨级提权。</summary>
    Task<IReadOnlyList<Permission>> QueryByIdsPlatformOnlyAsync(TenantId tenantId, IReadOnlyList<long> ids, CancellationToken cancellationToken);
    Task<IReadOnlyList<Permission>> QueryAllAsync(TenantId tenantId, CancellationToken cancellationToken, bool platformOnly = false);
    Task AddAsync(Permission permission, CancellationToken cancellationToken);
    Task UpdateAsync(Permission permission, CancellationToken cancellationToken);
    Task DeleteAsync(TenantId tenantId, long id, CancellationToken cancellationToken);
    Task<Permission?> FindByIdAndAppIdAsync(TenantId tenantId, long appId, long id, CancellationToken cancellationToken);
}
