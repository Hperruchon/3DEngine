using System.Numerics;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop;

// ADR-0085 (the redesign, slice e, TASK-0273): the active sketch under the body that it makes. The "mix" direction of the
// designer brief moves the sketch into view under its body, and the camera does not move. The plane of the sketch moves
// along its normal, out of the box of the body, to the side away from the body. A move along the normal keeps the position
// (x, y) of each point in the plane, so the drawing and each pick use the moved plane, and each command gets the same
// positions as on the plane of the record. The move is presentation (ADR-0007): no command and no log hold it.
internal sealed record SketchLift(Guid Sketch, double Dx, double Dy, double Dz)
{
    // Item 3: the space between the body and its sketch, as a fraction of the diagonal of the box of the body.
    public const double Gap = 0.25;

    // Item 3: the move of the plane of the sketch, or null when the box or the normal has no length. The distance of each
    // corner of the box from the plane gives the side of the body; the plane goes to the other side, a gap past the box.
    public static SketchLift? Under(Guid sketch, DatumFrame frame, (double X, double Y, double Z) min, (double X, double Y, double Z) max)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var length = Math.Sqrt((frame.Normal.X * frame.Normal.X) + (frame.Normal.Y * frame.Normal.Y) + (frame.Normal.Z * frame.Normal.Z));
        var diagonal = Math.Sqrt(((max.X - min.X) * (max.X - min.X)) + ((max.Y - min.Y) * (max.Y - min.Y)) + ((max.Z - min.Z) * (max.Z - min.Z)));
        if (!(length > 0) || !(diagonal > 0))
            return null;

        var (nx, ny, nz) = (frame.Normal.X / length, frame.Normal.Y / length, frame.Normal.Z / length);
        var (low, high) = (double.MaxValue, double.MinValue);
        foreach (var x in new[] { min.X, max.X })
        {
            foreach (var y in new[] { min.Y, max.Y })
            {
                foreach (var z in new[] { min.Z, max.Z })
                {
                    var along = ((x - frame.Origin.X) * nx) + ((y - frame.Origin.Y) * ny) + ((z - frame.Origin.Z) * nz);
                    (low, high) = (Math.Min(low, along), Math.Max(high, along));
                }
            }
        }

        var t = Math.Abs(high) >= Math.Abs(low) ? low - (Gap * diagonal) : high + (Gap * diagonal);
        return new SketchLift(sketch, t * nx, t * ny, t * nz);
    }

    // The sketch with the moved plane when it is the sketch of this move; else the sketch as it is.
    public SketchInfo Apply(SketchInfo sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        if (sketch.SketchId != Sketch || SketchOverlay.PlaneOf(sketch) is not { } frame)
            return sketch;
        var origin = new DatumPoint(frame.Origin.X + Dx, frame.Origin.Y + Dy, frame.Origin.Z + Dz);
        return sketch with { Frame = new DatumFrame(origin, frame.U, frame.V, frame.Normal) };
    }

    // Item 4: a quiet line from each point of the moved sketch to its place on the plane of the record, in pixels, so that
    // the person sees which body the sketch makes.
    public IReadOnlyList<SketchOverlay.Stroke> Links(SketchInfo sketch, Camera camera, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        if (sketch.SketchId != Sketch)
            return [];
        var moved = Apply(sketch);
        var links = new List<SketchOverlay.Stroke>();
        foreach (var point in sketch.Points)
        {
            if (SketchOverlay.Pixel(moved, camera, width, height, (point.X, point.Y)) is Vector2 a
                && SketchOverlay.Pixel(sketch, camera, width, height, (point.X, point.Y)) is Vector2 b)
                links.Add(new SketchOverlay.Stroke(a, b, SketchOverlay.Mark.Reference));
        }

        return links;
    }
}
