using Engine.Contracts;

namespace Engine.Core.Commands;

// ADR-0089 item 5 (phase K5d, TASK-0289): one sphere body with its centre at the origin, as CreateBox makes a box with its
// centre at the origin. The record holds the input only (ADR-0007). The body handle comes from the command identifier.
public sealed record CreateSphereCommand : Command
{
    public override string Name => "CreateSphere";
    public override int SchemaVersion => 1;

    public required double Radius { get; init; }
}

// ADR-0089 item 5 (phase K5d, TASK-0289): one torus body with its centre at the origin and its axis along +z. Radius is the
// distance from the axis to the centre of the tube, and TubeRadius the radius of the tube.
public sealed record CreateTorusCommand : Command
{
    public override string Name => "CreateTorus";
    public override int SchemaVersion => 1;

    public required double Radius { get; init; }
    public required double TubeRadius { get; init; }
}
