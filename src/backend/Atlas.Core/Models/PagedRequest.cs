namespace Atlas.Core.Models;

/// <summary>
/// 统一分页请求模型。
/// 说明：
/// - 保留可写属性以兼容 ASP.NET Core Query Binder（支持 pageIndex/PageIndex 混用）。
/// - 保留参数构造函数，兼容现有手动 new PagedRequest(...) 调用点。
/// </summary>
public sealed class PagedRequest
{
    public PagedRequest()
    {
        PageIndex = 1;
        PageSize = 10;
        SortDesc = false;
    }

    public PagedRequest(int pageIndex, int pageSize, string? keyword, string? sortBy, bool sortDesc)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        Keyword = keyword;
        SortBy = sortBy;
        SortDesc = sortDesc;
    }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}