using Atlas.Application.DataSource;
using Atlas.Core.DataSource;

namespace Atlas.Infrastructure.DataSource;

public sealed class DataSourceConnectorRegistry : IDataSourceConnectorRegistry
{
    private readonly Dictionary<string, IDataSourceConnector> _connectors =
        new(StringComparer.OrdinalIgnoreCase);

    public void Register(IDataSourceConnector connector)
    {
        _connectors[connector.DataSourceType] = connector;
    }

    public IDataSourceConnector? Get(string dataSourceType)
        => _connectors.TryGetValue(dataSourceType, out var c) ? c : null;

    public IReadOnlyList<IDataSourceConnector> GetAll()
        => [.. _connectors.Values];
}
