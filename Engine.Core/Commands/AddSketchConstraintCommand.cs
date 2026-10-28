using Engine.Contracts;

namespace Engine.Core.Commands;

// Adds a constraint of a kind to a sketch (ADR-0033 items 3 and 6). First, Second and
// Third are its items in order, and Value and SecondValue its values; an absent item or
// value ends the list. The identifier of the constraint is the identifier of the command.
public sealed record AddSketchConstraintCommand : Command
{
    public override string Name => "AddSketchConstraint";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required string Kind { get; init; }
    public required Guid First { get; init; }
    public Guid? Second { get; init; }
    public Guid? Third { get; init; }
    public double? Value { get; init; }
    public double? SecondValue { get; init; }
}
