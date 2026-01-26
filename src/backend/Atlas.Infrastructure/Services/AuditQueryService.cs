using Atlas.Application.Audit.Abstractions;
using Atlas.Application.Audit.Models;
using Atlas.Core.Abstractions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Infrastructure.Services;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly IIdGenerator _idGenerator;

    public AuditQueryService(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public PagedResult<AuditListItem> QueryAudits(PagedRequest request, TenantId tenantId)
    {
        var total = 18;
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var start = (pageIndex - 1) * pageSize;
        if (start >= total)
        {
            return new PagedResult<AuditListItem>(Array.Empty<AuditListItem>(), total, pageIndex, pageSize);
        }

        var count = Math.Min(pageSize, total - start);
        var baseTime = DateTimeOffset.UtcNow;
        var items = Enumerable.Range(start, count)
            .Select(i => new AuditListItem(_idGenerator.NextId().ToString(), $"审计操作-{i + 1}", baseTime.AddMinutes(-i)))
            .ToArray();

        return new PagedResult<AuditListItem>(items, total, pageIndex, pageSize);
    }
}