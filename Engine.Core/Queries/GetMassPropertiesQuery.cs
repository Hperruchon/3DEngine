using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// ADR-0180 item 3 (phase K15a, TASK-0432): the volume and the centre of a live body, from the solid of the owned kernel.
public sealed record GetMassPropertiesQuery : Query
{
    public override string Name => "GetMassProperties";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
}

// The volume in cubic millimetres and the centre of the volume in millimetres, each the nearest double to the exact value
// of the solid. ADR-0180 item 10 (phase K15b, TASK-0434): a body of one curved command, moved or turned, gives the volume
// and the centre of the curve of the design in doubles. Facets is true when a face of the body is curved and no closed
// form exists: the volume and the centre are then those of the plane facets of the solid, and not those of the curve
// (answer Q2 A of the codebase review of 2026-11-21).
public sealed record MassProperties(double Volume, DatumPoint Centre, bool Facets);
