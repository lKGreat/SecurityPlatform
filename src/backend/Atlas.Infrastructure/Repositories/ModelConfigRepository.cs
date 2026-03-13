using Atlas.Core.Tenancy;
using Atlas.Domain.AiPlatform.Entities;

namespace Atlas.Infrastructure.Repositories;

public sealed class ModelConfigRepository : RepositoryBase<ModelConfig>
{
    public ModelConfigRepository(SqlSugar.ISqlSugarClient db)
        : base(db)
    {
    }

    public async Task<(List<ModelConfig> Items, long Total)> GetPagedAsync(
        TenantId tenantId,
        string? keyword,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = BuildFilteredQuery(tenantId, keyword);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Id, SqlSugar.OrderByType.Desc)
            .ToPageListAsync(pageIndex, pageSize, cancellationToken);

        return (items, total);
    }

    public async Task<(long Total, long Enabled, long EmbeddingCount)> GetStatsAsync(
        TenantId tenantId,
        string? keyword,
        CancellationToken cancellationToken)
    {
        var total = await BuildFilteredQuery(tenantId, keyword).CountAsync(cancellationToken);
        var enabled = await BuildFilteredQuery(tenantId, keyword)
            .Where(x => x.IsEnabled)
            .CountAsync(cancellationToken);
        var embeddingCount = await BuildFilteredQuery(tenantId, keyword)
            .Where(x => x.SupportsEmbedding)
            .CountAsync(cancellationToken);

        return (total, enabled, embeddingCount);
    }

    public async Task<List<ModelConfig>> GetAllEnabledAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return await Db.Queryable<ModelConfig>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.IsEnabled)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ModelConfig?> FindByNameAsync(TenantId tenantId, string name, CancellationToken cancellationToken)
    {
        return await Db.Queryable<ModelConfig>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Name == name)
            .FirstAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(TenantId tenantId, string name, CancellationToken cancellationToken)
    {
        var count = await Db.Queryable<ModelConfig>()
            .Where(x => x.TenantIdValue == tenantId.Value && x.Name == name)
            .CountAsync(cancellationToken);
        return count > 0;
    }

    private SqlSugar.ISugarQueryable<ModelConfig> BuildFilteredQuery(TenantId tenantId, string? keyword)
    {
        var query = Db.Queryable<ModelConfig>()
            .Where(x => x.TenantIdValue == tenantId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.ProviderType.Contains(keyword) ||
                x.DefaultModel.Contains(keyword));
        }

        return query;
    }
}
