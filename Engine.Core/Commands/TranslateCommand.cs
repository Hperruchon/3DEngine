using Engine.Contracts;

namespace Engine.Core.Commands;

// Translate op per ADR-0012 §1. Produces a NEW body: a copy of BodyId moved by
// (Dx, Dy, Dz). The commit consumes the source body (ADR-0021 item 3; until
// TASK-0085 this comment said that the source stays). The new body takes the name
// and the visibility of the source (ADR-0028 item 5). Its handle is deterministic
// from CommandId (ADR-0012 §4) so replay is byte-stable.
public sealed record TranslateCommand : Command
{
    public override string Name => "Translate";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required double Dx { get; init; }
    public required double Dy { get; init; }
    public required double Dz { get; init; }
}
