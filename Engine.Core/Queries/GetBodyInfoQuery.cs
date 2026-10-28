using Engine.Contracts;

namespace Engine.Core.Queries;

// The identifier, the kind, the name and the visibility of a live body (ADR-0028 item 2).
public sealed record GetBodyInfoQuery : Query
{
    public override string Name => "GetBodyInfo";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}

// The result of GetBodyInfo. Name is null when the body has no name.
public sealed record BodyInfo(Guid BodyId, string Kind, string? Name, bool Visible);
