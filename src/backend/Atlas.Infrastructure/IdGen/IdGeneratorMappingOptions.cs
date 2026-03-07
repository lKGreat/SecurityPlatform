namespace Atlas.Infrastructure.IdGen;

public sealed class IdGeneratorMappingOptions
{
    public string DefaultAppId { get; init; } = "SecurityPlatform";

    /// <summary>
    /// 未匹配到租户+应用组合时的回退 GeneratorId（0-1023）。
    /// 开发/单节点环境设为 1 即可；多节点生产环境应在 Mappings 中明确配置每个节点。
    /// </summary>
    public int? FallbackGeneratorId { get; init; }

    public List<IdGeneratorMapping> Mappings { get; init; } = new();
}

public sealed record IdGeneratorMapping(string TenantId, string AppId, int GeneratorId);
