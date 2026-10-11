using Engine.Contracts;

namespace Engine.Core.Queries;

// The triangles of a body, per ADR-0018 item 6 (TASK-0028). A query is not
// logged and not replayed, so the tessellation never enters the Document.
public sealed record GetTessellationQuery : Query
{
    public override string Name => "GetTessellation";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}
