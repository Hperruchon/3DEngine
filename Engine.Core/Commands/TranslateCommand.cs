using Engine.Contracts;

namespace Engine.Core.Commands;

// Translate op per ADR-0012 (Amendment 1, P7c). Produces a NEW body: a copy of
// BodyId moved by (Dx, Dy, Dz). The source body is left intact. The new body's
// handle is deterministic from CommandId (ADR-0012 §4) so replay is byte-stable.
public sealed record TranslateCommand : Command
{
    public override string Name => "Translate";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required double Dx { get; init; }
    public required double Dy { get; init; }
    public required double Dz { get; init; }
}
