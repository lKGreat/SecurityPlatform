using Atlas.Application.Abstractions;
using Atlas.Application.Identity.Repositories;
using Atlas.Application.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Identity.Entities;

namespace Atlas.Infrastructure.Services;

public sealed class AuthProfileService : IAuthProfileService
{
    private readonly IUserAccountRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IPermissionRepository _permissionRepository;

    public AuthProfileService(
        IUserAccountRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IRolePermissionRepository rolePermissionRepository,
        IPermissionRepository permissionRepository)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<AuthProfileResult?> GetProfileAsync(
        long userId,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var account = await _userRepository.FindByIdAsync(tenantId, userId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var roleCodes = await ResolveRoleCodesAsync(account, tenantId, cancellationToken);
        var permissionCodes = await ResolvePermissionCodesAsync(tenantId, userId, cancellationToken);

        return new AuthProfileResult(
            account.Id.ToString(),
            account.Username,
            account.DisplayName,
            tenantId.Value.ToString("D"),
            roleCodes,
            permissionCodes);
    }

    private async Task<IReadOnlyList<string>> ResolveRoleCodesAsync(
        UserAccount account,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(account.Roles))
        {
            foreach (var role in account.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                codes.Add(role);
            }
        }

        var userRoles = await _userRoleRepository.QueryByUserIdAsync(tenantId, account.Id, cancellationToken);
        if (userRoles.Count == 0)
        {
            return codes.ToArray();
        }

        var roleIds = userRoles.Select(x => x.RoleId).Distinct().ToArray();
        var roles = await _roleRepository.QueryByIdsAsync(tenantId, roleIds, cancellationToken);
        foreach (var role in roles)
        {
            codes.Add(role.Code);
        }

        return codes.ToArray();
    }

    private async Task<IReadOnlyList<string>> ResolvePermissionCodesAsync(
        TenantId tenantId,
        long userId,
        CancellationToken cancellationToken)
    {
        var userRoles = await _userRoleRepository.QueryByUserIdAsync(tenantId, userId, cancellationToken);
        if (userRoles.Count == 0)
        {
            return Array.Empty<string>();
        }

        var permissionIds = new HashSet<long>();
        foreach (var roleId in userRoles.Select(x => x.RoleId).Distinct())
        {
            var rolePermissions = await _rolePermissionRepository.QueryByRoleIdAsync(tenantId, roleId, cancellationToken);
            foreach (var permissionId in rolePermissions.Select(x => x.PermissionId))
            {
                permissionIds.Add(permissionId);
            }
        }

        if (permissionIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var permissions = await _permissionRepository.QueryByIdsAsync(tenantId, permissionIds.ToArray(), cancellationToken);
        return permissions.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
