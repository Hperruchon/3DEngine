using Engine.Contracts;

namespace Engine.Core.Commands;

// Removes a point, a line or a constraint of a sketch, and each item that depends on it
// (ADR-0033 item 6).
public sealed record DeleteSketchItemCommand : Command
{
    public override string Name => "DeleteSketchItem";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid ItemId { get; init; }
}
