using Atlas.Application.Assets.Abstractions;
using Atlas.Application.Assets.Models;
using Atlas.Core.Abstractions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;

namespace Atlas.Infrastructure.Services;

public sealed class AssetQueryService : IAssetQueryService
{
    private readonly IIdGenerator _idGenerator;

    public AssetQueryService(IIdGenerator idGenerator)
    {
        _idGenerator = idGenerator;
    }

    public PagedResult<AssetListItem> QueryAssets(PagedRequest request, TenantId tenantId)
    {
        var total = 25;
        var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;
        var start = (pageIndex - 1) * pageSize;
        if (start >= total)
        {
            return new PagedResult<AssetListItem>(Array.Empty<AssetListItem>(), total, pageIndex, pageSize);
        }

        var count = Math.Min(pageSize, total - start);
        var items = Enumerable.Range(start, count)
            .Select(i => new AssetListItem(_idGenerator.NextId().ToString(), $"资产-{i + 1}"))
            .ToArray();

        return new PagedResult<AssetListItem>(items, total, pageIndex, pageSize);
    }
}