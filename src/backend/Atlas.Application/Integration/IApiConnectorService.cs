using Atlas.Domain.Integration;

namespace Atlas.Application.Integration;

public interface IApiConnectorService
{
    Task<IReadOnlyList<ApiConnector>> GetAllAsync(CancellationToken cancellationToken);
    Task<ApiConnector?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(CreateApiConnectorRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(long id, UpdateApiConnectorRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApiConnectorOperation>> GetOperationsAsync(long connectorId, CancellationToken cancellationToken);

    /// <summary>从 OpenAPI spec 同步操作列表</summary>
    Task SyncFromSpecAsync(long connectorId, CancellationToken cancellationToken);

    /// <summary>代理执行一个操作</summary>
    Task<ApiConnectorExecuteResult> ExecuteAsync(
        long connectorId, string operationId,
        Dictionary<string, string?> pathParams,
        Dictionary<string, string?> queryParams,
        string? requestBody,
        CancellationToken cancellationToken);

    /// <summary>健康检查</summary>
    Task<bool> HealthCheckAsync(long connectorId, CancellationToken cancellationToken);
}

public sealed record CreateApiConnectorRequest(
    string Name,
    string BaseUrl,
    ApiAuthType AuthType,
    string? AuthConfig,
    string? OpenApiSpecUrl,
    string? HealthCheckUrl,
    int TimeoutSeconds);

public sealed record UpdateApiConnectorRequest(
    string Name,
    string BaseUrl,
    ApiAuthType AuthType,
    string? AuthConfig,
    string? OpenApiSpecUrl,
    string? HealthCheckUrl,
    int TimeoutSeconds,
    bool IsActive);

public sealed record ApiConnectorExecuteResult(
    bool Success,
    int StatusCode,
    string ResponseBody,
    long DurationMs);
