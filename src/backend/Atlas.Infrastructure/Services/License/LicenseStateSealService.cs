using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Application.License.Abstractions;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.License;

/// <summary>
/// 本地激活状态密封服务。
/// Windows 使用 DPAPI（机器级别），可防止整目录复制到其他机器后仍能读取状态。
/// Linux 使用 AES-256-GCM（机器 ID 派生密钥），提供类似的绑定效果。
/// 存储路径：%ProgramData%/Atlas/license.state（不在应用目录）。
/// </summary>
public sealed class LicenseStateSealService : ILicenseStateSealService
{
    private static readonly string StateFilePath = GetStateFilePath();
    private readonly ILogger<LicenseStateSealService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LicenseStateSealService(ILogger<LicenseStateSealService> logger)
    {
        _logger = logger;
    }

    public void Seal(LicenseSealedState state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var encrypted = Encrypt(plainBytes);

            var dir = Path.GetDirectoryName(StateFilePath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(StateFilePath, encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存本地激活状态失败，路径：{Path}", StateFilePath);
        }
    }

    public LicenseSealedState? Unseal()
    {
        try
        {
            if (!File.Exists(StateFilePath))
                return null;

            var encrypted = File.ReadAllBytes(StateFilePath);
            var plainBytes = Decrypt(encrypted);
            if (plainBytes is null)
                return null;

            var json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<LicenseSealedState>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取本地激活状态失败，路径：{Path}", StateFilePath);
            return null;
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(StateFilePath))
                File.Delete(StateFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清除本地激活状态失败");
        }
    }

    private static byte[] Encrypt(byte[] plainBytes)
    {
        if (OperatingSystem.IsWindows())
        {
            // DPAPI 机器级别加密，绑定当前机器
            return EncryptDpapi(plainBytes);
        }

        return EncryptAesGcm(plainBytes);
    }

    private static byte[]? Decrypt(byte[] cipherBytes)
    {
        if (OperatingSystem.IsWindows())
        {
            return DecryptDpapi(cipherBytes);
        }

        return DecryptAesGcm(cipherBytes);
    }

    [SupportedOSPlatform("windows")]
    private static byte[] EncryptDpapi(byte[] plainBytes)
    {
        return ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);
    }

    [SupportedOSPlatform("windows")]
    private static byte[]? DecryptDpapi(byte[] cipherBytes)
    {
        try
        {
            return ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.LocalMachine);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] EncryptAesGcm(byte[] plainBytes)
    {
        var key = DeriveLinuxKey();
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        var ciphertext = new byte[plainBytes.Length];

        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, ciphertext, tag);

        // 格式：nonce(12) + tag(16) + ciphertext
        var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);
        return result;
    }

    private static byte[]? DecryptAesGcm(byte[] data)
    {
        try
        {
            const int nonceLen = 12;
            const int tagLen = 16;
            if (data.Length < nonceLen + tagLen)
                return null;

            var key = DeriveLinuxKey();
            var nonce = data[..nonceLen];
            var tag = data[nonceLen..(nonceLen + tagLen)];
            var ciphertext = data[(nonceLen + tagLen)..];
            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, tagLen);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] DeriveLinuxKey()
    {
        // Linux 下使用 /etc/machine-id 或主机名派生密钥
        string seed;
        try
        {
            seed = File.Exists("/etc/machine-id")
                ? File.ReadAllText("/etc/machine-id").Trim()
                : Environment.MachineName;
        }
        catch
        {
            seed = Environment.MachineName;
        }

        seed += "|atlas-license-seal-v1";
        return SHA256.HashData(Encoding.UTF8.GetBytes(seed));
    }

    private static string GetStateFilePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, "Atlas", "license.state");
        }

        return Path.Combine("/var/lib/atlas", "license.state");
    }
}
