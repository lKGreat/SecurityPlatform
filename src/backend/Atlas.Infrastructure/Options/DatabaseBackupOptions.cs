namespace Atlas.Infrastructure.Options;

public sealed class DatabaseBackupOptions
{
    public bool Enabled { get; init; }
    public string BackupDirectory { get; init; } = "backups";
    public int IntervalHours { get; init; } = 24;
    public int RetentionDays { get; init; } = 30;
}
