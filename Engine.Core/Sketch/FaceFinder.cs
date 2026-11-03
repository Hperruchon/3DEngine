using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;

namespace Engine.Core.Sketch;

// ADR-0053 items 1 to 3: the face of a live body under a point, as the reference that CreateDatum takes. The walk goes
// back through each Translate to its body and through each Subtract to its minuend, to an Extrude or a CreateBox. Each
// face of that feature resolves at the end of the log, so its frame includes each Translate of the chain, and the
// first face whose normal and extent hold the point matches. The finder is a read: it does not enter the log.
public static class FaceFinder
{
    // The smallest dot product of the normal of a face and the normal of the hit (item 3).
    public const double SameNormal = 0.999;

    // The reference of the face and the frame of the face now, or the reason that no face matches.
    public static (DatumReference? Reference, DatumFrame? Frame, string? Reason) Find(
        Document document, Guid body, (double X, double Y, double Z) point, (double X, double Y, double Z) normal)
    {
        ArgumentNullException.ThrowIfNull(document);
        var graph = FeatureGraph.Of(document.Log);
        var length = Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z));
        if (!(length > 0) || !double.IsFinite(length) || !double.IsFinite(point.X) || !double.IsFinite(point.Y) || !double.IsFinite(point.Z))
            return (null, null, "A face needs a finite point and a normal of a length greater than 0.");
        var n = (normal.X / length, normal.Y / length, normal.Z / length);

        // Item 2: the walk from the body to the feature that holds the face.
        var handle = body;
        var feature = graph.Creator(handle);
        while (feature is TranslateCommand or SubtractCommand)
        {
            handle = feature is TranslateCommand translate ? translate.BodyId : ((SubtractCommand)feature).MinuendBodyId;
            feature = graph.Creator(handle);
        }

        DatumResolver.ExtrudeState State(ExtrudeCommand extrude) => new(
            SketchModel.Find(document, extrude.SketchId),
            SketchRebuild.DistanceInEffect(document.Log, extrude.CommandId) ?? extrude.Distance);
        DatumFrame? FrameOf(Guid id) => DatumResolver.Frame(document, id);

        foreach (var reference in Faces(feature, State))
        {
            var (frame, _) = DatumResolver.Resolve(reference, graph, int.MaxValue, State, FrameOf);
            if (frame is not { } face || !OnPlane(face, point, n, out var t))
                continue;
            var on = feature switch
            {
                CreateBoxCommand box => OnBoxFace(box, face, point, t),
                ExtrudeCommand extrude => OnExtrudeFace(reference, face, State(extrude), FrameOf, point, t),
                _ => false,
            };
            if (on)
                return (reference, face, null);
        }

        return (null, null, feature switch
        {
            ExtrudeCommand or CreateBoxCommand =>
                $"No plane face of the feature '{feature.CommandId}' holds the point with that normal. A curved face, or a face of the subtrahend of a Subtract, is not a face that a datum names.",
            null => $"No command in effect creates the body '{handle}'.",
            _ => $"The command '{feature.Name}' that creates the body '{handle}' has no faces that a datum names.",
        });
    }

    // Item 3: the faces of the feature in their order.
    private static IEnumerable<DatumReference> Faces(Command? feature, Func<ExtrudeCommand, DatumResolver.ExtrudeState> state)
    {
        switch (feature)
        {
            case ExtrudeCommand extrude:
                yield return new DatumReference(extrude.CommandId, "start", null);
                yield return new DatumReference(extrude.CommandId, "end", null);
                if (state(extrude).Sketch is { } sketch)
                {
                    foreach (var line in sketch.Lines)
                        yield return new DatumReference(extrude.CommandId, "side", line.Id);
                }
                break;
            case CreateBoxCommand box:
                foreach (var element in DatumResolver.BoxFaces)
                    yield return new DatumReference(box.CommandId, element, null);
                break;
        }
    }

    // Item 3: the normal of the face is the normal of the hit, and the point is on the plane of the face within t.
    private static bool OnPlane(DatumFrame face, (double X, double Y, double Z) point, (double X, double Y, double Z) n, out double t)
    {
        var o = face.Origin;
        var largest = Math.Max(Math.Max(Math.Max(Math.Abs(point.X), Math.Abs(point.Y)), Math.Max(Math.Abs(point.Z), Math.Abs(o.X))), Math.Max(Math.Abs(o.Y), Math.Abs(o.Z)));
        t = 1e-6 * (1 + largest);
        return Dot(face.Normal, n) >= SameNormal && Math.Abs(Along(point, o, face.Normal)) <= t;
    }

    // A face of a box is the rectangle of the two other half sizes. Each axis of a box face holds 0 and ±1.
    private static bool OnBoxFace(CreateBoxCommand box, DatumFrame face, (double X, double Y, double Z) point, double t)
    {
        double Half(DatumAxis axis) => (Math.Abs(axis.X) * box.SizeX / 2) + (Math.Abs(axis.Y) * box.SizeY / 2) + (Math.Abs(axis.Z) * box.SizeZ / 2);
        return Math.Abs(Along(point, face.Origin, face.U)) <= Half(face.U) + t && Math.Abs(Along(point, face.Origin, face.V)) <= Half(face.V) + t;
    }

    // A cap holds the point inside the loop of the profile, in the axes of the plane of the sketch; the origin of the cap
    // goes to the origin of the sketch. A side is the rectangle of its line and of the height of the extrude.
    private static bool OnExtrudeFace(
        DatumReference reference, DatumFrame face, DatumResolver.ExtrudeState state, Func<Guid, DatumFrame?> frameOf,
        (double X, double Y, double Z) point, double t)
    {
        if (state.Sketch is not { } sketch || frameOf(sketch.DatumId) is not { } plane
            || ExtrudeProfile.Parameters(sketch, state.Distance, plane).Parameters is not { } profile)
            return false;

        if (reference.Element is "start" or "end")
        {
            var at = (Along(point, face.Origin, plane.U), Along(point, face.Origin, plane.V));
            return Inside(profile.Loop, at, t);
        }

        var line = sketch.Lines.First(item => item.Id == reference.ItemId);
        var (p, q) = (SketchModel.Point(sketch, line.Start), SketchModel.Point(sketch, line.End));
        var (dx, dy) = (q.X - p.X, q.Y - p.Y);
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        var (s, h) = (Along(point, face.Origin, face.U), Along(point, face.Origin, face.V));
        return s >= -t && s <= length + t && h >= Math.Min(0, state.Distance) - t && h <= Math.Max(0, state.Distance) + t;
    }

    // The point is inside the loop by the rule of an odd number of crossings, or within t of an edge of the loop.
    private static bool Inside(IReadOnlyList<(double X, double Y)> loop, (double X, double Y) at, double t)
    {
        var inside = false;
        for (var i = 0; i < loop.Count; i++)
        {
            var (a, b) = (loop[i], loop[(i + 1) % loop.Count]);
            if (Near(a, b, at, t))
                return true;
            if ((a.Y > at.Y) != (b.Y > at.Y) && at.X < a.X + ((at.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y)))
                inside = !inside;
        }

        return inside;
    }

    // The distance of the point from the segment ab is at most t.
    private static bool Near((double X, double Y) a, (double X, double Y) b, (double X, double Y) at, double t)
    {
        var (dx, dy) = (b.X - a.X, b.Y - a.Y);
        var squared = (dx * dx) + (dy * dy);
        var k = squared > 0 ? Math.Clamp((((at.X - a.X) * dx) + ((at.Y - a.Y) * dy)) / squared, 0, 1) : 0;
        var (ex, ey) = (at.X - (a.X + (k * dx)), at.Y - (a.Y + (k * dy)));
        return (ex * ex) + (ey * ey) <= t * t;
    }

    private static double Along((double X, double Y, double Z) point, DatumPoint origin, DatumAxis axis)
        => ((point.X - origin.X) * axis.X) + ((point.Y - origin.Y) * axis.Y) + ((point.Z - origin.Z) * axis.Z);

    private static double Dot(DatumAxis a, (double X, double Y, double Z) b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
}
