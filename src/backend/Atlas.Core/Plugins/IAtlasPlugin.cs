namespace Atlas.Core.Plugins;

/// <summary>
/// Atlas 插件入口约定。
/// 插件通过实现此接口声明元数据，并通过 AssemblyLoadContext 按需加载/卸载。
/// </summary>
public interface IAtlasPlugin
{
    string Code { get; }
    string Name { get; }
    string Version { get; }

    /// <summary>插件描述（可选）</summary>
    string Description => string.Empty;

    /// <summary>作者/发布者（可选）</summary>
    string Author => string.Empty;

    /// <summary>图标 URL（可选）</summary>
    string? IconUrl => null;

    /// <summary>插件分类</summary>
    PluginCategory Category => PluginCategory.General;

    /// <summary>插件依赖声明（其他插件 Code + 版本范围）</summary>
    IReadOnlyList<PluginDependency> Dependencies => [];

    /// <summary>插件所需权限代码（加载时自动校验）</summary>
    IReadOnlyList<string> RequiredPermissions => [];

    /// <summary>插件配置 Schema（JSON Schema 字符串，可选）</summary>
    string? ConfigSchema => null;

    Task OnLoadedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task OnUnloadingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

/// <summary>插件分类</summary>
public enum PluginCategory
{
    /// <summary>通用/未分类</summary>
    General = 0,

    /// <summary>字段类型扩展</summary>
    FieldType = 1,

    /// <summary>校验器</summary>
    Validator = 2,

    /// <summary>数据源连接器</summary>
    DataSource = 3,

    /// <summary>审批流节点</summary>
    FlowNode = 4,

    /// <summary>表格/Grid 渲染器</summary>
    GridRenderer = 5,

    /// <summary>主题/样式</summary>
    Theme = 6,

    /// <summary>集成/Webhook 处理器</summary>
    Integration = 7
}

/// <summary>插件依赖声明</summary>
public sealed record PluginDependency(
    string Code,
    string? MinVersion = null,
    string? MaxVersion = null);

