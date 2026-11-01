using Engine.Contracts;

namespace Engine.Core.Commands;

// Changes the distance of an extrude in effect (ADR-0049 item 1). The command records the
// identifier of the Extrude, which is also the handle of its body, and a signed distance, and no
// solid: the rebuild reads the live sketch and the distance in effect (ADR-0049 item 3).
public sealed record SetExtrudeDistanceCommand : Command
{
    public override string Name => "SetExtrudeDistance";
    public override int SchemaVersion => 1;

    public required Guid ExtrudeId { get; init; }
    public required double Distance { get; init; }
}
