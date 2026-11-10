using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0112 item 1 (TASK-0335): a NEW body, a copy of BodyId turned about the origin of the Document by the quaternion
// (Qw, Qx, Qy, Qz), as Translate moves a copy. The log holds the four numbers and no angle (CLAUDE.md, determinism rule
// 2). The commit consumes the source body (ADR-0021 item 3), and the new body takes its name and its visibility. Its
// handle is the CommandId (ADR-0012 item 4).
public sealed record RotateCommand : Command
{
    public override string Name => "Rotate";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required double Qw { get; init; }
    public required double Qx { get; init; }
    public required double Qy { get; init; }
    public required double Qz { get; init; }
}
