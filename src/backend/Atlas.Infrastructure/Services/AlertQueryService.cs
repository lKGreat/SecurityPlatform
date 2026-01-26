using AutoMapper;
using Atlas.Application.Alert.Abstractions;
using Atlas.Application.Alert.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Alert.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Services;

public sealed class AlertQueryService : IAlertQueryService
{
    private readonly ISqlSugarClient _db;
    private readonly IMapper _mapper;

    public AlertQueryService(ISqlSugarClient db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public PagedResult<AlertListItem> QueryAlerts(PagedRequest request, TenantId tenantId)
    {
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var total = 0;

        var query = _db.Queryable<AlertRecord>();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(x => x.Title.Contains(request.Keyword));
        }

        var items = query
            .OrderBy(x => x.CreatedAt, OrderByType.Desc)
            .ToPageList(pageIndex, pageSize, ref total)
            .Select(x => _mapper.Map<AlertListItem>(x))
            .ToArray();

        return new PagedResult<AlertListItem>(items, total, pageIndex, pageSize);
    }
}