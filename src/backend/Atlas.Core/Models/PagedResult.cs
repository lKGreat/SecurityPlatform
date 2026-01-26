namespace Atlas.Core.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    long Total,
    int PageIndex,
    int PageSize
);