namespace Atlas.Infrastructure.Options;

public sealed class DatabaseEncryptionOptions
{
    public bool Enabled { get; init; }
    public string Key { get; init; } = string.Empty;
}
