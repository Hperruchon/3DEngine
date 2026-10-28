using Engine.Contracts;

namespace Engine.Core.Commands;

// Makes a sketch on a datum, with its frame (ADR-0033 items 1, 2 and 6). The identifier of
// the sketch is the identifier of the command. A name is optional.
public sealed record CreateSketchCommand : Command
{
    public override string Name => "CreateSketch";
    public override int SchemaVersion => 1;

    public required Guid DatumId { get; init; }
    public string? SketchName { get; init; }
}
