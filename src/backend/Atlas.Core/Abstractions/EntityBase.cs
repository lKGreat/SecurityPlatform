namespace Atlas.Core.Abstractions;

public abstract class EntityBase
{
    protected EntityBase()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; init; }
}