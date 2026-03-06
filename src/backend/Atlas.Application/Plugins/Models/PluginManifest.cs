namespace Atlas.Application.Plugins.Models;

/// <summary>
/// .atpkg 插件包的 manifest.json 结构定义。
/// </summary>
public sealed class PluginManifest
{
    /// <summary>插件代码（唯一标识）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>插件名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>版本号（SemVer）</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>作者</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>分类</summary>
    public string Category { get; set; } = "General";

    /// <summary>主程序集文件名（位于 lib/ 目录）</summary>
    public string Assembly { get; set; } = string.Empty;

    /// <summary>依赖声明</summary>
    public List<PluginManifestDependency> Dependencies { get; set; } = [];

    /// <summary>所需权限</summary>
    public List<string> RequiredPermissions { get; set; } = [];

    /// <summary>最低平台版本要求</summary>
    public string? MinPlatformVersion { get; set; }
}

public sealed class PluginManifestDependency
{
    public string Code { get; set; } = string.Empty;
    public string? MinVersion { get; set; }
    public string? MaxVersion { get; set; }
}
