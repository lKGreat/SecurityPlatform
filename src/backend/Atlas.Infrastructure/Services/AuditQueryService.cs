using AutoMapper;
using Atlas.Application.Audit.Abstractions;
using Atlas.Application.Audit.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Audit.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Services;

public sealed class AuditQueryService : IAuditQueryService
{
    private readonly ISqlSugarClient _db;
    private readonly IMapper _mapper;

    public AuditQueryService(ISqlSugarClient db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public PagedResult<AuditListItem> QueryAudits(PagedRequest request, TenantId tenantId)
    {
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var total = 0;

        var query = _db.Queryable<AuditRecord>();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(x => x.Action.Contains(request.Keyword));
        }

        var items = query
            .OrderBy(x => x.OccurredAt, OrderByType.Desc)
            .ToPageList(pageIndex, pageSize, ref total)
            .Select(x => _mapper.Map<AuditListItem>(x))
            .ToArray();

        return new PagedResult<AuditListItem>(items, total, pageIndex, pageSize);
    }
}