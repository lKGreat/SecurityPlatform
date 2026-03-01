using Atlas.Application.Identity.Abstractions;
using Atlas.Core.Models;
using Atlas.Core.Tenancy;
using ClosedXML.Excel;

namespace Atlas.Infrastructure.Services;

/// <summary>
/// 基于 ClosedXML 的 Excel 导出实现
/// </summary>
public sealed class ClosedXmlExcelExportService : IExcelExportService
{
    private readonly IUserQueryService _userQueryService;

    public ClosedXmlExcelExportService(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<byte[]> ExportUsersAsync(
        TenantId tenantId, string? keyword = null, CancellationToken ct = default)
    {
        // 最多导出 5000 条
        var result = await _userQueryService.QueryUsersAsync(
            new PagedRequest(1, 5000, keyword, null, false), tenantId, ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("用户列表");

        // 表头
        var headers = new[] { "用户名", "显示名称", "邮箱", "手机号", "状态", "最后登录时间" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // 数据行
        var row = 2;
        foreach (var user in result.Items)
        {
            ws.Cell(row, 1).Value = user.Username;
            ws.Cell(row, 2).Value = user.DisplayName;
            ws.Cell(row, 3).Value = user.Email ?? string.Empty;
            ws.Cell(row, 4).Value = user.PhoneNumber ?? string.Empty;
            ws.Cell(row, 5).Value = user.IsActive ? "启用" : "禁用";
            ws.Cell(row, 6).Value = user.LastLoginAt.HasValue
                ? user.LastLoginAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : string.Empty;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] GenerateUserImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("用户导入模板");

        var headers = new[] { "用户名*", "显示名称*", "邮箱", "手机号" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // 示例行
        ws.Cell(2, 1).Value = "zhangsan";
        ws.Cell(2, 2).Value = "张三";
        ws.Cell(2, 3).Value = "zhangsan@example.com";
        ws.Cell(2, 4).Value = "13800138000";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
