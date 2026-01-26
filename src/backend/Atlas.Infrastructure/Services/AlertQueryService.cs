using Atlas.Application.Alert.Abstractions;
using Atlas.Application.Alert.Models;
using Atlas.Core.Abstractions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Infrastructure.Services;

public sealed class AlertQueryService : IAlertQueryService
{
    private readonly IIdGenerator _idGenerator;

    public AlertQueryService(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public PagedResult<AlertListItem> QueryAlerts(PagedRequest request, TenantId tenantId)
    {
        var total = 12;
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var start = (pageIndex - 1) * pageSize;
        if (start >= total)
        {
            return new PagedResult<AlertListItem>(Array.Empty<AlertListItem>(), total, pageIndex, pageSize);
        }

        var count = Math.Min(pageSize, total - start);
        var baseTime = DateTimeOffset.UtcNow;
        var items = Enumerable.Range(start, count)
            .Select(i => new AlertListItem(_idGenerator.NextId().ToString(), $"告警-{i + 1}", baseTime.AddMinutes(-i * 5)))
            .ToArray();

        return new PagedResult<AlertListItem>(items, total, pageIndex, pageSize);
    }
}