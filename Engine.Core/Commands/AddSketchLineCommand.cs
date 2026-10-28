using Engine.Contracts;

namespace Engine.Core.Commands;

// Adds a line segment between two different points of a sketch (ADR-0033 items 2 and 6).
// The identifier of the line is the identifier of the command.
public sealed record AddSketchLineCommand : Command
{
    public override string Name => "AddSketchLine";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid StartPointId { get; init; }
    public required Guid EndPointId { get; init; }
}
