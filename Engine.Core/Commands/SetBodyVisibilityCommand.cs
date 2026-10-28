using Engine.Contracts;

namespace Engine.Core.Commands;

// Shows or hides a live body (ADR-0028 item 2). A hidden body is a body: each command and
// each query works on it as on a visible one (ADR-0028 item 4). Only the client draws less.
public sealed record SetBodyVisibilityCommand : Command
{
    public override string Name => "SetBodyVisibility";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required bool Visible { get; init; }
}
