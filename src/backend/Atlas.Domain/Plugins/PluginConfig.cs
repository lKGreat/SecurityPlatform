namespace Atlas.Domain.Plugins;

/// <summary>
/// 插件配置实体，支持 Global/Tenant/App 三级 Scope 分层覆盖。
/// </summary>
public sealed class PluginConfig
{
    public long Id { get; set; }

    /// <summary>插件代码</summary>
    public string PluginCode { get; set; } = string.Empty;

    /// <summary>配置作用域</summary>
    public PluginConfigScope Scope { get; set; }

    /// <summary>作用域 ID（Tenant/App 作用域时为 TenantId/AppId，Global 时为 null）</summary>
    public string? ScopeId { get; set; }

    /// <summary>配置内容（JSON）</summary>
    public string ConfigJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum PluginConfigScope
{
    /// <summary>全局默认配置</summary>
    Global = 0,

    /// <summary>租户级配置（覆盖全局）</summary>
    Tenant = 1,

    /// <summary>应用级配置（覆盖租户）</summary>
    App = 2
}
