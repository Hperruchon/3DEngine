using Engine.Contracts;

namespace Engine.Core.Commands;

// Sets the name of a live body (ADR-0028 item 2). An empty name clears it. The body
// keeps its handle, its kind and its geometry; only its record changes.
public sealed record RenameBodyCommand : Command
{
    public override string Name => "RenameBody";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required string NewName { get; init; }
}
