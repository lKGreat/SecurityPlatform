namespace Atlas.Domain.Plugins;

/// <summary>
/// 插件市场版本历史
/// </summary>
public sealed class PluginMarketVersion
{
    public long Id { get; set; }
    public long EntryId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public string? PackageHash { get; set; }
    public string? PackageUrl { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}
