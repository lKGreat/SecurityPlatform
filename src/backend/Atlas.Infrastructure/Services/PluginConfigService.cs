using System.Text.Json;
using System.Text.Json.Nodes;
using Atlas.Application.Plugins.Abstractions;
using Atlas.Application.Plugins.Repositories;
using Atlas.Core.Abstractions;
using Atlas.Domain.Plugins;

namespace Atlas.Infrastructure.Services;

/// <summary>
/// 插件配置服务实现：按 Global &lt; Tenant &lt; App 三级优先级合并 JSON 配置。
/// </summary>
public sealed class PluginConfigService : IPluginConfigService
{
    private readonly IPluginConfigRepository _repository;
    private readonly IIdGeneratorAccessor _idGen;

    public PluginConfigService(IPluginConfigRepository repository, IIdGeneratorAccessor idGen)
    {
        _repository = repository;
        _idGen = idGen;
    }

    public async Task<string> GetMergedConfigAsync(
        string pluginCode,
        string? tenantId,
        string? appId,
        CancellationToken cancellationToken)
    {
        var allConfigs = await _repository.GetByPluginCodeAsync(pluginCode, cancellationToken);

        // 按优先级顺序：Global(0) < Tenant(1) < App(2)
        var merged = new JsonObject();

        // 1. Global
        var global = allConfigs.FirstOrDefault(c => c.Scope == PluginConfigScope.Global);
        if (global is not null)
        {
            MergeInto(merged, global.ConfigJson);
        }

        // 2. Tenant
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var tenant = allConfigs.FirstOrDefault(c => c.Scope == PluginConfigScope.Tenant && c.ScopeId == tenantId);
            if (tenant is not null)
            {
                MergeInto(merged, tenant.ConfigJson);
            }
        }

        // 3. App
        if (!string.IsNullOrWhiteSpace(appId))
        {
            var app = allConfigs.FirstOrDefault(c => c.Scope == PluginConfigScope.App && c.ScopeId == appId);
            if (app is not null)
            {
                MergeInto(merged, app.ConfigJson);
            }
        }

        return merged.ToJsonString();
    }

    public async Task SaveConfigAsync(
        string pluginCode,
        PluginConfigScope scope,
        string? scopeId,
        string configJson,
        CancellationToken cancellationToken)
    {
        var config = new PluginConfig
        {
            PluginCode = pluginCode,
            Scope = scope,
            ScopeId = scopeId,
            ConfigJson = configJson
        };

        await _repository.UpsertAsync(config, cancellationToken);
    }

    public async Task DeleteConfigAsync(
        string pluginCode,
        PluginConfigScope scope,
        string? scopeId,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.FindAsync(pluginCode, scope, scopeId, cancellationToken);
        if (existing is not null)
        {
            await _repository.DeleteAsync(existing.Id, cancellationToken);
        }
    }

    private static void MergeInto(JsonObject target, string sourceJson)
    {
        try
        {
            var source = JsonNode.Parse(sourceJson) as JsonObject;
            if (source is null)
            {
                return;
            }

            foreach (var prop in source)
            {
                target[prop.Key] = prop.Value?.DeepClone();
            }
        }
        catch (JsonException)
        {
            // 忽略无效 JSON
        }
    }
}
