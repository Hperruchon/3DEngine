using Engine.Contracts;

namespace Engine.Core.Commands;

// Adds to a sketch the projection of an edge or a vertex of a feature (ADR-0052 items 1 to 3). The
// command records the reference, never a position: the engine resolves the points at each apply
// and each rebuild. The identifier of the reference is the identifier of the command.
public sealed record AddSketchReferenceCommand : Command
{
    public override string Name => "AddSketchReference";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid FeatureId { get; init; }
    public required string Element { get; init; }
    public Guid? ItemId { get; init; }
}
