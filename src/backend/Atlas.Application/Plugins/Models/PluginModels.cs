using Atlas.Core.Plugins;

namespace Atlas.Application.Plugins.Models;

public sealed record PluginDescriptor(
    string Code,
    string Name,
    string Version,
    string Description,
    string Author,
    string? IconUrl,
    PluginCategory Category,
    IReadOnlyList<PluginDependency> Dependencies,
    IReadOnlyList<string> RequiredPermissions,
    string? ConfigSchema,
    string AssemblyName,
    string FilePath,
    string State,
    DateTimeOffset LoadedAt,
    string? ErrorMessage);
