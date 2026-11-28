using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 item 14 (phase K15c, TASK-0444): the volume and the centre of the curve of a body with a Subtract whose tool is
// one curved command: a cylinder, a cone, a sphere or an extrude of one circle, with only Translate and Rotate steps after
// it. The tool is a solid of revolution about the segment from P to P + A, with the square of its radius at the part s of
// the segment. Its curve meets the body before the Subtract only at planes across the axis when each other face of that
// body keeps the largest radius from the segment in the slab of the tool. Then the body holds a part of the tool between
// two of those planes either whole or not at all, and the removed volume is π |A| times the integral of the square of the
// radius over the parts that it holds. The decisions are exact; the volume and the centre leave as doubles, with π and one
// root of A · A.
//
// Item 20 (phase K15d, TASK-0450): the tools of a cylinder, a cone or an extrude of a circle on one line make one group, as
// a hole and its counterbore or its countersink. The radius of the group at s is the largest radius of its tools there,
// a linear function on each part between the cuts, the ends of the tools and the points where two radii are equal. A first
// body of one curved command (a cylinder, a cone or an extrude of one circle) with tools on its axis only, as a tube, gives
// the integral of the square of the lower of its radius and the radius of the group, with no test of inside. Item 21: the
// parts of the line where a curved face is the face of the body give the side of a hole, and the side of a tube.
internal static class CutMass
{
    // The profile of a tool: a radius that is linear along its segment for a cylinder and a cone, and the square of its
    // radius R² 4 s (1 - s) for a sphere on the diameter from P to P + A.
    private enum Kind
    {
        Cylinder,
        Cone,
        Sphere,
    }

    // A tool: its segment, its largest radius, its profile, the feature of its faces and the tag of its curved face.
    private sealed record Tool(Vector3R Base, Vector3R Axis, Rational Radius, Kind Kind, Guid Feature, FaceTag Side);

    // Item 20: a radius α + β s on the part [Low, High] of the line of a group.
    private readonly record struct Linear(Rational Low, Rational High, Rational Alpha, Rational Beta)
    {
        public Rational At(Rational s) => Alpha + (Beta * s);
    }

    // Item 21: a part [Low, High] of the line of a group where the curved face Side is a face of the body, with its radius.
    // Inward is true for the side of a hole, whose outward normals point to the axis.
    private sealed record Band(FaceTag Side, Rational Low, Rational High, Linear Radius, bool Inward);

    // A group of tools on one line, from Base along Axis: the solid of the body before its first tool, or null when no
    // later tool can join it (after a Translate or a Rotate, or for a sphere), the removed volume and its centre, the parts
    // where a face is exposed, and the faces whose radius is equal to that of another tool on a part.
    private sealed record Group(
        Vector3R Base, Vector3R Axis, IReadOnlyList<Tool> Tools, KernelSolid? Solid, double Volume, Vector3R Centre,
        IReadOnlyList<Band> Bands, IReadOnlyList<FaceTag> Ties);

    // The volume and the centre of the first body, the groups of the Subtract steps, in the place of the step of the walk,
    // and, for a first body of one curved command, its radius along the line of its group and the tag of its side.
    private sealed record Cut(double Volume, Vector3R Centre, IReadOnlyList<Group> Groups, Linear? First = null, FaceTag? FirstSide = null);

    // The mass of the curve of the body, or null when its chain has no Subtract of a curved tool or has no closed form.
    public static MassProperties? Of(Document document, Guid body)
    {
        ArgumentNullException.ThrowIfNull(document);
        var graph = FeatureGraph.Of(document);
        if (!HasTool(document, graph, body) || Walk(document, graph, body) is not { Groups.Count: > 0 } cut)
            return null;
        var removed = cut.Groups.Sum(group => group.Volume);
        var volume = cut.Volume - removed;
        var (mx, my, mz) = cut.Centre.ToDoubles();
        var (x, y, z) = ((mx * cut.Volume) / volume, (my * cut.Volume) / volume, (mz * cut.Volume) / volume);
        foreach (var group in cut.Groups.Where(group => group.Volume > 0))
        {
            var (gx, gy, gz) = group.Centre.ToDoubles();
            (x, y, z) = (x - (gx * group.Volume / volume), y - (gy * group.Volume / volume), z - (gz * group.Volume / volume));
        }

        return new MassProperties(volume, new DatumPoint(x, y, z), false);
    }

    // Item 21 (phase K15d, TASK-0450): the parts of a curved face of a body with a Subtract, each a side of a cylinder or
    // of a frustum, or the reason that no closed form exists. A part goes from its larger radius to its smaller one.
    public static (IReadOnlyList<MeasureCurve.Surface>? Surfaces, string? Reason) Sides(Document document, Guid body, FaceTag tag)
    {
        ArgumentNullException.ThrowIfNull(document);
        var graph = FeatureGraph.Of(document);
        if (!HasTool(document, graph, body) || Walk(document, graph, body) is not { } cut)
            return (null, "its body has no closed form for the parts of its curved faces: it is not a first body of plane faces or of one curved command with Subtract steps of a cylinder, a cone or an extrude of one circle whose faces meet the other faces at planes across their axes (ADR-0180 items 14 and 20)");
        if (cut.Groups.Any(group => group.Ties.Contains(tag)))
            return (null, "two tools on one axis have the same radius on a part of the face, so the part has no one face");
        var surfaces = new List<MeasureCurve.Surface>();
        foreach (var group in cut.Groups)
        {
            var (p, a) = (group.Base, group.Axis);
            foreach (var band in Merged(group.Bands.Where(band => band.Side == tag)))
            {
                var (rLow, rHigh) = (band.Radius.At(band.Low), band.Radius.At(band.High));
                var (from, to, r0, r1) = rLow >= rHigh ? (band.Low, band.High, rLow, rHigh) : (band.High, band.Low, rHigh, rLow);
                var (start, axis) = (p + (a * from), a * (to - from));
                var middle = (band.Low + band.High) / 2;
                var (ex, ey, ez) = CurvedDistance.Across(a);
                var (cx, cy, cz) = (p + (a * middle)).ToDoubles();
                var rm = band.Radius.At(middle).ToDouble();
                var onCurve = Vector3R.FromDoubles(cx + (ex * rm), cy + (ey * rm), cz + (ez * rm));
                surfaces.Add(r0 == r1
                    ? new MeasureCurve.Surface(Drawing.SideCurveKind.Cylinder, start, axis, r0, onCurve, Inward: band.Inward)
                    : new MeasureCurve.Surface(Drawing.SideCurveKind.Cone, start, axis, r0, onCurve, Top: r1, Inward: band.Inward));
            }
        }

        return surfaces.Count == 0
            ? (null, "it is a part of a curved face with no closed form: the zone of a sphere, or a face that the closed form of the volume does not give")
            : (surfaces, null);
    }

    // The bands of one face in the order of the line, with two neighbours of one radius joined.
    private static IEnumerable<Band> Merged(IEnumerable<Band> bands)
    {
        Band? open = null;
        foreach (var band in bands.OrderBy(band => band.Low))
        {
            if (open is { } last && last.High == band.Low && last.Radius == band.Radius)
            {
                open = last with { High = band.High };
                continue;
            }

            if (open is not null)
                yield return open;
            open = band;
        }

        if (open is not null)
            yield return open;
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

    // ADR-0191 item 5 (phase P11.0, TASK-0474): the cut of the body in the Document. Each step of the walk reads its operands in
    // the Document, with their placements, so the result of a step is at the placement that its command gives it: the identity
    // for a Translate and a Rotate, the placement of the minuend for a Subtract. A PlaceBody of the result moves the cut from that
    // placement to its own. The first body of the chain reads the solid, the map and the curves with its placement.
    // Finding E246 of the codebase review of 2026-11-28 (TASK-0476): a Subtract builds its cut from the step only when its
    // subtrahend is a curved tool, as the case of Steps says; a Subtract by another body is a first body of the chain, whose solid
    // has its placement now. Until TASK-0476 such a cut moved a second time.
    private static Cut? Walk(Document document, FeatureGraph graph, Guid handle)
    {
        if (Steps(document, graph, handle) is not { } cut)
            return null;
        var given = graph.Creator(handle) switch
        {
            TranslateCommand or RotateCommand => BodyPlacement.Identity,
            SubtractCommand subtract when ToolOf(document, subtract.SubtrahendBodyId) is not null
                => graph.Placement(subtract.MinuendBodyId, graph.Order(subtract.CommandId)),
            _ => (BodyPlacement?)null,
        };
        var now = graph.Placement(handle);
        if (given is not { } from || from == now)
            return cut;
        var change = Placements.Compose(now, Placements.Inverse(from));
        return Mapped(cut, point => Placements.Point(change, point), direction => Placements.Direction(change, direction));
    }

    // The cut with each point and each direction of its groups and tools mapped. A moved group takes no later tool.
    private static Cut Mapped(Cut cut, Func<Vector3R, Vector3R> point, Func<Vector3R, Vector3R> direction) => cut with
    {
        Centre = point(cut.Centre),
        Groups = [.. cut.Groups.Select(group => group with
        {
            Base = point(group.Base), Axis = direction(group.Axis), Centre = point(group.Centre), Solid = null,
            Tools = [.. group.Tools.Select(tool => tool with { Base = point(tool.Base), Axis = direction(tool.Axis) })],
        })],
    };

    private static Cut? Steps(Document document, FeatureGraph graph, Guid handle)
    {
        switch (graph.Creator(handle))
        {
            case TranslateCommand translate:
            {
                if (Walk(document, graph, translate.BodyId) is not { } moved)
                    return null;
                var offset = Vector3R.FromDoubles(translate.Dx, translate.Dy, translate.Dz);
                return moved with
                {
                    Centre = moved.Centre + offset,
                    Groups = [.. moved.Groups.Select(group => group with
                    {
                        Base = group.Base + offset, Centre = group.Centre + offset, Solid = null,
                        Tools = [.. group.Tools.Select(tool => tool with { Base = tool.Base + offset })],
                    })],
                };
            }

            case RotateCommand rotate:
            {
                if (RotateCommandHandler.Turn(rotate) is not { } r || Walk(document, graph, rotate.BodyId) is not { } turned)
                    return null;
                var (x, y, z) = (Vector3R.FromDoubles(r.Xx, r.Xy, r.Xz), Vector3R.FromDoubles(r.Yx, r.Yy, r.Yz), Vector3R.FromDoubles(r.Zx, r.Zy, r.Zz));
                Vector3R Map(Vector3R q) => (x * q.X) + (y * q.Y) + (z * q.Z);
                return turned with
                {
                    Centre = Map(turned.Centre),
                    Groups = [.. turned.Groups.Select(group => group with
                    {
                        Base = Map(group.Base), Axis = Map(group.Axis), Centre = Map(group.Centre), Solid = null,
                        Tools = [.. group.Tools.Select(tool => tool with { Base = Map(tool.Base), Axis = Map(tool.Axis) })],
                    })],
                };
            }

            case SubtractCommand subtract when ToolOf(document, subtract.SubtrahendBodyId) is { } tool:
            {
                if (Walk(document, graph, subtract.MinuendBodyId) is not { } minuend)
                    return null;
                return Remove(minuend, tool, () => KernelProjection.Of(document, subtract.MinuendBodyId).Solid);
            }

            case null:
                return null;

            default:
            {
                // The first body of the chain: a body of plane faces, with the exact values of its solid, or (item 20) one
                // curved command whose curve gives its volume, its centre and its radius along its axis.
                if (KernelProjection.Of(document, handle).Solid is not { } solid)
                    return null;
                if (!MeasureTarget.HasCurve(solid))
                {
                    var mass = GetMassPropertiesQueryHandler.Of(solid);
                    return new Cut(mass.Volume, Vector3R.FromDoubles(mass.Centre.X, mass.Centre.Y, mass.Centre.Z), []);
                }

                if (ToolOf(document, handle) is not { Kind: not Kind.Sphere } first || MeasureCurve.DesignMass(document, handle) is not { } design)
                    return null;
                var line = new Group(first.Base, first.Axis, [], null, 0, first.Base, [], []);
                var radius = new Linear(Rational.Zero, Rational.One, first.Radius, first.Kind == Kind.Cone ? -first.Radius : Rational.Zero);
                return new Cut(design.Volume, Vector3R.FromDoubles(design.Centre.X, design.Centre.Y, design.Centre.Z), [line], radius, first.Side);
            }
        }
    }

    // The curve of a tool body, or null when it is not one curved command with only Translate and Rotate steps after it.
    private static Tool? ToolOf(Document document, Guid handle)
    {
        if (KernelProjection.Placed(document, handle) is not { } placed)
            return null;
        var curves = KernelProjection.Curves(document, handle);
        var (tag, curve) = curves.Count == 1 ? curves.First() : default;
        switch (placed.Creator)
        {
            case CreateCylinderCommand cylinder when curve is not null:
                return new Tool(curve.Base, curve.Axis, curve.Radius, Kind.Cylinder, cylinder.CommandId, tag);
            case CreateConeCommand cone when curve is not null:
                return new Tool(curve.Base, curve.Axis, curve.Radius, Kind.Cone, cone.CommandId, tag);
            case CreateSphereCommand sphere:
            {
                var r = Rational.FromDouble(sphere.Radius);
                var zero = Rational.Zero;
                return new Tool(placed.Point(new Vector3R(zero, zero, -r)), placed.Direction(new Vector3R(zero, zero, r * 2)), r, Kind.Sphere, sphere.CommandId, new FaceTag(sphere.CommandId, "surface"));
            }

            case ExtrudeCommand extrude when KernelProjection.ExtrudeDesign(document, extrude) is var (sketch, _, _, _, _, _)
                && sketch.Circles.Count == 1 && sketch.Arcs.Count == 0 && sketch.Lines.Count == 0 && curve is not null:
                return new Tool(curve.Base, curve.Axis, curve.Radius, Kind.Cylinder, extrude.CommandId, tag);
            default:
                return null;
        }
    }

    // The body before the Subtract less the part of the tool that it holds. A tool of a cylinder or a cone joins the open
    // group on its line (item 20). Each tool of another group must keep the sum of the two largest radii between the two
    // segments, so the holes do not meet. With a first body of one curved command, each tool must be on its axis.
    private static Cut? Remove(Cut minuend, Tool tool, Func<KernelSolid?> solidOf)
    {
        var index = -1;
        for (var i = 0; i < minuend.Groups.Count; i++)
        {
            var group = minuend.Groups[i];
            if (tool.Kind != Kind.Sphere && group.Tools.All(other => other.Kind != Kind.Sphere) && (minuend.First is not null || group.Solid is not null)
                && group.Axis.Cross(tool.Axis).IsZero && (tool.Base - group.Base).Cross(group.Axis).IsZero)
            {
                index = i;
                break;
            }
        }

        if (minuend.First is { } first)
        {
            if (index < 0)
                return null;
            var line = minuend.Groups[index];
            return Joined(minuend, index, Integrate(line.Base, line.Axis, [.. line.Tools, tool], null, (first, minuend.FirstSide!), new HashSet<Guid>()));
        }

        var axis = new NearestPoints.Piece([tool.Base, tool.Base + tool.Axis]);
        for (var i = 0; i < minuend.Groups.Count; i++)
        {
            if (i == index)
                continue;
            foreach (var earlier in minuend.Groups[i].Tools)
            {
                var apart = tool.Radius + earlier.Radius;
                if (NearestPoints.Between(axis, new NearestPoints.Piece([earlier.Base, earlier.Base + earlier.Axis])).Squared < apart * apart)
                    return null;
            }
        }

        var skip = minuend.Groups.SelectMany(group => group.Tools).Select(earlier => earlier.Feature).ToHashSet();
        if (index >= 0)
        {
            var group = minuend.Groups[index];
            return Joined(minuend, index, Integrate(group.Base, group.Axis, [.. group.Tools, tool], group.Solid, null, skip));
        }

        if (solidOf() is not { } solid)
            return null;
        var added = Integrate(tool.Base, tool.Axis, [tool], solid, null, skip);
        return added is null ? null : minuend with { Groups = [.. minuend.Groups, tool.Kind == Kind.Sphere ? added with { Solid = null } : added] };
    }

    private static Cut? Joined(Cut cut, int index, Group? group)
        => group is null ? null : cut with { Groups = [.. cut.Groups.Select((old, i) => i == index ? group : old)] };

    // The removed volume of the tools of one group on the line from p along a. With a solid (a first body of plane faces),
    // a face across the axis cuts the line; each other face in the slab of the tools must keep the largest radius from the
    // segment, else no closed form exists, and between two cuts the middle point of the line is inside the solid or not: the
    // test of inside of item 4 decides it, with a cube that keeps a quarter of the room. With a first body of one curved
    // command on the line (a tube), the part of its span is held, and the radius is the lower of the two.
    private static Group? Integrate(
        Vector3R p, Vector3R a, IReadOnlyList<Tool> tools, KernelSolid? solid, (Linear Radius, FaceTag Side)? first, IReadOnlySet<Guid> skip)
    {
        var length = a.Dot(a);
        Rational Along(Vector3R x) => (x - p).Dot(a);
        var radii = tools.Select(tool => RadiusOf(p, a, length, tool)).ToList();
        var largest = tools.Max(tool => tool.Radius);
        var (low, high) = first is not null ? (Rational.Zero, Rational.One) : (radii.Min(r => r.Low), radii.Max(r => r.High));

        var cuts = new List<Rational> { low, high };
        List<Linear> lines = first is { } outer ? [.. radii, outer.Radius] : radii;
        foreach (var r in lines)
        {
            cuts.Add(r.Low);
            cuts.Add(r.High);
            foreach (var other in lines)
            {
                if (r.Beta != other.Beta)
                    cuts.Add((other.Alpha - r.Alpha) / (r.Beta - other.Beta));
            }
        }

        if (solid is not null)
        {
            var segment = new NearestPoints.Piece([p + (a * low), p + (a * high)]);
            foreach (var polygon in solid.Polygons)
            {
                if (skip.Contains(polygon.Tag.Feature)
                    || CurvedDistance.Clip(CurvedDistance.Clip(polygon.Vertices, x => Along(x) - (low * length))?.Vertices ?? [], x => (high * length) - Along(x)) is not { } inSlab)
                    continue;
                if (polygon.Plane.Normal.Cross(a).IsZero)
                    cuts.Add(Along(polygon.Vertices[0]) / length);
                else if (NearestPoints.Between(segment, inSlab).Squared < largest * largest)
                    return null;
            }
        }

        cuts = [.. cuts.Where(s => s >= low && s <= high).Distinct().Order()];
        var (integral, moment) = (Rational.Zero, Rational.Zero);
        var (bands, ties) = (new List<Band>(), new List<FaceTag>());
        for (var i = 0; i + 1 < cuts.Count; i++)
        {
            var (from, to) = (cuts[i], cuts[i + 1]);
            var middle = (from + to) / 2;
            var covering = Enumerable.Range(0, tools.Count).Where(k => radii[k].Low <= from && to <= radii[k].High).ToList();
            if (first is { } body)
            {
                var widest = covering.Count == 0 ? -1 : covering.MaxBy(k => radii[k].At(middle));
                var bore = widest < 0 ? Rational.Zero : radii[widest].At(middle);
                if (bore < body.Radius.At(middle))
                {
                    bands.Add(new Band(body.Side, from, to, body.Radius, false));
                    if (widest >= 0)
                        Expose(widest);
                }

                if (widest >= 0)
                    Add(bore <= body.Radius.At(middle) ? radii[widest] : body.Radius, Kind.Cylinder);
                continue;
            }

            if (covering.Count == 0)
                continue;
            var half = (to - from) / 2;
            var room = half * half * length;
            if (largest * largest < room)
                room = largest * largest;
            if (!GetDistanceQueryHandler.Inside(solid!, p + (a * middle), room / 4))
                continue;
            var top = covering.MaxBy(k => radii[k].At(middle));
            Expose(top);
            Add(radii[top], tools[top].Kind);

            void Add(Linear r, Kind kind)
            {
                integral += Square(kind, r, largest, to) - Square(kind, r, largest, from);
                moment += Moment(kind, r, largest, to) - Moment(kind, r, largest, from);
            }

            // The zone of a sphere is not a side of revolution of a linear radius, so it gives no band.
            void Expose(int k)
            {
                if (tools[k].Kind == Kind.Sphere)
                    return;
                var r = radii[k].At(middle);
                if (covering.Any(other => other != k && radii[other].At(middle) == r))
                    ties.AddRange(covering.Where(other => radii[other].At(middle) == r).Select(other => tools[other].Side));
                else
                    bands.Add(new Band(tools[k].Side, from, to, radii[k], true));
            }
        }

        var removed = integral.IsZero ? 0 : Math.PI * Math.Sqrt(length.ToDouble()) * integral.ToDouble();
        var centre = integral.IsZero ? p : p + (a * (moment / integral));
        return new Group(p, a, tools, solid, removed, centre, bands, ties);
    }

    // The radius of a tool along the line from p along a: a constant for a cylinder, and for a cone the radius R at the
    // base, 0 at the apex. A sphere keeps its own profile, with its span only.
    private static Linear RadiusOf(Vector3R p, Vector3R a, Rational length, Tool tool)
    {
        var (start, end) = ((tool.Base - p).Dot(a) / length, (tool.Base + tool.Axis - p).Dot(a) / length);
        var (low, high) = start <= end ? (start, end) : (end, start);
        return tool.Kind switch
        {
            Kind.Cone => new Linear(low, high, tool.Radius * end / (end - start), -tool.Radius / (end - start)),
            _ => new Linear(low, high, tool.Radius, Rational.Zero),
        };
    }

    // The integral of the square of the radius from 0 to s: (α + β s)² for a linear radius, and R² 4 s (1 - s) for a sphere
    // on the line of its own segment.
    private static Rational Square(Kind kind, Linear r, Rational largest, Rational s) => kind switch
    {
        Kind.Sphere => largest * largest * ((2 * s * s) - (4 * s * s * s / 3)),
        _ => (r.Alpha * r.Alpha * s) + (r.Alpha * r.Beta * s * s) + (r.Beta * r.Beta * s * s * s / 3),
    };

    // The integral of s times the square of the radius from 0 to s.
    private static Rational Moment(Kind kind, Linear r, Rational largest, Rational s) => kind switch
    {
        Kind.Sphere => largest * largest * ((4 * s * s * s / 3) - (s * s * s * s)),
        _ => (r.Alpha * r.Alpha * s * s / 2) + (2 * r.Alpha * r.Beta * s * s * s / 3) + (r.Beta * r.Beta * s * s * s * s / 4),
    };
}
