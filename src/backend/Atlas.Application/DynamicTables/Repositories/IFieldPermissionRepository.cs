using Atlas.Core.Tenancy;
using Atlas.Domain.DynamicTables.Entities;

namespace Atlas.Application.DynamicTables.Repositories;

public interface IFieldPermissionRepository
{
    Task<IReadOnlyList<FieldPermission>> ListByTableKeyAsync(
        TenantId tenantId,
        string tableKey,
        long? appId,
        CancellationToken cancellationToken);

    Task ReplaceByTableKeyAsync(
        TenantId tenantId,
        string tableKey,
        long? appId,
        IReadOnlyList<FieldPermission> permissions,
        CancellationToken cancellationToken);

    /// <summary>查询某应用下某角色（RoleCode）所有表的字段权限。</summary>
    Task<IReadOnlyList<FieldPermission>> ListByRoleCodeAndAppIdAsync(
        TenantId tenantId,
        long appId,
        string roleCode,
        CancellationToken cancellationToken);

    /// <summary>替换某应用下某角色的所有字段权限（先按 roleCode+app 范围删除，再批量插入）。</summary>
    Task ReplaceByRoleCodeAndAppIdAsync(
        TenantId tenantId,
        long appId,
        string roleCode,
        IReadOnlyList<FieldPermission> permissions,
        CancellationToken cancellationToken);
}
