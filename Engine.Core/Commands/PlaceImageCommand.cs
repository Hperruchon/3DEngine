using Engine.Contracts;

namespace Engine.Core.Commands;

// Places an image of the store of the assets on a datum (ADR-0045 items 1, 2 and 4). The
// identifier of the image is the identifier of the command. The client decodes the image
// and gives its width and its height in pixels; the engine does not read the bytes.
public sealed record PlaceImageCommand : Command
{
    public override string Name => "PlaceImage";
    public override int SchemaVersion => 1;

    public required string Asset { get; init; }
    public required Guid DatumId { get; init; }
    public required long Width { get; init; }
    public required long Height { get; init; }
    public double OriginU { get; init; }
    public double OriginV { get; init; }
    public double Scale { get; init; } = 1;
}
