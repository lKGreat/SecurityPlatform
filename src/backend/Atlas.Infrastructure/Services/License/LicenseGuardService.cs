using Atlas.Application.License.Abstractions;
using Atlas.Application.License.Models;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Domain.License;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.License;

/// <summary>
/// 授权门控服务：实现 ILicenseService，缓存当前授权状态并提供功能/限额检查。
/// 注册为 Singleton，激活证书后调用 ReloadAsync 刷新缓存。
/// </summary>
public sealed class LicenseGuardService : ILicenseService
{
    private volatile LicenseStatusDto _currentStatus = LicenseStatusDto.None();
    private readonly ILicenseRepository _repository;
    private readonly IMachineFingerprintService _fingerprintService;
    private readonly ILogger<LicenseGuardService> _logger;

    public LicenseGuardService(
        ILicenseRepository repository,
        IMachineFingerprintService fingerprintService,
        ILogger<LicenseGuardService> logger)
    {
        _repository = repository;
        _fingerprintService = fingerprintService;
        _logger = logger;
    }

    public LicenseStatusDto GetCurrentStatus() => _currentStatus;

    public bool IsFeatureEnabled(string feature)
    {
        var status = _currentStatus;
        if (status.Status != "Active")
            return false;

        return status.Features.TryGetValue(feature, out var enabled) && enabled;
    }

    public int GetLimit(string limitKey)
    {
        var status = _currentStatus;
        if (status.Status != "Active")
            return 0;

        return status.Limits.TryGetValue(limitKey, out var limit) ? limit : -1;
    }

    public void EnsureWithinLimit(string limitKey, int currentCount)
    {
        var limit = GetLimit(limitKey);
        if (limit < 0)
            return; // -1 表示不限制

        if (currentCount >= limit)
        {
            throw new BusinessException(
                $"已达到授权限额：{limitKey} = {limit}，当前已有 {currentCount} 条记录",
                ErrorCodes.Forbidden);
        }
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await _repository.GetActiveAsync(cancellationToken);
            if (record is null)
            {
                _currentStatus = LicenseStatusDto.None();
                _logger.LogInformation("未找到有效授权证书");
                return;
            }

            var now = DateTimeOffset.UtcNow;

            // 检查是否过期
            if (!record.IsPermanent && record.ExpiresAt.HasValue && now > record.ExpiresAt.Value)
            {
                _currentStatus = BuildExpiredStatus(record);
                _logger.LogWarning("授权证书已过期：{ExpiresAt}", record.ExpiresAt);
                return;
            }

            var machineMatched = _fingerprintService.Matches(record.MachineFingerprintHash);
            var remainingDays = CalculateRemainingDays(record, now);

            _currentStatus = new LicenseStatusDto(
                "Active",
                record.Edition.ToString(),
                record.IsPermanent,
                record.IssuedAt,
                record.ExpiresAt,
                remainingDays,
                machineMatched,
                BuildFeaturesForEdition(record.Edition),
                BuildLimitsForEdition(record.Edition));

            _logger.LogInformation("授权证书加载成功：{Edition}，{Status}",
                record.Edition, record.IsPermanent ? "永久" : $"到期日：{record.ExpiresAt:yyyy-MM-dd}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载授权证书失败");
            _currentStatus = LicenseStatusDto.None();
        }
    }

    private static LicenseStatusDto BuildExpiredStatus(LicenseRecord record) =>
        new(
            "Expired",
            record.Edition.ToString(),
            record.IsPermanent,
            record.IssuedAt,
            record.ExpiresAt,
            0,
            false,
            new Dictionary<string, bool>(),
            new Dictionary<string, int>());

    private static int? CalculateRemainingDays(LicenseRecord record, DateTimeOffset now)
    {
        if (record.IsPermanent || !record.ExpiresAt.HasValue)
            return null;

        var remaining = (int)(record.ExpiresAt.Value - now).TotalDays;
        return Math.Max(0, remaining);
    }

    private static IReadOnlyDictionary<string, bool> BuildFeaturesForEdition(LicenseEdition edition)
    {
        return edition switch
        {
            LicenseEdition.Trial => new Dictionary<string, bool>
            {
                ["lowCode"] = true,
                ["workflow"] = false,
                ["approval"] = false,
                ["alert"] = false,
                ["offlineDeploy"] = false,
                ["multiTenant"] = false,
                ["audit"] = true,
            },
            LicenseEdition.Pro => new Dictionary<string, bool>
            {
                ["lowCode"] = true,
                ["workflow"] = true,
                ["approval"] = true,
                ["alert"] = true,
                ["offlineDeploy"] = true,
                ["multiTenant"] = true,
                ["audit"] = true,
            },
            LicenseEdition.Enterprise => new Dictionary<string, bool>
            {
                ["lowCode"] = true,
                ["workflow"] = true,
                ["approval"] = true,
                ["alert"] = true,
                ["offlineDeploy"] = true,
                ["multiTenant"] = true,
                ["audit"] = true,
            },
            _ => new Dictionary<string, bool>()
        };
    }

    private static IReadOnlyDictionary<string, int> BuildLimitsForEdition(LicenseEdition edition)
    {
        return edition switch
        {
            LicenseEdition.Trial => new Dictionary<string, int>
            {
                ["maxApps"] = 3,
                ["maxUsers"] = 10,
                ["maxTenants"] = 1,
                ["maxDataSources"] = 2,
                ["auditRetentionDays"] = 7,
            },
            LicenseEdition.Pro => new Dictionary<string, int>
            {
                ["maxApps"] = 20,
                ["maxUsers"] = 500,
                ["maxTenants"] = 5,
                ["maxDataSources"] = 10,
                ["auditRetentionDays"] = 180,
            },
            LicenseEdition.Enterprise => new Dictionary<string, int>
            {
                ["maxApps"] = -1,
                ["maxUsers"] = -1,
                ["maxTenants"] = -1,
                ["maxDataSources"] = -1,
                ["auditRetentionDays"] = 365,
            },
            _ => new Dictionary<string, int>()
        };
    }
}
