using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 item 14 (phase K15c, TASK-0444): the volume and the centre of the curve of a body with a Subtract whose tool is
// one curved command: a cylinder, a cone, a sphere or an extrude of one circle, with only Translate and Rotate steps after
// it. The tool is a solid of revolution about the segment from P to P + A, with the square of its radius R² q(s) at the
// part s of the segment. Its curve meets the body before the Subtract only at planes across the axis when each other face
// of that body keeps the distance R from the segment in the slab 0 ≤ s ≤ 1. Then the body holds a part of the tool between
// two of those planes either whole or not at all, and the removed volume is π R² |A| times the integral of q over the parts
// that it holds. The decisions are exact; the volume and the centre leave as doubles, with π and one root of A · A.
internal static class CutMass
{
    // The profile of a tool: q(s) = 1 for a cylinder, (1 - s)² for a cone with its apex at s = 1, and 4 s (1 - s) for a
    // sphere on the diameter from P to P + A.
    private enum Kind
    {
        Cylinder,
        Cone,
        Sphere,
    }

    // A tool: its segment, its largest radius, its profile and the feature of its faces.
    private sealed record Tool(Vector3R Base, Vector3R Axis, Rational Radius, Kind Kind, Guid Feature);

    // The volume, the centre and the tools of the Subtract steps of a step of the walk, in the place of that step.
    private sealed record Cut(double Volume, Vector3R Centre, IReadOnlyList<Tool> Tools);

    // The mass of the curve of the body, or null when its chain has no Subtract of a curved tool or has no closed form.
    public static MassProperties? Of(Document document, Guid body)
    {
        ArgumentNullException.ThrowIfNull(document);
        var graph = FeatureGraph.Of(document);
        if (!HasTool(document, graph, body) || Walk(document, graph, body) is not { Tools.Count: > 0 } cut)
            return null;
        var (x, y, z) = cut.Centre.ToDoubles();
        return new MassProperties(cut.Volume, new DatumPoint(x, y, z), false);
    }

    // True when a Subtract of a curved tool is on the chain of Translate, Rotate and Subtract steps under the handle. It
    // reads no solid, so a body with no such Subtract pays nothing.
    private static bool HasTool(Document document, FeatureGraph graph, Guid handle)
    {
        while (true)
        {
            switch (graph.Creator(handle))
            {
                case TranslateCommand translate:
                    handle = translate.BodyId;
                    break;
                case RotateCommand rotate:
                    handle = rotate.BodyId;
                    break;
                case SubtractCommand subtract:
                    if (ToolOf(document, subtract.SubtrahendBodyId) is not null)
                        return true;
                    handle = subtract.MinuendBodyId;
                    break;
                default:
                    return false;
            }
        }
    }

    private static Cut? Walk(Document document, FeatureGraph graph, Guid handle)
    {
        switch (graph.Creator(handle))
        {
            case TranslateCommand translate:
            {
                if (Walk(document, graph, translate.BodyId) is not { } moved)
                    return null;
                var offset = Vector3R.FromDoubles(translate.Dx, translate.Dy, translate.Dz);
                return new Cut(moved.Volume, moved.Centre + offset, [.. moved.Tools.Select(tool => tool with { Base = tool.Base + offset })]);
            }

            case RotateCommand rotate:
            {
                if (RotateCommandHandler.Turn(rotate) is not { } r || Walk(document, graph, rotate.BodyId) is not { } turned)
                    return null;
                var (x, y, z) = (Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz));
                Vector3R Map(Vector3R q) => (x * q.X) + (y * q.Y) + (z * q.Z);
                return new Cut(turned.Volume, Map(turned.Centre), [.. turned.Tools.Select(tool => tool with { Base = Map(tool.Base), Axis = Map(tool.Axis) })]);
            }

            case SubtractCommand subtract when ToolOf(document, subtract.SubtrahendBodyId) is { } tool:
            {
                if (Walk(document, graph, subtract.MinuendBodyId) is not { } minuend
                    || KernelProjection.Of(document, subtract.MinuendBodyId).Solid is not { } solid)
                    return null;
                return Remove(minuend, solid, tool);
            }

            case null:
                return null;

            default:
            {
                // The first body of the chain: a body of plane faces, with the exact values of its solid.
                if (KernelProjection.Of(document, handle).Solid is not { } solid || MeasureTarget.HasCurve(solid))
                    return null;
                var mass = GetMassPropertiesQueryHandler.Of(solid);
                return new Cut(mass.Volume, Vector3R.FromDoubles(mass.Centre.X, mass.Centre.Y, mass.Centre.Z), []);
            }
        }
    }

    // The curve of a tool body, or null when it is not one curved command with only Translate and Rotate steps after it.
    private static Tool? ToolOf(Document document, Guid handle)
    {
        if (KernelProjection.Placed(document, handle) is not { } placed)
            return null;
        Drawing.SideCurve? Single() => KernelProjection.Curves(document, handle).Values.SingleOrDefault();
        switch (placed.Creator)
        {
            case CreateCylinderCommand cylinder when Single() is { } curve:
                return new Tool(curve.Base, curve.Axis, curve.Radius, Kind.Cylinder, cylinder.CommandId);
            case CreateConeCommand cone when Single() is { } curve:
                return new Tool(curve.Base, curve.Axis, curve.Radius, Kind.Cone, cone.CommandId);
            case CreateSphereCommand sphere:
            {
                var r = Rational.FromDouble(sphere.Radius);
                var zero = Rational.Zero;
                return new Tool(placed.Point(new Vector3R(zero, zero, -r)), placed.Direction(new Vector3R(zero, zero, r * 2)), r, Kind.Sphere, sphere.CommandId);
            }

            case ExtrudeCommand extrude when KernelProjection.ExtrudeDesign(document, extrude) is var (sketch, _, _, _, _, _)
                && sketch.Circles.Count == 1 && sketch.Arcs.Count == 0 && sketch.Lines.Count == 0 && Single() is { } curve:
                return new Tool(curve.Base, curve.Axis, curve.Radius, Kind.Cylinder, extrude.CommandId);
            default:
                return null;
        }
    }

    // The body before the Subtract less the part of the tool that it holds. Each earlier tool must keep the sum of the two
    // largest radii between the two segments, so the earlier holes do not meet this one, and their faces are left out of
    // the test of the other faces. A face across the axis cuts the segment; each other face in the slab must keep the
    // distance R from the segment, else no closed form exists. Between two cuts the middle point of the segment is inside
    // the solid of the body or not; the test of inside of item 4 decides it, with a cube that keeps a quarter of the room.
    private static Cut? Remove(Cut minuend, KernelSolid solid, Tool tool)
    {
        var (p, a, radius) = (tool.Base, tool.Axis, tool.Radius);
        var length = a.Dot(a);
        var axis = new NearestPoints.Piece([p, p + a]);
        foreach (var earlier in minuend.Tools)
        {
            var apart = radius + earlier.Radius;
            if (NearestPoints.Between(axis, new NearestPoints.Piece([earlier.Base, earlier.Base + earlier.Axis])).Squared < apart * apart)
                return null;
        }

        var skip = minuend.Tools.Select(earlier => earlier.Feature).ToHashSet();
        Rational Along(Vector3R x) => (x - p).Dot(a);
        var cuts = new List<Rational> { Rational.Zero, Rational.One };
        foreach (var polygon in solid.Polygons)
        {
            if (skip.Contains(polygon.Tag.Feature)
                || CurvedDistance.Clip(CurvedDistance.Clip(polygon.Vertices, Along)?.Vertices ?? [], x => length - Along(x)) is not { } inSlab)
                continue;
            if (polygon.Plane.Normal.Cross(a).IsZero)
            {
                var s = Along(polygon.Vertices[0]) / length;
                if (s.Sign > 0 && s < Rational.One)
                    cuts.Add(s);
            }
            else if (NearestPoints.Between(axis, inSlab).Squared < radius * radius)
            {
                return null;
            }
        }

        cuts = [.. cuts.Distinct().Order()];
        var (integral, moment) = (Rational.Zero, Rational.Zero);
        for (var i = 0; i + 1 < cuts.Count; i++)
        {
            var (low, high) = (cuts[i], cuts[i + 1]);
            var half = (high - low) / 2;
            var room = half * half * length;
            if (radius * radius < room)
                room = radius * radius;
            if (GetDistanceQueryHandler.Inside(solid, p + (a * (low + half)), room / 4))
            {
                integral += Integral(tool.Kind, high) - Integral(tool.Kind, low);
                moment += Moment(tool.Kind, high) - Moment(tool.Kind, low);
            }
        }

        if (integral.IsZero)
            return minuend with { Tools = [.. minuend.Tools, tool] };
        var removed = Math.PI * (radius * radius).ToDouble() * Math.Sqrt(length.ToDouble()) * integral.ToDouble();
        var volume = minuend.Volume - removed;
        var (mx, my, mz) = minuend.Centre.ToDoubles();
        var (rx, ry, rz) = (p + (a * (moment / integral))).ToDoubles();
        var centre = Vector3R.FromDoubles(
            ((mx * minuend.Volume) - (rx * removed)) / volume,
            ((my * minuend.Volume) - (ry * removed)) / volume,
            ((mz * minuend.Volume) - (rz * removed)) / volume);
        return new Cut(volume, centre, [.. minuend.Tools, tool]);
    }

    // The integral of q from 0 to s.
    private static Rational Integral(Kind kind, Rational s) => kind switch
    {
        Kind.Cylinder => s,
        Kind.Cone => s - (s * s) + (s * s * s / 3),
        _ => (2 * s * s) - (4 * s * s * s / 3),
    };

    // The integral of s q from 0 to s.
    private static Rational Moment(Kind kind, Rational s) => kind switch
    {
        Kind.Cylinder => s * s / 2,
        Kind.Cone => (s * s / 2) - (2 * s * s * s / 3) + (s * s * s * s / 4),
        _ => (4 * s * s * s / 3) - (s * s * s * s),
    };
}
