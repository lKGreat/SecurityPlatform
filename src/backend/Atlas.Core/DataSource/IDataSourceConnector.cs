using Atlas.Core.Models;
using Atlas.Core.Plugins;

namespace Atlas.Core.DataSource;

/// <summary>
/// 数据源连接器插件接口（继承 IAtlasPlugin）
/// 内置连接器和外部插件连接器都实现此接口
/// </summary>
public interface IDataSourceConnector : IAtlasPlugin
{
    /// <summary>支持的数据源类型（如 "sqlite", "mysql", "postgresql"）</summary>
    string DataSourceType { get; }

    /// <summary>测试连接是否可用</summary>
    Task<bool> TestConnectionAsync(string connectionString, CancellationToken cancellationToken);

    /// <summary>获取数据库 Schema（表/视图列表）</summary>
    Task<DataSourceSchema> GetSchemaAsync(string connectionString, CancellationToken cancellationToken);

    /// <summary>执行查询，返回分页结果</summary>
    Task<PagedResult<Dictionary<string, object?>>> QueryAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>执行非查询语句（INSERT/UPDATE/DELETE）</summary>
    Task<int> ExecuteAsync(
        string connectionString,
        string sql,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken);
}

public sealed record DataSourceSchema(
    IReadOnlyList<TableInfo> Tables,
    IReadOnlyList<TableInfo> Views);

public sealed record TableInfo(
    string Name,
    string? Comment,
    IReadOnlyList<ColumnInfo> Columns);

public sealed record ColumnInfo(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    string? Comment);
