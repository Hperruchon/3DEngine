using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;

namespace Engine.Core.Sketch;

// ADR-0036 items 2 to 4 and ADR-0037 item 5: the parameters of an extrude of a sketch by a
// distance. Extrude and the rebuild of an extrude use this one code, so a rebuild of a sketch
// gives the same bits as a new extrude of it. The code uses + - * / and comparisons only.
public static class ExtrudeProfile
{
    // The parameters, or the reason that the sketch has no profile: it is not solved, or its
    // lines are not one closed loop. The caller checks the distance.
    public static (ExtrudeParameters? Parameters, string? Reason) Parameters(SketchRecord sketch, double distance)
    {
        // ADR-0036 item 4 (Q3 A): a sketch that is not solved gives no solid.
        if (!SketchSolver.Status(sketch).Solved)
            return (null, $"The sketch '{sketch.Id}' is not solved, so its profile is not the part that its constraints give.");

        var (loop, reason) = SketchProfile.Find(sketch);
        if (loop is null)
            return (null, reason);

        // CreateSketch refuses a datum that does not exist (ADR-0033 item 6), so each
        // live sketch has one of the origin planes.
        var datum = Datums.Find(sketch.DatumId)!;

        // ADR-0036 item 3: the height is |distance|, and the frame moves by min(0, distance)
        // along n. Each axis of an origin plane holds 0 and ±1 only, so each product is exact.
        var height = distance > 0 ? distance : -distance;
        var shift = distance > 0 ? 0.0 : distance;
        var (u, v, n) = (datum.U, datum.V, datum.Normal);
        return (new ExtrudeParameters(
            loop,
            height,
            (shift * n.X, shift * n.Y, shift * n.Z),
            (u.X, u.Y, u.Z),
            (v.X, v.Y, v.Z),
            (n.X, n.Y, n.Z)), null);
    }

    // True when two sets of parameters give the same solid: the same loop, height and frame.
    public static bool Same(ExtrudeParameters a, ExtrudeParameters b)
        => a.Height.Equals(b.Height) && a.Origin.Equals(b.Origin) && a.U.Equals(b.U) && a.V.Equals(b.V)
        && a.N.Equals(b.N) && a.Loop.SequenceEqual(b.Loop);
}
