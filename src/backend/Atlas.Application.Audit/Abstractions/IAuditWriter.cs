using Atlas.Domain.Audit.Entities;

namespace Atlas.Application.Audit.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken);
}
