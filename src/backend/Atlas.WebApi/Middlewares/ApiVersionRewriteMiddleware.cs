namespace Atlas.WebApi.Middlewares;

public sealed class ApiVersionRewriteMiddleware
{
    private const string VersionPrefix = "/api/v1";
    private const string LegacyPrefix = "/api";
    private readonly RequestDelegate _next;

    public ApiVersionRewriteMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return _next(context);
        }

        // 已有版本前缀的路径（/api/v1, /api/v2, ...）直接放行
        if (path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase) && path.Length > 6 && char.IsDigit(path[6]))
        {
            return _next(context);
        }

        if (path.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var rest = path.Substring(LegacyPrefix.Length);
            context.Request.Path = string.IsNullOrWhiteSpace(rest)
                ? VersionPrefix
                : $"{VersionPrefix}{rest}";
        }

        return _next(context);
    }
}
