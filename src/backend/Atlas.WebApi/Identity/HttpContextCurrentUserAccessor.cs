using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Atlas.Core.Identity;
using Atlas.Core.Tenancy;
using Atlas.WebApi.Helpers;

namespace Atlas.WebApi.Identity;

public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private const string DisplayNameClaimType = "display_name";
    private const string SessionIdClaimType = "sid";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantProvider _tenantProvider;

    public HttpContextCurrentUserAccessor(
        IHttpContextAccessor httpContextAccessor,
        ITenantProvider tenantProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantProvider = tenantProvider;
    }

    public CurrentUserInfo? GetCurrentUser()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = ControllerHelper.GetUserIdSafely(user);
        if (!userId.HasValue)
        {
            return null;
        }

        var username = user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? string.Empty;
        var displayName = user.FindFirstValue(DisplayNameClaimType) ?? username;
        var tenantId = _tenantProvider.GetTenantId();
        var roles = user.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sessionId = 0L;
        var sessionRaw = user.FindFirstValue(SessionIdClaimType);
        if (!string.IsNullOrWhiteSpace(sessionRaw) && long.TryParse(sessionRaw, out var parsed))
        {
            sessionId = parsed;
        }

        return new CurrentUserInfo(userId.Value, username, displayName, tenantId, roles, sessionId);
    }

    public CurrentUserInfo GetCurrentUserOrThrow()
    {
        var currentUser = GetCurrentUser();
        if (currentUser is null)
        {
            throw new UnauthorizedAccessException("未登录或无法解析当前用户信息。");
        }

        return currentUser;
    }
}
