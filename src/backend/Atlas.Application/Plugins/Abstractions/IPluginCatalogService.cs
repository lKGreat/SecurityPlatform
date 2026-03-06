using Atlas.Application.Plugins.Models;

namespace Atlas.Application.Plugins.Abstractions;

public interface IPluginCatalogService
{
    Task<IReadOnlyList<PluginDescriptor>> GetPluginsAsync(CancellationToken cancellationToken);

    Task ReloadAsync(CancellationToken cancellationToken);

    /// <summary>启用插件（加载并激活）</summary>
    Task EnableAsync(string pluginCode, CancellationToken cancellationToken);

    /// <summary>禁用插件（停止分发事件，但保留已加载实例）</summary>
    Task DisableAsync(string pluginCode, CancellationToken cancellationToken);

    /// <summary>卸载插件（卸载 AssemblyLoadContext，释放资源）</summary>
    Task UnloadAsync(string pluginCode, CancellationToken cancellationToken);

    /// <summary>从包安装插件（复制到插件目录后触发重载）</summary>
    Task InstallFromPackageAsync(string packagePath, CancellationToken cancellationToken);
}
