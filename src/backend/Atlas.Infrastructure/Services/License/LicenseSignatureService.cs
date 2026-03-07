using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Application.License.Abstractions;
using Atlas.Application.License.Models;
using Microsoft.Extensions.Logging;

namespace Atlas.Infrastructure.Services.License;

/// <summary>
/// 证书签名验证服务：用内嵌的 ECDSA P-256 公钥验证证书签名。
/// 私钥仅存在于颁发工具（Atlas.LicenseIssuer），平台只有公钥。
/// </summary>
public sealed class LicenseSignatureService : ILicenseSignatureService
{
    // 内嵌公钥（PEM 格式）。颁发工具生成密钥对后，将公钥嵌入此处。
    // 使用占位公钥，正式发布时替换为实际公钥。
    private const string EmbeddedPublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEPLAeG/ADI5HmfBGNNY3Y7v0bS5zx
        Xo7Fp3tGKY6sU5Y+RJnGNMvY0Gh+wXMpq3DhKnF+sDqFZmKR7v0HdLPZpA==
        -----END PUBLIC KEY-----
        """;

    private readonly ILogger<LicenseSignatureService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public LicenseSignatureService(ILogger<LicenseSignatureService> logger)
    {
        _logger = logger;
    }

    public LicenseEnvelope? Parse(string rawContent)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(rawContent.Trim()));
            return JsonSerializer.Deserialize<LicenseEnvelope>(decoded, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "证书解析失败");
            return null;
        }
    }

    public bool Verify(LicenseEnvelope envelope)
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(EmbeddedPublicKeyPem);

            var payloadJson = JsonSerializer.Serialize(envelope.Payload, _jsonOptions);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var signatureBytes = Convert.FromBase64String(envelope.Signature);

            return ecdsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "证书签名验证失败");
            return false;
        }
    }
}
