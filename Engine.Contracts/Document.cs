namespace Engine.Contracts;

public sealed class Document
{
    public Guid DocumentId { get; }
    public Guid? ProjectId { get; }
    public int SchemaVersion { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    // Runtime observation version — mirrors the last emitted Seq across ALL events
    // (applied + rejected + cancelled), not a successful-mutation counter. Per TASK-0001 §Notes.
    // "Document unchanged" on rejection refers to log + materialized state, not Version.
    public long Version { get; private set; }

    private readonly List<Command> _log = new();
    public IReadOnlyList<Command> Log => _log;

    public Document(Guid? projectId = null, int schemaVersion = 1)
    {
        DocumentId = Guid.NewGuid();
        ProjectId = projectId;
        SchemaVersion = schemaVersion;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        Version = 0;
    }

    internal void AppendCommand(Command command) => _log.Add(command);

    internal void AdvanceVersion(long newSeq)
    {
        Version = newSeq;
        UpdatedAt = DateTime.UtcNow;
    }
}
