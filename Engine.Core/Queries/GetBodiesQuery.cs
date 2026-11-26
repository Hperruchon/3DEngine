using Engine.Contracts;

namespace Engine.Core.Queries;

// ADR-0189 item 4 (phase P21, TASK-0471): the live bodies of the Document with their names. Before P21 only the snapshot of
// the events gave them; GetFeatures gives the identifiers with no name (R-0075).
public sealed record GetBodiesQuery : Query
{
    public override string Name => "GetBodies";
    public override int SchemaVersion => 1;
}

// The result of GetBodies: one BodyInfo for each live body, the answer of GetBodyInfo for that body, in the order of the
// Document (ADR-0189 items 4 and 5).
public sealed record BodiesInfo(IReadOnlyList<BodyInfo> Bodies);
