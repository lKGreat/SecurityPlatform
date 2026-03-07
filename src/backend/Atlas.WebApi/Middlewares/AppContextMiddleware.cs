using System;
using System.Security.Claims;
using Atlas.Core.Identity;
using Atlas.WebApi.Helpers;
using Atlas.WebApi.Identity;
using Microsoft.Extensions.Options;

namespace Atlas.WebApi.Middlewares;

public sealed class AppContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppOptions _options;

    public AppContextMiddleware(RequestDelegate next, IOptions<AppOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (!context.Items.ContainsKey(HttpContextAppContextAccessor.AppIdItemKey))
        {
            var claimAppId = TryResolveAppIdFromClaims(context.User);
            var headerAppId = TryResolveAppIdFromHeader(context);

            if (context.User?.Identity?.IsAuthenticated == true)
            {
                if (_options.AllowHeaderOverrideWhenAuthenticated && !string.IsNullOrWhiteSpace(headerAppId))
                {
                    context.Items[HttpContextAppContextAccessor.AppIdItemKey] = headerAppId;
                    return _next(context);
                }

                if (!string.IsNullOrWhiteSpace(claimAppId))
                {
                    context.Items[HttpContextAppContextAccessor.AppIdItemKey] = claimAppId;
                    return _next(context);
                }
            }
            else if (!string.IsNullOrWhiteSpace(headerAppId))
            {
                context.Items[HttpContextAppContextAccessor.AppIdItemKey] = headerAppId;
                return _next(context);
            }

            var clientContext = ControllerHelper.GetClientContext(context);
            context.Items[HttpContextAppContextAccessor.AppIdItemKey] = ResolveAppIdByClientType(clientContext);
        }

        return _next(context);
    }

    private static string? TryResolveAppIdFromClaims(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var appId = user.FindFirst("app_id")?.Value ?? user.FindFirst("appId")?.Value;
        return string.IsNullOrWhiteSpace(appId) ? null : appId;
    }

    private string ResolveAppIdByClientType(ClientContext clientContext)
    {
        if (_options.ClientTypeMappings.Count == 0)
        {
            return _options.DefaultAppId;
        }

        var clientType = clientContext.ClientType.ToString();
        foreach (var mapping in _options.ClientTypeMappings)
        {
            if (string.Equals(mapping.ClientType, clientType, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(mapping.AppId))
            {
                return mapping.AppId.Trim();
            }
        }

        return _options.DefaultAppId;
    }

    private string? TryResolveAppIdFromHeader(HttpContext context)
    {
        var headerName = _options.HeaderName;
        if (string.IsNullOrWhiteSpace(headerName)
            || !context.Request.Headers.TryGetValue(headerName, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = raw.ToString().Trim();
        if (!_options.RequireHeaderAppIdNumeric)
        {
            return value;
        }

        return long.TryParse(value, out var parsed) && parsed > 0 ? value : null;
    }
}
