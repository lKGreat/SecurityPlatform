using Atlas.Application.Abstractions;
using Atlas.Application.Models;
using Atlas.Application.Options;
using Atlas.Core.Tenancy;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Atlas.Infrastructure.Services;

public sealed class JwtAuthTokenService : IAuthTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAuthTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public AuthTokenResult CreateToken(AuthTokenRequest request, TenantId tenantId)
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_options.ExpiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new("tenant_id", tenantId.Value.ToString("D"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(token);
        return new AuthTokenResult(tokenString, expires);
    }
}