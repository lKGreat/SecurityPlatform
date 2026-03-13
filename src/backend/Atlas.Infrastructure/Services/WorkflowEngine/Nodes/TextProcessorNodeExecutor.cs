using System.Text.Json;
using Atlas.Domain.Workflow.Enums;
using Atlas.Domain.Workflow.ValueObjects;

namespace Atlas.Infrastructure.Services.WorkflowEngine.Nodes;

/// <summary>
/// TextProcessor 节点：文本拼接、截取、格式化、替换等操作。
/// Config 结构：
/// {
///   "operation": "concat",  // concat | substring | replace | trim | upper | lower | template
///   "template": "Hello, {{entry_1.name}}!",
///   "separator": " ",
///   "parts": ["entry_1.firstName", "entry_1.lastName"]
/// }
/// </summary>
public sealed class TextProcessorNodeExecutor : INodeExecutor
{
    public NodeType NodeType => NodeType.TextProcessor;

    public Task<NodeExecutorResult> ExecuteAsync(NodeSchema node, NodeExecutionContext context)
    {
        var operation = node.Configs.TryGetValue("operation", out var op) ? op?.ToString() ?? "template" : "template";

        var result = operation.ToLowerInvariant() switch
        {
            "concat" => Concat(node, context),
            "substring" => Substring(node, context),
            "replace" => Replace(node, context),
            "trim" => Trim(node, context),
            "upper" => Transform(node, context, s => s.ToUpperInvariant()),
            "lower" => Transform(node, context, s => s.ToLowerInvariant()),
            _ => Template(node, context)
        };

        context.SetOutput(node.Key, "text", result);
        return Task.FromResult(NodeExecutorResult.Default);
    }

    private static string Concat(NodeSchema node, NodeExecutionContext context)
    {
        var separator = node.Configs.TryGetValue("separator", out var sep) ? sep?.ToString() ?? "" : "";

        if (!node.Configs.TryGetValue("parts", out var partsObj) ||
            partsObj is not System.Text.Json.JsonElement je ||
            je.ValueKind != System.Text.Json.JsonValueKind.Array)
            return string.Empty;

        var parts = je.Deserialize<List<string>>() ?? new List<string>();
        return string.Join(separator, parts.Select(p => context.GetVariable(p)?.ToString() ?? string.Empty));
    }

    private static string Substring(NodeSchema node, NodeExecutionContext context)
    {
        var textRef = node.Configs.TryGetValue("textRef", out var tr) ? tr?.ToString() : null;
        var text = (string.IsNullOrEmpty(textRef) ? null : context.GetVariable(textRef)?.ToString()) ?? string.Empty;
        var start = GetInt(node, "start") ?? 0;
        var length = GetInt(node, "length");

        if (start >= text.Length) return string.Empty;
        return length.HasValue
            ? text.Substring(start, Math.Min(length.Value, text.Length - start))
            : text[start..];
    }

    private static string Replace(NodeSchema node, NodeExecutionContext context)
    {
        var textRef = node.Configs.TryGetValue("textRef", out var tr) ? tr?.ToString() : null;
        var text = (string.IsNullOrEmpty(textRef) ? null : context.GetVariable(textRef)?.ToString()) ?? string.Empty;
        var oldValue = node.Configs.TryGetValue("oldValue", out var ov) ? ov?.ToString() ?? "" : "";
        var newValue = node.Configs.TryGetValue("newValue", out var nv) ? nv?.ToString() ?? "" : "";
        return text.Replace(oldValue, newValue);
    }

    private static string Trim(NodeSchema node, NodeExecutionContext context)
    {
        var textRef = node.Configs.TryGetValue("textRef", out var tr) ? tr?.ToString() : null;
        var text = (string.IsNullOrEmpty(textRef) ? null : context.GetVariable(textRef)?.ToString()) ?? string.Empty;
        return text.Trim();
    }

    private static string Transform(NodeSchema node, NodeExecutionContext context, Func<string, string> transform)
    {
        var textRef = node.Configs.TryGetValue("textRef", out var tr) ? tr?.ToString() : null;
        var text = (string.IsNullOrEmpty(textRef) ? null : context.GetVariable(textRef)?.ToString()) ?? string.Empty;
        return transform(text);
    }

    private static string Template(NodeSchema node, NodeExecutionContext context)
    {
        var template = node.Configs.TryGetValue("template", out var t) ? t?.ToString() ?? "" : "";
        return System.Text.RegularExpressions.Regex.Replace(
            template,
            @"\{\{([^}]+)\}\}",
            m => context.GetVariable(m.Groups[1].Value.Trim())?.ToString() ?? string.Empty);
    }

    private static int? GetInt(NodeSchema node, string key)
    {
        if (node.Configs.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var i))
            return i;
        return null;
    }
}
