namespace Atlas.Core.Utilities;

/// <summary>
/// CSV 导出相关工具方法。
/// </summary>
public static class CsvUtility
{
    /// <summary>
    /// 按 RFC 4180 对 CSV 字段值进行转义。
    /// </summary>
    /// <param name="value">原始字符串，可为 null。</param>
    /// <returns>转义后的字符串；null 或空字符串返回空字符串；仅含空格的字符串会保留。</returns>
    public static string EscapeField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // RFC 4180: escape " as ""
        var escaped = value.Replace("\"", "\"\"");
        // RFC 4180: fields containing ", comma, or newline must be enclosed in quotes
        bool needsQuotes = value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r');
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}
