using Engine.Contracts;

namespace Engine.Core.Commands;

// Adds a circle about a point of a sketch, with a radius (ADR-0033 item 2, phase K2d). The
// identifier of the circle is the identifier of the command. The radius is an input of the
// solver, which can change it (ADR-0035, History of TASK-0117).
public sealed record AddSketchCircleCommand : Command
{
    public override string Name => "AddSketchCircle";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required Guid CenterPointId { get; init; }
    public required double Radius { get; init; }
}
