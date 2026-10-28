using Engine.Contracts;

namespace Engine.Core.Commands;

// Gives a constraint of a sketch new values, with the same kind and the same items
// (ADR-0033 item 6). The constraint keeps its identifier.
public sealed record SetSketchConstraintValueCommand : Command
{
    public override string Name => "SetSketchConstraintValue";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid ConstraintId { get; init; }
    public required double Value { get; init; }
    public double? SecondValue { get; init; }
}
