using Engine.Contracts;

namespace Engine.Core.Queries;

// The identifier, the kind, the name and the visibility of a live body (ADR-0028 item 2).
public sealed record GetBodyInfoQuery : Query
{
    public override string Name => "GetBodyInfo";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}

// The result of GetBodyInfo. Name is null when the body has no name. Distance is the distance in
// effect of the extrude whose handle is the body, or null for each other body (ADR-0049 item 7).
// ADR-0191 item 8 (phase P11.0, TASK-0474): the placement of the body, null at the identity, so a client that moves a body
// with the absolute PlaceBody reads where the body is.
public sealed record BodyInfo(Guid BodyId, string Kind, string? Name, bool Visible, double? Distance = null, BodyPlacement? Placement = null);
