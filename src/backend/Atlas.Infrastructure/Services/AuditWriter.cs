using Atlas.Application.Audit.Abstractions;
using Atlas.Domain.Audit.Entities;
using SqlSugar;

namespace Atlas.Infrastructure.Services;

public sealed class AuditWriter : IAuditWriter
{
    private readonly ISqlSugarClient _db;

    public AuditWriter(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken)
    {
        return _db.Insertable(record).ExecuteCommandAsync(cancellationToken);
    }
}
