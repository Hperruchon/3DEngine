using Engine.Contracts;
using Engine.Contracts.Sketch;

namespace Engine.Core.Queries;

// ADR-0053 item 1: the face of a live body under a point of a hit, with the normal of the hit triangle toward the eye.
public sealed record GetFaceAtQuery : Query
{
    public override string Name => "GetFaceAt";
    public override int SchemaVersion => 1;

    public required Guid BodyId { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Z { get; init; }
    public required double Nx { get; init; }
    public required double Ny { get; init; }
    public required double Nz { get; init; }
}

// The reference of the face, as CreateDatum takes it, and the first datum of the Document with an equal reference, no
// fault and the frame of the face now, or null.
public sealed record FaceInfo(DatumReference Reference, Guid? DatumId);
