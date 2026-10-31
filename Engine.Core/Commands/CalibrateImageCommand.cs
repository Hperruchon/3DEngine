using Engine.Contracts;

namespace Engine.Core.Commands;

// Gives a live image the scale at which two of its points, in pixels, are Distance apart in
// millimetres (ADR-0045 item 5). The log holds the six inputs, and the handler computes the
// scale; the origin of the image stays.
public sealed record CalibrateImageCommand : Command
{
    public override string Name => "CalibrateImage";
    public override int SchemaVersion => 1;

    public required Guid ImageId { get; init; }
    public required double X1 { get; init; }
    public required double Y1 { get; init; }
    public required double X2 { get; init; }
    public required double Y2 { get; init; }
    public required double Distance { get; init; }
}
