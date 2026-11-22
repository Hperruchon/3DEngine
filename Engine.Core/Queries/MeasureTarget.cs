using Engine.Contracts;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 2, 6 and 7 (phase K15a, TASK-0432): the solid of a body for a measure, the name of a face or an edge in
// it, and the refusals. Items 11 and 12 (phase K15b, TASK-0434): a whole curved face resolves to its curve. Item 17 (phase
// K15c, TASK-0444): the side of an arc resolves to its curve and its sector. Item 21 (phase K15d, TASK-0450): the side of a
// hole or of a tube resolves to its parts. The three measure queries read the solid of the owned kernel through
// KernelProjection, as GetFaces does, so the window and an agent measure the same solid by one path.
internal static class MeasureTarget
{
    // One side of GetDistance: a body, a face of it, or the edge between two of its faces. A face is the reference of
    // GetFaces (the feature, the element and the item); an edge is the pair of the references of its two faces.
    internal sealed record Side(
        Guid BodyId,
        Guid? FeatureId,
        string? Element,
        Guid? ItemId,
        Guid? SecondFeatureId,
        string? SecondElement,
        Guid? SecondItemId);

    // The pieces of one side, and the solid when the side is a body, which holds the inside of the body too. ADR-0180 item
    // 11 (phase K15b, TASK-0434): Curve is the whole curved face of the design when the side is one; the pieces are then its
    // facets. Item 21 (phase K15d, TASK-0450): the side of a hole in two parts or more gives each part in Parts, and Curve is
    // the first.
    internal sealed record Resolved(IReadOnlyList<NearestPoints.Piece> Pieces, KernelSolid? Solid, MeasureCurve.Surface? Curve = null, IReadOnlyList<MeasureCurve.Surface>? Parts = null);

    // Item 7: the solid of a live body that the owned kernel builds, or the refusal. The refusals are those of GetFaces
    // (ADR-0058 item 3), and a mesh body is refused as GetFaces refuses it.
    public static (KernelSolid? Solid, ErrorDetail? Error) SolidOf(Document document, Guid bodyId)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Bodies.FirstOrDefault(body => body.Handle.Id == bodyId) is not { } record)
            return (null, new ErrorDetail(DiagnosticCodes.GeomBodyNotFound, $"No live body with id '{bodyId}' exists in the Document."));
        if (record.Kind == Commands.ImportStlCommandHandler.MeshKind)
            return (null, new ErrorDetail(DiagnosticCodes.MeshUnsupported,
                $"The body '{bodyId}' is a mesh body of an STL file, which the owned kernel does not hold."));
        if (record.Fault is { } fault)
            return (null, new ErrorDetail(DiagnosticCodes.GeomBodyFaulted,
                $"The body '{bodyId}' has the fault {fault.Code}, so its solid comes from an earlier state and it has no measure."));
        var (solid, reason) = KernelProjection.OfRecord(document, record);
        return solid is null ? (null, new ErrorDetail(DiagnosticCodes.GeomFacesFailed, reason!)) : (solid, null);
    }

    // Item 6: a face name is curved when its pieces lie on more than one plane: the facets of a cylinder, a cone, a
    // sphere, a torus or an arc of a profile. GetFaces gives no normal for the same names.
    public static bool IsCurved(KernelSolid solid, FaceTag tag)
    {
        ArgumentNullException.ThrowIfNull(solid);
        PlaneR? first = null;
        foreach (var polygon in solid.Polygons)
        {
            if (polygon.Tag != tag)
                continue;
            if (first is null)
                first = polygon.Plane;
            else if (polygon.Plane != first.Value)
                return true;
        }

        return false;
    }

    // True when a face name of the solid is curved.
    public static bool HasCurve(KernelSolid solid)
    {
        ArgumentNullException.ThrowIfNull(solid);
        return solid.Polygons.Select(polygon => polygon.Tag).Distinct().Any(tag => IsCurved(solid, tag));
    }

    // Items 2, 6 and 12: the plane of a face of one plane, or the whole curved face of the design (phase K15b, TASK-0434),
    // or the refusal. GetAngle reads it.
    public static (PlaneR? Plane, MeasureCurve.Surface? Curve, ErrorDetail? Error) FaceOf(Document document, KernelSolid solid, Guid bodyId, FaceTag tag)
    {
        ArgumentNullException.ThrowIfNull(solid);
        if (solid.Polygons.FirstOrDefault(polygon => polygon.Tag == tag) is not { } polygon)
            return (null, null, NotFound($"The body '{bodyId}' has no face '{tag}'."));
        if (!IsCurved(solid, tag))
            return (polygon.Plane, null, null);
        var (curves, reason) = CurveOf(document, bodyId, solid, tag);
        return curves is null ? (null, null, Curved($"The face '{tag}' of the body '{bodyId}' is curved, and {reason}.")) : (null, curves[0], null);
    }

    // Items 11, 17 and 21: the curve of a curved face: the whole face of one command, the side of an arc, or (phase K15d,
    // TASK-0450) the parts of the side of a hole or of a tube that CutMass gives for a body with a Subtract. GetConnectors
    // reads MeasureCurve.Whole only, so the side of a hole stays Curved there.
    internal static (IReadOnlyList<MeasureCurve.Surface>? Curves, string? Reason) CurveOf(Document document, Guid bodyId, KernelSolid solid, FaceTag tag)
    {
        var (curve, reason) = MeasureCurve.Whole(document, bodyId, solid, tag, arcs: true);
        if (curve is not null)
            return ([curve], null);
        return KernelProjection.Placed(document, bodyId) is null ? CutMass.Sides(document, bodyId, tag) : (null, reason);
    }

    // Items 2 and 6: the pieces of one side of GetDistance, or the refusal. A body gives each polygon of its solid, a
    // face the polygons of its name, and an edge the segments of the B-Rep edges between its two names (ADR-0077 item 6).
    public static (Resolved? Side, ErrorDetail? Error) Resolve(Document document, Side side, string label)
    {
        ArgumentNullException.ThrowIfNull(side);
        var (solid, error) = SolidOf(document, side.BodyId);
        if (solid is null)
            return (null, error);

        var hasFace = side.FeatureId is not null || side.Element is not null || side.ItemId is not null;
        var hasSecond = side.SecondFeatureId is not null || side.SecondElement is not null || side.SecondItemId is not null;
        if (!hasFace && !hasSecond)
        {
            if (HasCurve(solid))
                return (null, Curved($"The body '{side.BodyId}' of side {label} has a curved face."));
            return (new Resolved([.. solid.Polygons.Select(polygon => new NearestPoints.Piece(polygon.Vertices))], solid), null);
        }

        if (side.FeatureId is not { } feature || side.Element is not { } element)
            return (null, NotFound($"Side {label} gives a part of the name of a face only: a face needs its feature and its element."));
        var tag = new FaceTag(feature, element, side.ItemId);
        if (!solid.Polygons.Any(polygon => polygon.Tag == tag))
            return (null, NotFound($"The body '{side.BodyId}' of side {label} has no face '{tag}'."));
        if (!hasSecond)
        {
            IReadOnlyList<MeasureCurve.Surface>? curves = null;
            if (IsCurved(solid, tag))
            {
                (curves, var reason) = CurveOf(document, side.BodyId, solid, tag);
                if (curves is null)
                    return (null, Curved($"The face '{tag}' of side {label} is curved, and {reason}."));
            }

            var pieces = solid.Polygons.Where(polygon => polygon.Tag == tag).Select(polygon => new NearestPoints.Piece(polygon.Vertices)).ToList();
            return (new Resolved(pieces, null, curves?[0], curves is { Count: > 1 } ? curves : null), null);
        }

        if (side.SecondFeatureId is not { } secondFeature || side.SecondElement is not { } secondElement)
            return (null, NotFound($"Side {label} gives a part of the name of the second face of an edge only."));
        var second = new FaceTag(secondFeature, secondElement, side.SecondItemId);
        if (second == tag || !solid.Polygons.Any(polygon => polygon.Tag == second))
            return (null, NotFound($"The body '{side.BodyId}' of side {label} has no edge between '{tag}' and '{second}'."));

        var brep = Brep.Of(solid);
        var segments = brep.Edges
            .Where(edge => edge.Faces.Any(face => brep.Faces[face].Tag == tag) && edge.Faces.Any(face => brep.Faces[face].Tag == second))
            .Select(edge => new NearestPoints.Piece([brep.Vertices[edge.From], brep.Vertices[edge.To]]))
            .ToList();
        if (segments.Count == 0)
            return (null, NotFound($"The body '{side.BodyId}' of side {label} has no edge between '{tag}' and '{second}'."));
        if (IsCurved(solid, tag) || IsCurved(solid, second))
            return (null, Curved($"The edge between '{tag}' and '{second}' of side {label} has a curved face."));
        return (new Resolved(segments, null), null);
    }

    private static ErrorDetail NotFound(string message) => new(DiagnosticCodes.MeasureNameNotFound, message);

    // ADR-0180 items 6, 11 to 17 and 20 to 23: a curved case that phases K15b, K15c and K15d give no closed form for.
    internal static ErrorDetail Curved(string message)
        => new(DiagnosticCodes.MeasureCurved, $"{message} The measure queries measure a whole curved face of a cylinder, a cone or a sphere, the side of an arc, and the side of a hole or of a tube, in the cases of ADR-0180 items 6, 11 to 17 and 20 to 22 only.");
}
