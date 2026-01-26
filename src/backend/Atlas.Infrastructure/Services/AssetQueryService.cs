using AutoMapper;
using Atlas.Application.Assets.Abstractions;
using Atlas.Application.Assets.Models;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Assets.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Services;

public sealed class AssetQueryService : IAssetQueryService
{
    private readonly ISqlSugarClient _db;
    private readonly IMapper _mapper;

    public AssetQueryService(ISqlSugarClient db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public PagedResult<AssetListItem> QueryAssets(PagedRequest request, TenantId tenantId)
    {
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var total = 0;

        var query = _db.Queryable<Asset>();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = query.Where(x => x.Name.Contains(request.Keyword));
        }

        var items = query
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToPageList(pageIndex, pageSize, ref total)
            .Select(x => _mapper.Map<AssetListItem>(x))
            .ToArray();

        return new PagedResult<AssetListItem>(items, total, pageIndex, pageSize);
    }
}