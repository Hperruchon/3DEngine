using Engine.Contracts;

namespace Engine.Core.Commands;

// Proof-of-concept command. The only command in P0.
// Echoes its input parameter back through Outputs to prove round-trip.
public sealed record NoOpCommand : Command
{
    public override string Name => "NoOp";
    public override int SchemaVersion => 1;
    public required string Echo { get; init; }
}
