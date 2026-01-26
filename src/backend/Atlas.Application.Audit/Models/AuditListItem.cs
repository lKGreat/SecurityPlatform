namespace Atlas.Application.Audit.Models;

public sealed record AuditListItem(string Id, string Action, DateTimeOffset OccurredAt);
