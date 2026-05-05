namespace Engine.Contracts;

public abstract record Query
{
    public Guid QueryId { get; init; } = Guid.NewGuid();
    public abstract string Name { get; }
    public abstract int SchemaVersion { get; }
}
