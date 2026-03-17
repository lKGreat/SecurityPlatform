using Microsoft.AspNetCore.Mvc.Filters;

namespace Atlas.WebApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class DeprecatedApiAttribute : ActionFilterAttribute
{
    private readonly string _message;
    private readonly string _sunset;
    private readonly string _replacement;

    public DeprecatedApiAttribute(string message, string replacement, string sunset = "2026-09-17")
    {
        _message = message;
        _replacement = replacement;
        _sunset = sunset;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        headers["Deprecation"] = "true";
        headers["Sunset"] = _sunset;
        headers["Warning"] = $"299 - \"Deprecated API: {_message}. Use {_replacement}.\"";
        headers["X-Api-Deprecated"] = "true";
        headers["X-Api-Replacement"] = _replacement;
        base.OnActionExecuting(context);
    }
}
