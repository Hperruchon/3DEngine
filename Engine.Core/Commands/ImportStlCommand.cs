using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0083 item 1 (phase P9b, TASK-0258): the import of an STL file of the store of the assets as a mesh body. The
// identifier of the body is the identifier of the command. Asset is the name of the bytes in the store; BodyName is the
// name of the body, for example the name of the file, or null.
public sealed record ImportStlCommand : Command
{
    public override string Name => "ImportStl";
    public override int SchemaVersion => 1;

    public required string Asset { get; init; }
    public string? BodyName { get; init; }
}
