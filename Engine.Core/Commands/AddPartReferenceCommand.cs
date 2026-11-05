using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0071 item 3 (phase P8b1, TASK-0202): an assembly references a part at a fixed version. Part is the name of the
// bytes of the part file in the store of the assets: its header and its first Version entries. X, Y and Z place the
// origin of the part. The identifier of the reference is the identifier of the command.
public sealed record AddPartReferenceCommand : Command
{
    public override string Name => "AddPartReference";
    public override int SchemaVersion => 1;

    public required string Part { get; init; }
    public required long Version { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; }
}
