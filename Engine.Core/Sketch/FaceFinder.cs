using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Sketch;

// ADR-0053 items 1 to 3: the face of a live body under a point, as the reference that CreateDatum takes. The finder is a
// read: it does not enter the log.
//
// ADR-0059 item 5 (phase K4c, TASK-0185): the face comes from the solid of the body in the owned kernel, so a wall of a
// hole is a face, with the name of the face of the tool that cut it, and a part of a face that a cut removed is not a
// face. The first convex piece whose plane holds the point, whose normal is the normal of the hit and whose boundary
// holds the point gives the name. Each face of a name resolves at the end of the log (ADR-0051 item 2), so its frame
// includes each Translate of the chain and the turn of each Subtract.
public static class FaceFinder
{
    // The smallest dot product of the normal of a face and the normal of the hit (item 3).
    public const double SameNormal = 0.999;

    // The reference of the face and the frame of the face now, or the reason that no face matches.
    public static (DatumReference? Reference, DatumFrame? Frame, string? Reason) Find(
        Document document, Guid body, (double X, double Y, double Z) point, (double X, double Y, double Z) normal)
    {
        ArgumentNullException.ThrowIfNull(document);
        var length = Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z));
        if (!(length > 0) || !double.IsFinite(length) || !double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z))
            return (null, null, "A face needs a finite point and a normal of a length greater than 0.");
        var n = (normal.X / length, normal.Y / length, normal.Z / length);

        if (document.Bodies.FirstOrDefault(item => item.Handle.Id == body) is not { } record)
            return (null, null, $"No live body with id '{body}' exists in the Document.");
        var (solid, reason) = KernelProjection.OfRecord(document, record);
        if (solid is null)
            return (null, null, $"The owned kernel cannot build the body '{body}': {reason}");

        var largest = Math.Max(Math.Max(Math.Abs(point.X), Math.Abs(point.Y)), Math.Abs(point.Z));
        var t = 1e-6 * (1 + largest);
        if (solid.Polygons.FirstOrDefault(polygon => Holds(polygon, point, n, t)) is not { } piece)
            return (null, null, $"No face of the body '{body}' holds the point with that normal.");

        var tag = piece.Tag;
        if (solid.Polygons.Where(polygon => polygon.Tag == tag).Select(polygon => polygon.Plane).Distinct().Skip(1).Any())
            return (null, null, $"The face '{tag.Element}' of '{tag.Feature}' is curved, and a datum names a plane face only.");

        var reference = new DatumReference(tag.Feature, tag.Element, tag.Item);
        var graph = FeatureGraph.Of(document);
        DatumResolver.ExtrudeState State(ExtrudeCommand extrude) => new(
            SketchModel.Find(document, extrude.SketchId),
            SketchRebuild.DistanceInEffect(document, extrude.CommandId) ?? extrude.Distance);
        var (frame, why) = DatumResolver.Resolve(reference, graph, int.MaxValue, State, id => DatumResolver.Frame(document, id));
        return frame is null ? (null, null, why) : (reference, frame, null);
    }

    // Item 3: the normal of the piece is the normal of the hit, the point is on its plane within t, and the point is
    // inside each edge of the convex piece within t.
    private static bool Holds(KernelPolygon piece, (double X, double Y, double Z) point, (double X, double Y, double Z) n, double t)
    {
        var (nx, ny, nz) = piece.Plane.Normal.ToDoubles();
        var size = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        var unit = (X: nx / size, Y: ny / size, Z: nz / size);
        if ((unit.X * n.X) + (unit.Y * n.Y) + (unit.Z * n.Z) < SameNormal)
            return false;

        var vertices = piece.Vertices.Select(vertex => vertex.ToDoubles()).ToList();
        var first = vertices[0];
        if (Math.Abs(((point.X - first.X) * unit.X) + ((point.Y - first.Y) * unit.Y) + ((point.Z - first.Z) * unit.Z)) > t)
            return false;

        for (var i = 0; i < vertices.Count; i++)
        {
            var (a, b) = (vertices[i], vertices[(i + 1) % vertices.Count]);
            var (ex, ey, ez) = (b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var edge = Math.Sqrt((ex * ex) + (ey * ey) + (ez * ez));
            if (edge == 0)
                continue;
            // The inward direction of the edge in the plane is n × e; the point is inside when its distance along it
            // is at least -t.
            var (ix, iy, iz) = ((unit.Y * ez) - (unit.Z * ey), (unit.Z * ex) - (unit.X * ez), (unit.X * ey) - (unit.Y * ex));
            var inside = (((point.X - a.X) * ix) + ((point.Y - a.Y) * iy) + ((point.Z - a.Z) * iz)) / edge;
            if (inside < -t)
                return false;
        }

        return true;
    }
}
