using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Sketch;
using Engine.Kernel;

namespace Engine.Core.Features;

// ADR-0058 items 1 to 3 (phase K4b, TASK-0184): the solid of a body in the owned kernel, as a projection of the steps in
// effect. Manifold still makes the solid and the mesh of each body; the kernel gives the faces and their names. The
// walk goes from the body to the command that creates it, as the regeneration does: a CreateBox, a CreateCylinder
// (ADR-0076), a CreateCone (ADR-0089; finding E188 of the codebase review of 2026-11-08, TASK-0296), a CreateSphere
// or a CreateTorus (ADR-0089 item 5, TASK-0289), an Extrude of the
// sketch and the distance in effect on the frame of its datum, a Translate, a Rotate (ADR-0112 item 5), or a Subtract of two bodies. The kernel
// reads each input double exactly, so the projection does not depend on the platform, and it does not enter the log.
//
// ADR-0059 items 1 and 2 (phase K4c, TASK-0185): the projection also reads a state that is not committed yet, the state
// of an edit before its commit, and it keeps the solid of each committed geometry key.
internal static class KernelProjection
{
    // ADR-0059 item 2: the count of the committed solids that the projection keeps; the oldest goes first.
    public const int CacheSize = 64;

    private static readonly object Gate = new();
    // Finding E135 of the codebase review of 2026-11-05 (b) (TASK-0192): the key was the geometry key alone, which is
    // unique in one Document only (a client chooses the command identifier of a box), so two sessions in one process
    // read the solid of each other. The key holds the identity of the Document too.
    private static readonly Dictionary<(Guid Document, Guid Key), KernelSolid> Kept = [];
    private static readonly Queue<(Guid Document, Guid Key)> KeptOrder = new();

    // The kernel solid of the body in the committed state of the Document, or the reason that the kernel cannot build it.
    public static (KernelSolid? Solid, string? Reason) Of(Document document, Guid body)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Of(FeatureGraph.Of(document), body, Committed(document), id => DatumResolver.Frame(document, id));
    }

    // The kernel solid of the body in one state: state gives the sketch and the distance of an extrude, and frameOf the
    // frame of a datum.
    public static (KernelSolid? Solid, string? Reason) Of(
        FeatureGraph graph, Guid body, Func<ExtrudeCommand, DatumResolver.ExtrudeState> state, Func<Guid, DatumFrame?> frameOf)
    {
        ArgumentNullException.ThrowIfNull(graph);
        return Solid(graph, body, state, frameOf);
    }

    // ADR-0059 item 2: the solid of a live body with no fault, kept under its geometry key. A rebuild gives a body a new
    // key (ADR-0037 item 2), and a key comes from the command and the body (ADR-0037 item 3), so a committed key names
    // one solid. A body with a fault keeps the solid of an earlier state, so it gives no solid.
    public static (KernelSolid? Solid, string? Reason) OfRecord(Document document, BodyRecord record)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(record);
        if (record.Fault is { } fault)
            return (null, $"The body '{record.Handle.Id}' has the fault {fault.Code}, so its solid comes from an earlier state.");
        var key = (document.DocumentId, record.GeometryKey);
        lock (Gate)
        {
            if (Kept.TryGetValue(key, out var kept))
                return (kept, null);
        }

        var (solid, reason) = Of(document, record.Handle.Id);
        if (solid is null)
            return (null, reason);
        lock (Gate)
        {
            if (Kept.TryAdd(key, solid))
            {
                KeptOrder.Enqueue(key);
                if (KeptOrder.Count > CacheSize)
                    Kept.Remove(KeptOrder.Dequeue());
            }
        }

        return (solid, null);
    }

    // True when the projection keeps a solid of the Document under the key, for a test of item 2.
    internal static bool IsKept(Guid document, Guid key)
    {
        lock (Gate)
            return Kept.ContainsKey((document, key));
    }

    // ADR-0059 item 3: the names of the faces of a solid.
    public static IReadOnlySet<FaceTag> Tags(KernelSolid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);
        return solid.Polygons.Select(polygon => polygon.Tag).ToHashSet();
    }

    // ADR-0059 item 3: the handle at the end of the chain of consumers of the body of a feature, and true when a Subtract
    // is on the chain. A consumer that is not a Translate, a Rotate (ADR-0112 item 6) or a Subtract gives no end.
    public static (Guid? End, bool Cut) End(FeatureGraph graph, Guid feature)
    {
        ArgumentNullException.ThrowIfNull(graph);
        var (handle, cut) = (feature, false);
        while (graph.Consumer(handle) is { } consumer)
        {
            if (consumer is SubtractCommand)
                cut = true;
            else if (consumer is not (TranslateCommand or RotateCommand))
                return (null, cut);
            handle = consumer.CommandId;
        }

        return (handle, cut);
    }

    // ADR-0143 item 1 (phase K9b, TASK-0376): the curve of the design of each curved side of the live body, under the tag
    // of its faces: the base, the axis and the radius of a cylinder or a cone, and the centre and the radius of a sphere. The
    // walk is the walk of the solid: a Translate moves each curve, a Rotate turns it with the exact turn of the solid, and a
    // Subtract keeps the curves of the two bodies, as it keeps their tags. A torus and a box give no curve.
    public static IReadOnlyDictionary<FaceTag, Drawing.SideCurve> Curves(Document document, Guid body)
    {
        ArgumentNullException.ThrowIfNull(document);
        return CurvesOf(FeatureGraph.Of(document), body, Committed(document), id => DatumResolver.Frame(document, id));
    }

    private static Dictionary<FaceTag, Drawing.SideCurve> CurvesOf(
        FeatureGraph graph, Guid handle, Func<ExtrudeCommand, DatumResolver.ExtrudeState> state, Func<Guid, DatumFrame?> frameOf)
    {
        var two = (Rational)2;
        switch (graph.Creator(handle))
        {
            case CreateCylinderCommand cylinder:
            {
                var height = Rational.FromDouble(cylinder.Height);
                return new() { [new FaceTag(cylinder.CommandId, "side")] = new(Drawing.SideCurveKind.Cylinder,
                    new Vector3R(Rational.Zero, Rational.Zero, -height / two), new Vector3R(Rational.Zero, Rational.Zero, height), Rational.FromDouble(cylinder.Radius)) };
            }
            case CreateConeCommand cone:
            {
                var height = Rational.FromDouble(cone.Height);
                return new() { [new FaceTag(cone.CommandId, "side")] = new(Drawing.SideCurveKind.Cone,
                    new Vector3R(Rational.Zero, Rational.Zero, -height / two), new Vector3R(Rational.Zero, Rational.Zero, height), Rational.FromDouble(cone.Radius)) };
            }
            case CreateSphereCommand sphere:
                return new() { [new FaceTag(sphere.CommandId, "surface")] = new(Drawing.SideCurveKind.Sphere,
                    new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero), new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero), Rational.FromDouble(sphere.Radius)) };
            case ExtrudeCommand extrude:
                return ExtrudeCurves(graph, extrude, state, frameOf);
            case TranslateCommand translate:
            {
                var offset = Vector3R.FromDoubles(translate.Dx, translate.Dy, translate.Dz);
                return CurvesOf(graph, translate.BodyId, state, frameOf).ToDictionary(entry => entry.Key, entry => entry.Value with { Base = entry.Value.Base + offset });
            }
            case RotateCommand rotate when RotateCommandHandler.Turn(rotate) is { } r:
            {
                var (x, y, z) = (Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz));
                Vector3R Map(Vector3R q) => (x * q.X) + (y * q.Y) + (z * q.Z);
                return CurvesOf(graph, rotate.BodyId, state, frameOf).ToDictionary(entry => entry.Key, entry => entry.Value with { Base = Map(entry.Value.Base), Axis = Map(entry.Value.Axis) });
            }
            case SubtractCommand subtract:
            {
                var curves = CurvesOf(graph, subtract.MinuendBodyId, state, frameOf);
                foreach (var (tag, curve) in CurvesOf(graph, subtract.SubtrahendBodyId, state, frameOf))
                    curves.TryAdd(tag, curve);
                return curves;
            }
            default:
                return [];
        }
    }

    // ADR-0143 item 1: each circle and each arc of the sketch of an extrude is a cylinder: its centre on the exact frame of
    // the datum, the axis along the normal by the distance in effect, and the radius of the circle, or of the arc from its
    // centre to its start.
    private static Dictionary<FaceTag, Drawing.SideCurve> ExtrudeCurves(
        FeatureGraph graph, ExtrudeCommand extrude, Func<ExtrudeCommand, DatumResolver.ExtrudeState> stateOf, Func<Guid, DatumFrame?> frameOf)
    {
        var state = stateOf(extrude);
        if (state.Sketch is not { } sketch || ExactFrame(graph, sketch.DatumId, stateOf, frameOf) is not var (origin, u, v, n)
            || state.Distance == 0 || !double.IsFinite(state.Distance))
            return [];
        var axis = n * Rational.FromDouble(state.Distance);
        var points = sketch.Points.ToDictionary(point => point.Id);
        Vector3R At(SketchPoint p) => origin + (u * Rational.FromDouble(p.X)) + (v * Rational.FromDouble(p.Y));
        var curves = new Dictionary<FaceTag, Drawing.SideCurve>();
        foreach (var circle in sketch.Circles)
        {
            if (points.TryGetValue(circle.Center, out var c))
                curves[new FaceTag(extrude.CommandId, "side", circle.Id)] = new(Drawing.SideCurveKind.Cylinder, At(c), axis, Rational.FromDouble(circle.Radius));
        }

        foreach (var arc in sketch.Arcs)
        {
            if (points.TryGetValue(arc.Center, out var c) && points.TryGetValue(arc.Start, out var s))
            {
                var (dx, dy) = (s.X - c.X, s.Y - c.Y);
                curves[new FaceTag(extrude.CommandId, "side", arc.Id)] = new(Drawing.SideCurveKind.Cylinder, At(c), axis, Rational.FromDouble(Math.Sqrt((dx * dx) + (dy * dy))));
            }
        }

        return curves;
    }

    // The state of the committed Document: the sketch and the distance in effect of an extrude.
    private static Func<ExtrudeCommand, DatumResolver.ExtrudeState> Committed(Document document)
        => extrude => new DatumResolver.ExtrudeState(
            SketchModel.Find(document, extrude.SketchId),
            SketchRebuild.DistanceInEffect(document, extrude.CommandId) ?? extrude.Distance);

    private static (KernelSolid? Solid, string? Reason) Solid(
        FeatureGraph graph, Guid handle, Func<ExtrudeCommand, DatumResolver.ExtrudeState> state, Func<Guid, DatumFrame?> frameOf)
    {
        switch (graph.Creator(handle))
        {
            case CreateBoxCommand box:
                return (KernelSolid.Box(box.CommandId, Rational.FromDouble(box.SizeX), Rational.FromDouble(box.SizeY), Rational.FromDouble(box.SizeZ)), null);
            // ADR-0076 item 3: the polygon of the handler, read exactly.
            case CreateCylinderCommand cylinder:
                return (KernelSolid.Cylinder(
                    cylinder.CommandId,
                    [.. CreateCylinderCommandHandler.Circle(cylinder.Radius).Select(p => (Rational.FromDouble(p.X), Rational.FromDouble(p.Y)))],
                    Rational.FromDouble(cylinder.Height)), null);
            // ADR-0089 item 3: the polygon of the handler, read exactly.
            case CreateConeCommand cone:
                return (KernelSolid.Cone(
                    cone.CommandId,
                    [.. CreateCylinderCommandHandler.Circle(cone.Radius).Select(p => (Rational.FromDouble(p.X), Rational.FromDouble(p.Y)))],
                    Rational.FromDouble(cone.Height)), null);
            // ADR-0089 item 5: the mesh of the handler, read exactly.
            case CreateSphereCommand sphere:
                return (Mesh(sphere.CommandId, CreateSphereCommandHandler.Mesh(sphere.Radius)), null);
            case CreateTorusCommand torus:
                return (Mesh(torus.CommandId, CreateTorusCommandHandler.Mesh(torus.Radius, torus.TubeRadius)), null);
            case ExtrudeCommand extrude:
                return Extrude(graph, extrude, state, frameOf);
            case TranslateCommand translate:
            {
                var (solid, reason) = Solid(graph, translate.BodyId, state, frameOf);
                return (solid?.Translate(Vector3R.FromDoubles(translate.Dx, translate.Dy, translate.Dz)), reason);
            }
            // ADR-0112 item 5: the kernel reads the nine doubles of the turn exactly, as the backend takes them.
            case RotateCommand rotate:
            {
                if (RotateCommandHandler.Turn(rotate) is not { } r)
                    return (null, $"The Rotate '{rotate.CommandId}' has a quaternion that gives no turn.");
                var (solid, reason) = Solid(graph, rotate.BodyId, state, frameOf);
                return (solid?.Rotate(Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz)), reason);
            }
            case SubtractCommand subtract:
            {
                var (minuend, first) = Solid(graph, subtract.MinuendBodyId, state, frameOf);
                if (minuend is null)
                    return (null, first);
                var (subtrahend, second) = Solid(graph, subtract.SubtrahendBodyId, state, frameOf);
                if (subtrahend is null)
                    return (null, second);
                var result = PlanarBoolean.Subtract(minuend, subtrahend);
                return result is null ? (null, $"The Subtract '{subtract.CommandId}' leaves no solid in the owned kernel.") : (result, null);
            }
            case ImportStlCommand:
                return (null, $"The body '{handle}' is a mesh body of an STL file, which the owned kernel does not build (ADR-0083 item 4).");
            case null:
                return (null, $"No command in effect creates the body '{handle}'.");
            case var other:
                return (null, $"The owned kernel does not build the command '{other.Name}'.");
        }
    }

    // Item 2: the profile of the sketch in the state, with the item of each side, on the frame of its datum, by the
    // distance in the state. The distance keeps its sign, so the start face is on the plane of the sketch, as ADR-0051
    // item 1 says.
    private static (KernelSolid? Solid, string? Reason) Extrude(
        FeatureGraph graph, ExtrudeCommand extrude, Func<ExtrudeCommand, DatumResolver.ExtrudeState> stateOf, Func<Guid, DatumFrame?> frameOf)
    {
        var state = stateOf(extrude);
        if (state.Sketch is not { } sketch)
            return (null, $"The sketch '{extrude.SketchId}' of the Extrude '{extrude.CommandId}' is not in the Document.");
        if (!SketchSolver.Status(sketch).Solved)
            return (null, $"The sketch '{sketch.Id}' is not solved.");
        var (loop, reason) = SketchProfile.FindWithItems(sketch);
        if (loop is null)
            return (null, reason);
        if (ExactFrame(graph, sketch.DatumId, stateOf, frameOf) is not { } frame)
            return (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' is not in the Document.");
        var distance = state.Distance;
        if (distance == 0 || !double.IsFinite(distance))
            return (null, $"The Extrude '{extrude.CommandId}' has the distance {distance}.");

        var profile = loop.Select(vertex => new ProfileVertex(Rational.FromDouble(vertex.X), Rational.FromDouble(vertex.Y), vertex.Item)).ToList();
        return (KernelSolid.Extrude(
            extrude.CommandId,
            profile,
            frame.Origin,
            frame.U,
            frame.V,
            frame.Normal,
            Rational.FromDouble(distance)), null);
    }

    // Finding E200 of the codebase review of 2026-11-10 (phase P0.140, TASK-0338): the frame of a datum on a face in exact
    // numbers, built as the kernel builds the solid of that face: a face of an extrude from the exact frame of its own
    // sketch, then the Translate and Rotate steps of its chain; so the start face of a pocket and the face are one plane.
    // An origin plane, and a datum that does not resolve, keep the frame of the Document. Until TASK-0338 the kernel read
    // that frame for each datum, which the Document builds in doubles, and a pocket left the face whole and a closed cavity
    // under it.
    private static (Vector3R Origin, Vector3R U, Vector3R V, Vector3R Normal)? ExactFrame(
        FeatureGraph graph, Guid datum, Func<ExtrudeCommand, DatumResolver.ExtrudeState> stateOf, Func<Guid, DatumFrame?> frameOf)
    {
        var placed = graph.Datum(datum) is { } create
            ? DatumResolver.Placement(new DatumReference(create.FeatureId, create.Element, create.ItemId), graph, graph.Order(create.CommandId), stateOf, frameOf)
            : null;
        var carrier = placed is { } p
            ? p.Feature is ExtrudeCommand extrude ? ExtrudeFace(graph, extrude, graph.Datum(datum)!, stateOf, frameOf) : Exact(p.Carrier)
            : null;
        if (placed is not { } place || carrier is not { } frame)
            return frameOf(datum) is { } stored ? Exact(stored) : null;

        var (origin, u, v, n) = frame;
        foreach (var step in place.Steps)
        {
            if (step is TranslateCommand translate)
            {
                origin += Vector3R.FromDoubles(translate.Dx, translate.Dy, translate.Dz);
            }
            else if (step is RotateCommand rotate && RotateCommandHandler.Turn(rotate) is { } r)
            {
                var (x, y, z) = (Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz));
                Vector3R Map(Vector3R q) => (x * q.X) + (y * q.Y) + (z * q.Z);
                (origin, u, v, n) = (Map(origin), Map(u), Map(v), Map(n));
            }
        }

        // ADR-0059 item 4: a face of a subtrahend is a wall of the hole, so (u, v, n) becomes (u, -v, -n).
        return place.Turned ? (origin, u, -v, -n) : (origin, u, v, n);
    }

    // E200: the start, the end or a side of an extrude, from the exact frame of its sketch, as DatumResolver gives the face
    // in doubles. The axis along a side is the exact direction of the side over its length in doubles, so it lies in the
    // exact plane of the side and has the length 1 within the rounding; a sketch on that side keeps its sizes.
    private static (Vector3R Origin, Vector3R U, Vector3R V, Vector3R Normal)? ExtrudeFace(
        FeatureGraph graph, ExtrudeCommand extrude, CreateDatumCommand create, Func<ExtrudeCommand, DatumResolver.ExtrudeState> stateOf, Func<Guid, DatumFrame?> frameOf)
    {
        var state = stateOf(extrude);
        if (state.Sketch is not { } sketch || ExactFrame(graph, sketch.DatumId, stateOf, frameOf) is not var (o, u, v, n)
            || frameOf(sketch.DatumId) is not { } stored || ExtrudeProfile.Parameters(sketch, state.Distance, stored).Parameters is not { } profile)
            return null;
        var d = Rational.FromDouble(state.Distance);
        var forward = state.Distance > 0;
        switch (create.Element)
        {
            case "start":
                return forward ? (o, u, -v, -n) : (o, u, v, n);
            case "end":
                var top = o + (n * d);
                return forward ? (top, u, v, n) : (top, u, -v, -n);
        }

        if (DatumResolver.SideEnds(extrude, sketch, new DatumReference(create.FeatureId, create.Element, create.ItemId), profile).Ends is not var (from, to))
            return null;
        var (px, py, qx, qy) = (Rational.FromDouble(from.X), Rational.FromDouble(from.Y), Rational.FromDouble(to.X), Rational.FromDouble(to.Y));
        var (tx, ty) = (to.X - from.X, to.Y - from.Y);
        var length = Math.Sqrt((tx * tx) + (ty * ty));
        if (!(length > 0) || !double.IsFinite(length))
            return null;
        var along = ((u * (qx - px)) + (v * (qy - py))) * Rational.FromDouble(1 / length);
        return (o + (u * px) + (v * py), along, n, along.Cross(n));
    }

    private static (Vector3R Origin, Vector3R U, Vector3R V, Vector3R Normal) Exact(DatumFrame frame)
        => (Vector3R.FromDoubles(frame.Origin.X, frame.Origin.Y, frame.Origin.Z), Vector3R.FromDoubles(frame.U.X, frame.U.Y, frame.U.Z),
            Vector3R.FromDoubles(frame.V.X, frame.V.Y, frame.V.Z), Vector3R.FromDoubles(frame.Normal.X, frame.Normal.Y, frame.Normal.Z));

    // ADR-0089 item 5: the kernel solid of a closed mesh of a handler, with the face 'surface'.
    private static KernelSolid Mesh(Guid feature, (double[] Positions, int[] Triangles) mesh)
    {
        var vertices = new Vector3R[mesh.Positions.Length / 3];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = Vector3R.FromDoubles(mesh.Positions[3 * i], mesh.Positions[(3 * i) + 1], mesh.Positions[(3 * i) + 2]);
        return KernelSolid.Mesh(feature, "surface", vertices, mesh.Triangles);
    }
}
