using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0076 item 1 (phase K5a, TASK-0238): one cylinder body with its centre at the origin and its axis along +z, as
// CreateBox makes a box with its centre at the origin. The record holds the inputs only, so a later slice can give the
// true surface from the log. The body handle comes from the command identifier (ADR-0012 item 4).
public sealed record CreateCylinderCommand : Command
{
    public override string Name => "CreateCylinder";
    public override int SchemaVersion => 1;

    public required double Radius { get; init; }
    public required double Height { get; init; }
}
