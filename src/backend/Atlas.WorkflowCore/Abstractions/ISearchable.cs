using System.Collections.Generic;

namespace Atlas.WorkflowCore.Abstractions;

/// <summary>
/// 可搜索接口 - 标记实体可被搜索索引
/// </summary>
public interface ISearchable
{
    /// <summary>
    /// 获取搜索令牌（用于全文搜索）
    /// </summary>
    IEnumerable<string> GetSearchTokens();
}
