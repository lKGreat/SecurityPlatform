namespace Atlas.Application.Identity.Models;

public sealed record DepartmentListItem(
    string Id,
    string Name,
    long? ParentId,
    int SortOrder);

public sealed record DepartmentCreateRequest(
    string Name,
    long? ParentId,
    int SortOrder);

public sealed record DepartmentUpdateRequest(
    string Name,
    long? ParentId,
    int SortOrder);
