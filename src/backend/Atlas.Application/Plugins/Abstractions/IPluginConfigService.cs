using Atlas.Domain.Plugins;

namespace Atlas.Application.Plugins.Abstractions;

/// <summary>
/// 插件配置服务接口，支持 Global &lt; Tenant &lt; App 三级优先级合并。
/// </summary>
public interface IPluginConfigService
{
    /// <summary>
    /// 按优先级合并获取插件配置（App > Tenant > Global）。
    /// 返回合并后的 JSON 字符串。
    /// </summary>
    Task<string> GetMergedConfigAsync(
        string pluginCode,
        string? tenantId,
        string? appId,
        CancellationToken cancellationToken);

    /// <summary>保存指定 Scope 的插件配置</summary>
    Task SaveConfigAsync(
        string pluginCode,
        PluginConfigScope scope,
        string? scopeId,
        string configJson,
        CancellationToken cancellationToken);

    /// <summary>删除指定 Scope 的插件配置</summary>
    Task DeleteConfigAsync(
        string pluginCode,
        PluginConfigScope scope,
        string? scopeId,
        CancellationToken cancellationToken);
}
