using Atlas.Domain.Plugins;

namespace Atlas.Application.Plugins.Repositories;

/// <summary>
/// 插件配置仓储接口
/// </summary>
public interface IPluginConfigRepository
{
    Task<PluginConfig?> FindAsync(string pluginCode, PluginConfigScope scope, string? scopeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PluginConfig>> GetByPluginCodeAsync(string pluginCode, CancellationToken cancellationToken);
    Task UpsertAsync(PluginConfig config, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
