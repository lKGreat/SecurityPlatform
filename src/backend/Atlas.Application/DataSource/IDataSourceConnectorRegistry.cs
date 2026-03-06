using Atlas.Core.DataSource;

namespace Atlas.Application.DataSource;

/// <summary>
/// 数据源连接器注册表（管理所有已注册的连接器）
/// </summary>
public interface IDataSourceConnectorRegistry
{
    void Register(IDataSourceConnector connector);
    IDataSourceConnector? Get(string dataSourceType);
    IReadOnlyList<IDataSourceConnector> GetAll();
}
