using Engine.Contracts;

namespace Engine.Core.Commands;

// Makes a datum on a face of a feature (ADR-0051 items 1 and 4). The command records the
// reference, never a frame: the engine resolves the frame at each apply and each rebuild. The
// identifier of the datum is the identifier of the command. ItemId and a name are optional.
public sealed record CreateDatumCommand : Command
{
    public override string Name => "CreateDatum";
    public override int SchemaVersion => 1;

    public required Guid FeatureId { get; init; }
    public required string Element { get; init; }
    public Guid? ItemId { get; init; }
    public string? DatumName { get; init; }
}
