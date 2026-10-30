using Engine.Contracts;

namespace Engine.Core.Commands;

// Adds an arc from its start point to its end point, counter-clockwise about its centre point
// (ADR-0033 item 2, phase K2d). The identifier of the arc is the identifier of the command. The
// two radii are equal by an implicit equation of the solver; no field is an angle.
public sealed record AddSketchArcCommand : Command
{
    public override string Name => "AddSketchArc";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid CenterPointId { get; init; }
    public required Guid StartPointId { get; init; }
    public required Guid EndPointId { get; init; }
}
