using Engine.Contracts;

namespace Engine.Core.Commands;

// Extrudes the closed profile of a live sketch into a solid (ADR-0036 item 1). The
// command records the identifier of the sketch and a signed distance, and no coordinate:
// the handler reads the current record of the sketch. The body handle is the identifier
// of the command (ADR-0012 §4).
public sealed record ExtrudeCommand : Command
{
    public override string Name => "Extrude";
    public override int SchemaVersion => 1;

    public required Guid SketchId { get; init; }
    public required double Distance { get; init; }
}
