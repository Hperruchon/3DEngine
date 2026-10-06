namespace Engine.Contracts;

public sealed class Document
{
    public Guid DocumentId { get; }
    public Guid? ProjectId { get; }
    public int SchemaVersion { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<Command> _log = new();

    // The version counts applied commands: it is the count of entries in Log
    // (ADR-0020). A rejected command and a cancelled command do not change it,
    // so a replay of the log rebuilds it. Seq, the identity of an event, is a
    // separate counter that the command bus keeps (ADR-0005), and no code
    // computes one from the other. The version cannot go back, because no code
    // can set it (finding E10 of the codebase review of 2026-09-30).
    public long Version => _log.Count;

    public IReadOnlyList<Command> Log => _log;

    // Body projection per ADR-0012 §3. The Document holds handles + minimum
    // metadata; the backend owns the geometry data (ADR-0001 §3).
    // Mutation goes only through CommandBus's commit section.
    private readonly Dictionary<Guid, BodyRecord> _bodies = new();
    public IReadOnlyCollection<BodyRecord> Bodies => _bodies.Values;

    public Document(Guid? projectId = null, int schemaVersion = 1)
    {
        DocumentId = Guid.NewGuid();
        ProjectId = projectId;
        SchemaVersion = schemaVersion;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    internal void AppendCommand(Command command)
    {
        _log.Add(command);
        UpdatedAt = DateTime.UtcNow;
    }

    internal void AddBody(BodyRecord body) => _bodies[body.Handle.Id] = body;
}
