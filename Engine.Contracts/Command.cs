namespace Engine.Contracts;

public abstract record Command
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
    public abstract string Name { get; }
    public abstract int SchemaVersion { get; }
    public long? ExpectedDocumentVersion { get; init; }
}
