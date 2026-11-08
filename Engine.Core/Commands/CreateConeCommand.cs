using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0089 item 2 (phase K5c, TASK-0288): one cone body with the centre of its base circle and its apex on the z axis, the
// base at z = -height / 2 and the apex at z = +height / 2, as CreateCylinder makes a cylinder with its centre at the
// origin. The record holds the inputs only (ADR-0007). The body handle comes from the command identifier (ADR-0012 item 4).
public sealed record CreateConeCommand : Command
{
    public override string Name => "CreateCone";
    public override int SchemaVersion => 1;

    public required double Radius { get; init; }
    public required double Height { get; init; }
}
