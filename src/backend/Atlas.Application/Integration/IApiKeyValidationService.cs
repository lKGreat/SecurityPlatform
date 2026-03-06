using Atlas.Core.Tenancy;

namespace Atlas.Application.Integration;

/// <summary>
/// 验证外部集成 API Key 是否有效
/// </summary>
public interface IApiKeyValidationService
{
    /// <summary>
    /// 验证给定租户下的 API Key 是否合法（存在、激活、未过期、哈希匹配）。
    /// 验证通过后自动更新 LastUsedAt。
    /// </summary>
    /// <param name="tenantId">目标租户 ID</param>
    /// <param name="rawApiKey">请求头中传入的明文 API Key</param>
    /// <param name="requiredScope">可选：要求该 Key 具备的权限范围（为 null 则不校验 Scope）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证通过返回 true，否则 false</returns>
    Task<bool> ValidateAsync(TenantId tenantId, string rawApiKey, string? requiredScope, CancellationToken cancellationToken);
}
