using Engine.Contracts;

namespace Engine.Core.Commands;

// Gives a point of a sketch a new position (ADR-0033 item 6). The point keeps its
// identifier, so each line and each constraint that references it stays valid.
public sealed record MoveSketchPointCommand : Command
{
    public override string Name => "MoveSketchPoint";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid PointId { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
}
