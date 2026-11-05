using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Core.Sketch;
using Engine.Kernel;

namespace Engine.Core.Features;

// ADR-0058 items 1 to 3 (phase K4b, TASK-0184): the solid of a body in the owned kernel, as a projection of the steps in
// effect. Manifold still makes the solid and the mesh of each body; the kernel gives the faces and their names. The
// walk goes from the body to the command that creates it, as the regeneration does: a CreateBox, an Extrude of the
// sketch and the distance in effect on the frame of its datum, a Translate, or a Subtract of two bodies. The kernel
// reads each input double exactly, so the projection does not depend on the platform, and it does not enter the log.
internal static class KernelProjection
{
    // The kernel solid of the body, or the reason that the kernel cannot build it.
    public static (KernelSolid? Solid, string? Reason) Of(Document document, Guid body)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Solid(document, FeatureGraph.Of(document), body);
    }

    private static (KernelSolid? Solid, string? Reason) Solid(Document document, FeatureGraph graph, Guid handle)
    {
        switch (graph.Creator(handle))
        {
            case CreateBoxCommand box:
                return (KernelSolid.Box(box.CommandId, Rational.FromDouble(box.SizeX), Rational.FromDouble(box.SizeY), Rational.FromDouble(box.SizeZ)), null);
            case ExtrudeCommand extrude:
                return Extrude(document, extrude);
            case TranslateCommand translate:
            {
                var (solid, reason) = Solid(document, graph, translate.BodyId);
                return (solid?.Translate(Vector3R.FromDoubles(translate.Dx, translate.Dy, translate.Dz)), reason);
            }
            case SubtractCommand subtract:
            {
                var (minuend, first) = Solid(document, graph, subtract.MinuendBodyId);
                if (minuend is null)
                    return (null, first);
                var (subtrahend, second) = Solid(document, graph, subtract.SubtrahendBodyId);
                if (subtrahend is null)
                    return (null, second);
                var result = PlanarBoolean.Subtract(minuend, subtrahend);
                return result is null ? (null, $"The Subtract '{subtract.CommandId}' leaves no solid in the owned kernel.") : (result, null);
            }
            case null:
                return (null, $"No command in effect creates the body '{handle}'.");
            case var other:
                return (null, $"The owned kernel does not build the command '{other.Name}'.");
        }
    }

    // Item 2: the profile of the sketch in effect, with the item of each side, on the frame of its datum, by the distance
    // in effect. The distance keeps its sign, so the start face is on the plane of the sketch, as ADR-0051 item 1 says.
    private static (KernelSolid? Solid, string? Reason) Extrude(Document document, ExtrudeCommand extrude)
    {
        if (SketchModel.Find(document, extrude.SketchId) is not { } sketch)
            return (null, $"The sketch '{extrude.SketchId}' of the Extrude '{extrude.CommandId}' is not in the Document.");
        if (!SketchSolver.Status(sketch).Solved)
            return (null, $"The sketch '{sketch.Id}' is not solved.");
        var (loop, reason) = SketchProfile.FindWithItems(sketch);
        if (loop is null)
            return (null, reason);
        if (DatumResolver.Frame(document, sketch.DatumId) is not { } frame)
            return (null, $"The datum '{sketch.DatumId}' of the sketch '{sketch.Id}' is not in the Document.");
        var distance = SketchRebuild.DistanceInEffect(document, extrude.CommandId) ?? extrude.Distance;
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
}
