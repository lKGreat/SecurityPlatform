namespace Atlas.Application.Identity.Models;

public sealed record RoleListItem(
    string Id,
    string Name,
    string Code,
    string? Description,
    bool IsSystem);

public sealed record RoleDetail(
    string Id,
    string Name,
    string Code,
    string? Description,
    bool IsSystem,
    IReadOnlyList<long> PermissionIds,
    IReadOnlyList<long> MenuIds);

public sealed record RoleCreateRequest(
    string Name,
    string Code,
    string? Description);

public sealed record RoleUpdateRequest(
    string Name,
    string? Description);

public sealed record RoleAssignPermissionsRequest(IReadOnlyList<long> PermissionIds);

public sealed record RoleAssignMenusRequest(IReadOnlyList<long> MenuIds);
