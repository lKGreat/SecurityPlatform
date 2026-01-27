using Atlas.Application.Abstractions;
using Atlas.Application.Audit.Abstractions;
using Atlas.Application.Models;
using Atlas.Application.Options;
using Atlas.Core.Exceptions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.Domain.Audit.Entities;
using Atlas.Domain.Identity.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Atlas.Infrastructure.Services;

public sealed class JwtAuthTokenService : IAuthTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordPolicyOptions _passwordPolicy;
    private readonly LockoutPolicyOptions _lockoutPolicy;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;

    public JwtAuthTokenService(
        IOptions<JwtOptions> jwtOptions,
        IOptions<PasswordPolicyOptions> passwordPolicy,
        IOptions<LockoutPolicyOptions> lockoutPolicy,
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IAuditWriter auditWriter,
        TimeProvider timeProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _passwordPolicy = passwordPolicy.Value;
        _lockoutPolicy = lockoutPolicy.Value;
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
    }

    public async Task<AuthTokenResult> CreateTokenAsync(
        AuthTokenRequest request,
        TenantId tenantId,
        AuthRequestContext context,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var account = await _userAccountRepository.FindByUsernameAsync(tenantId, request.Username, cancellationToken);
        if (account is null)
        {
            await WriteAuditAsync(tenantId, request.Username, "LOGIN", "FAILED", null, context, cancellationToken);
            throw new BusinessException("用户名或密码错误", ErrorCodes.Unauthorized);
        }

        if (!account.IsActive)
        {
            await WriteAuditAsync(tenantId, request.Username, "LOGIN", "FAILED", null, context, cancellationToken);
            throw new BusinessException("账号已停用", ErrorCodes.Forbidden);
        }

        var locked = IsLocked(account, now, out var lockStateChanged);
        if (lockStateChanged)
        {
            await _userAccountRepository.UpdateAsync(account, cancellationToken);
        }

        if (locked)
        {
            await WriteAuditAsync(tenantId, request.Username, "LOGIN", "LOCKED", null, context, cancellationToken);
            throw new BusinessException("账号已锁定", ErrorCodes.AccountLocked);
        }

        var passwordExpiredAt = account.LastPasswordChangeAt.AddDays(_passwordPolicy.ExpirationDays);
        if (passwordExpiredAt <= now)
        {
            await WriteAuditAsync(tenantId, request.Username, "LOGIN", "PASSWORD_EXPIRED", null, context, cancellationToken);
            throw new BusinessException("密码已过期", ErrorCodes.PasswordExpired);
        }

        var passwordValid = _passwordHasher.VerifyHashedPassword(account.PasswordHash, request.Password);
        if (!passwordValid)
        {
            account.MarkLoginFailure(now, _lockoutPolicy.MaxFailedAttempts, TimeSpan.FromMinutes(_lockoutPolicy.LockoutMinutes));
            await _userAccountRepository.UpdateAsync(account, cancellationToken);
            await WriteAuditAsync(tenantId, request.Username, "LOGIN", "FAILED", null, context, cancellationToken);
            throw new BusinessException("用户名或密码错误", ErrorCodes.Unauthorized);
        }

        account.MarkLoginSuccess(now);
        await _userAccountRepository.UpdateAsync(account, cancellationToken);
        await WriteAuditAsync(tenantId, request.Username, "LOGIN", "SUCCESS", null, context, cancellationToken);

        var expires = now.AddMinutes(_jwtOptions.ExpiresMinutes);
        var claims = BuildClaims(account, tenantId);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var tokenString = handler.WriteToken(token);
        return new AuthTokenResult(tokenString, expires);
    }

    private static List<Claim> BuildClaims(UserAccount account, TenantId tenantId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Username),
            new("tenant_id", tenantId.Value.ToString("D"))
        };

        if (!string.IsNullOrWhiteSpace(account.Roles))
        {
            var roles = account.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        return claims;
    }

    private bool IsLocked(UserAccount account, DateTimeOffset now, out bool stateChanged)
    {
        stateChanged = false;
        if (account.IsManualLocked && account.ManualLockAt.HasValue)
        {
            var autoUnlockAt = account.ManualLockAt.Value.AddMinutes(_lockoutPolicy.AutoUnlockMinutes);
            if (now >= autoUnlockAt)
            {
                account.Unlock();
                stateChanged = true;
                return false;
            }

            return true;
        }

        if (account.LockoutEndAt.HasValue)
        {
            if (now >= account.LockoutEndAt.Value)
            {
                account.Unlock();
                stateChanged = true;
                return false;
            }

            return true;
        }

        return false;
    }

    private Task WriteAuditAsync(
        TenantId tenantId,
        string actor,
        string action,
        string result,
        string? target,
        AuthRequestContext context,
        CancellationToken cancellationToken)
    {
        var record = new AuditRecord(tenantId, actor, action, result, target, context.IpAddress, context.UserAgent);
        return _auditWriter.WriteAsync(record, cancellationToken);
    }
}
