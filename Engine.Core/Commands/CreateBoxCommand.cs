using Engine.Contracts;

namespace Engine.Core.Commands;

// First geometry command per TASK-0011. Creates one axis-aligned box body
// centered at origin. Body handle is deterministic from CommandId
// (ADR-0012 §4) so replay is byte-stable.
public sealed record CreateBoxCommand : Command
{
    public override string Name => "CreateBox";
    public override int SchemaVersion => 1;

    public required double SizeX { get; init; }
    public required double SizeY { get; init; }
    public required double SizeZ { get; init; }
}
