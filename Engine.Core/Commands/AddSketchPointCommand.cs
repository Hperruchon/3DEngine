using Engine.Contracts;

namespace Engine.Core.Commands;

// Adds a point at (X, Y) in the plane of a sketch (ADR-0033 items 2 and 6). The identifier
// of the point is the identifier of the command.
public sealed record AddSketchPointCommand : Command
{
    public override string Name => "AddSketchPoint";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
}
