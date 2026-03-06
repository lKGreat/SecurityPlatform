using Atlas.Application.Plugins.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Domain.Plugins;
using SqlSugar;

namespace Atlas.Infrastructure.Repositories;

public sealed class PluginConfigRepository : IPluginConfigRepository
{
    private readonly ISqlSugarClient _db;
    private readonly IIdGeneratorAccessor _idGen;

    public PluginConfigRepository(ISqlSugarClient db, IIdGeneratorAccessor idGen)
    {
        _db = db;
        _idGen = idGen;
    }

    public async Task<PluginConfig?> FindAsync(
        string pluginCode, PluginConfigScope scope, string? scopeId, CancellationToken cancellationToken)
    {
        return await _db.Queryable<PluginConfig>()
            .Where(c => c.PluginCode == pluginCode && c.Scope == scope && c.ScopeId == scopeId)
            .FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PluginConfig>> GetByPluginCodeAsync(
        string pluginCode, CancellationToken cancellationToken)
    {
        return await _db.Queryable<PluginConfig>()
            .Where(c => c.PluginCode == pluginCode)
            .OrderBy(c => c.Scope)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(PluginConfig config, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(config.PluginCode, config.Scope, config.ScopeId, cancellationToken);
        if (existing is null)
        {
            config.Id = _idGen.Generator.NextId();
            config.CreatedAt = DateTimeOffset.UtcNow;
            config.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.Insertable(config).ExecuteCommandAsync(cancellationToken);
        }
        else
        {
            existing.ConfigJson = config.ConfigJson;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.Updateable(existing)
                .Where(c => c.Id == existing.Id)
                .ExecuteCommandAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        await _db.Deleteable<PluginConfig>()
            .Where(c => c.Id == id)
            .ExecuteCommandAsync(cancellationToken);
    }
}
