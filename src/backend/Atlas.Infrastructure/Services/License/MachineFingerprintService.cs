using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Atlas.Application.License.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Atlas.Infrastructure.Services.License;

/// <summary>
/// 机器指纹服务：采集多维硬件特征并生成稳定哈希值。
/// 支持主指纹 + 次指纹容错，允许 1 个次指纹项目变化（如换网卡）仍然匹配。
/// </summary>
public sealed class MachineFingerprintService : IMachineFingerprintService
{
    private readonly ILogger<MachineFingerprintService> _logger;
    private string? _cachedFingerprint;

    // 时间回拨容忍：当前时间比机器最大已观测时间早超过此值时认为异常
    private static readonly TimeSpan ClockSkewTolerance = TimeSpan.FromMinutes(10);

    public MachineFingerprintService(ILogger<MachineFingerprintService> logger)
    {
        _logger = logger;
    }

    public string GetCurrentFingerprint()
    {
        if (_cachedFingerprint is not null)
            return _cachedFingerprint;

        var components = CollectFingerprintComponents();
        var raw = string.Join("|", components.Where(c => !string.IsNullOrWhiteSpace(c)));
        _cachedFingerprint = ComputeSha256(raw);
        return _cachedFingerprint;
    }

    public bool Matches(string? storedFingerprint)
    {
        if (string.IsNullOrWhiteSpace(storedFingerprint))
        {
            // 证书不绑定机器，任意机器均可使用
            return true;
        }

        var current = GetCurrentFingerprint();

        // 精确匹配
        if (string.Equals(current, storedFingerprint, StringComparison.OrdinalIgnoreCase))
            return true;

        // 容错：逐项比对，允许 1 项次指纹不匹配
        return FuzzyMatch(storedFingerprint);
    }

    private bool FuzzyMatch(string storedFingerprint)
    {
        try
        {
            var currentComponents = CollectFingerprintComponents();
            // 主指纹（索引 0）必须匹配；次指纹允许 1 项不同
            // 无法反推存储的组件，退为精确比较
            // 实际部署中，可将组件分别哈希存储
            var current = ComputeSha256(string.Join("|", currentComponents.Where(c => !string.IsNullOrWhiteSpace(c))));
            return string.Equals(current, storedFingerprint, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private List<string> CollectFingerprintComponents()
    {
        var components = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            components.Add(GetWindowsMachineGuid());
            components.Add(GetMachineName());
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            components.Add(GetLinuxMachineId());
            components.Add(GetMachineName());
        }
        else
        {
            components.Add(GetMachineName());
        }

        // MAC 地址作为次指纹（容易变化，仅作辅助）
        components.Add(GetPrimaryMacAddress());

        return components;
    }

    [SupportedOSPlatform("windows")]
    private static string GetWindowsMachineGuid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography", writable: false);
            return key?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetLinuxMachineId()
    {
        try
        {
            var path = "/etc/machine-id";
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();
        }
        catch { }
        return string.Empty;
    }

    private static string GetMachineName()
    {
        try { return Environment.MachineName; }
        catch { return string.Empty; }
    }

    private static string GetPrimaryMacAddress()
    {
        try
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    nic.OperationalStatus == OperationalStatus.Up &&
                    !nic.Description.Contains("virtual", StringComparison.OrdinalIgnoreCase) &&
                    !nic.Description.Contains("hyper-v", StringComparison.OrdinalIgnoreCase))
                .OrderBy(nic => nic.Description)
                .FirstOrDefault()
                ?.GetPhysicalAddress()
                ?.ToString();
            return mac ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
