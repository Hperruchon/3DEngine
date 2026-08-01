using Engine.Contracts;

namespace Engine.Core.Commands;

// Boolean subtract per ADR-0012 (Amendment 1, P7c). Produces a NEW body:
// MinuendBodyId with SubtrahendBodyId's overlapping volume removed. Both operands
// are left intact. The new body's handle is deterministic from CommandId
// (ADR-0012 §4) so replay is byte-stable.
public sealed record SubtractCommand : Command
{
    public override string Name => "Subtract";
    public override int SchemaVersion => 1;

    public required Guid MinuendBodyId { get; init; }
    public required Guid SubtrahendBodyId { get; init; }
}
