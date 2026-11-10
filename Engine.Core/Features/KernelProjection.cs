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
                return Extrude(extrude, state(extrude), frameOf);
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
    private static (KernelSolid? Solid, string? Reason) Extrude(ExtrudeCommand extrude, DatumResolver.ExtrudeState state, Func<Guid, DatumFrame?> frameOf)
    {
        if (state.Sketch is not { } sketch)
            return (null, $"The sketch '{extrude.SketchId}' of the Extrude '{extrude.CommandId}' is not in the Document.");
        if (!SketchSolver.Status(sketch).Solved)
            return (null, $"The sketch '{sketch.Id}' is not solved.");
        var (loop, reason) = SketchProfile.FindWithItems(sketch);
        if (loop is null)
            return (null, reason);
        if (frameOf(sketch.DatumId) is not { } frame)
            return (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' is not in the Document.");
        var distance = state.Distance;
        if (distance == 0 || !double.IsFinite(distance))
            return (null, $"The Extrude '{extrude.CommandId}' has the distance {distance}.");

        var profile = loop.Select(vertex => new ProfileVertex(Rational.FromDouble(vertex.X), Rational.FromDouble(vertex.Y), vertex.Item)).ToList();
        return (KernelSolid.Extrude(
            extrude.CommandId,
            profile,
            Vector3R.FromDoubles(frame.Origin.X, frame.Origin.Y, frame.Origin.Z),
            Vector3R.FromDoubles(frame.U.X, frame.U.Y, frame.U.Z),
            Vector3R.FromDoubles(frame.V.X, frame.V.Y, frame.V.Z),
            Vector3R.FromDoubles(frame.Normal.X, frame.Normal.Y, frame.Normal.Z),
            Rational.FromDouble(distance)), null);
    }

    // ADR-0089 item 5: the kernel solid of a closed mesh of a handler, with the face 'surface'.
    private static KernelSolid Mesh(Guid feature, (double[] Positions, int[] Triangles) mesh)
    {
        var vertices = new Vector3R[mesh.Positions.Length / 3];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = Vector3R.FromDoubles(mesh.Positions[3 * i], mesh.Positions[(3 * i) + 1], mesh.Positions[(3 * i) + 2]);
        return KernelSolid.Mesh(feature, "surface", vertices, mesh.Triangles);
    }
}
