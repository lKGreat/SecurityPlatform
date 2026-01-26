namespace Atlas.Core.Models;

public sealed record PagedRequest(
    int PageIndex,
    int PageSize,
    string? Keyword,
    string? SortBy,
    bool SortDesc
);