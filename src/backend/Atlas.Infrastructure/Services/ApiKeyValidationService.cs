using Atlas.Application.Abstractions;
using Atlas.Application.Integration;
using Atlas.Core.Tenancy;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services;

/// <summary>
/// 验证外部集成 API Key：从数据库加载有效记录，逐一做 PBKDF2 哈希比对，通过后更新 LastUsedAt。
/// </summary>
public sealed class ApiKeyValidationService : IApiKeyValidationService
{
    private readonly IIntegrationApiKeyRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ApiKeyValidationService> _logger;

    public ApiKeyValidationService(
        IIntegrationApiKeyRepository repository,
        IPasswordHasher passwordHasher,
        ILogger<ApiKeyValidationService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(
        TenantId tenantId,
        string rawApiKey,
        string? requiredScope,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return false;
        }

        var activeKeys = await _repository.GetActiveByTenantAsync(tenantId, cancellationToken);

        foreach (var record in activeKeys)
        {
            if (!_passwordHasher.VerifyHashedPassword(record.KeyHash, rawApiKey))
            {
                continue;
            }

            // 验证通过，可选检查 Scope
            if (requiredScope is not null
                && !record.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("API Key (id={Id}) 缺少所需权限范围 '{Scope}'", record.Id, requiredScope);
                return false;
            }

            // 异步更新最后使用时间，不阻塞响应；使用 CancellationToken.None 避免请求取消信号中断后台写入
            _ = UpdateLastUsedAtSilentlyAsync(record.Id);
            return true;
        }

        _logger.LogWarning("租户 {TenantId} 的 API Key 验证失败", tenantId.Value);
        return false;
    }

    private async Task UpdateLastUsedAtSilentlyAsync(long id)
    {
        try
        {
            await _repository.UpdateLastUsedAtAsync(id, DateTimeOffset.UtcNow, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 API Key LastUsedAt 失败（id={Id}）", id);
        }
    }
}
