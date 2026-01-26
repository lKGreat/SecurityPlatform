using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using Atlas.WebApi.Tenancy;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Atlas.WebApi.Middlewares;

public sealed class TenantContextMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";
    public const string TenantClaimType = "tenant_id";

    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var path = context.Request.Path.Value ?? string.Empty;
        var allowWithoutTenant = allowAnonymous
            && (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase));

        if (allowWithoutTenant)
        {
            await _next(context);
            return;
        }

        if (!TryResolveTenantId(context, out var tenantId))
        {
            await WriteTenantErrorAsync(context, "无效或缺失租户标识");
            return;
        }

        context.Items[HttpContextTenantProvider.TenantContextKey] = new TenantContext(tenantId);

        await _next(context);
    }

    private static bool TryResolveTenantId(HttpContext context, out TenantId tenantId)
    {
        tenantId = TenantId.Empty;

        if (context.Request.Headers.TryGetValue(TenantHeaderName, out var headerValue))
        {
            if (Guid.TryParse(headerValue.ToString(), out var tenantGuid))
            {
                tenantId = new TenantId(tenantGuid);
                return true;
            }

            return false;
        }

        var claim = context.User.FindFirstValue(TenantClaimType);
        if (!string.IsNullOrWhiteSpace(claim) && Guid.TryParse(claim, out var claimGuid))
        {
            tenantId = new TenantId(claimGuid);
            return true;
        }

        return false;
    }

    private static async Task WriteTenantErrorAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object?>.Fail(ErrorCodes.ValidationError, message, context.TraceIdentifier);
        await context.Response.WriteAsJsonAsync(payload);
    }
}