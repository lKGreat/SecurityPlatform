using Atlas.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Atlas.Infrastructure.Security;

/// <summary>
/// OIDC 配置选项
/// </summary>
public sealed class OidcOptions
{
    public bool Enabled { get; init; }
    public string Authority { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = ["openid", "profile", "email"];
    public string CallbackPath { get; init; } = "/auth/oidc/callback";

    /// <summary>OIDC claim -> Atlas role 映射规则（claim value 前缀匹配）</summary>
    public Dictionary<string, string> RoleClaimMapping { get; init; } = new();
}
