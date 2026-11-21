using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Features;
using Engine.Core.Sketch;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 10, 11 and 13 (phase K15b, TASK-0434): the curve of the design of a body, for the measure queries. The
// kernel holds the facets of a curve and not the curve, so these steps read the values of the commands: the radius and the
// height of a primitive, the arcs and the circles of a sketch, and the map of the Translate and Rotate steps of the walk of
// the solid (KernelProjection.Placed). The results are doubles from + - * / and sqrt, the constant π, and the angle of an
// arc from Angle below. Nothing here enters the log.
internal static class MeasureCurve
{
    // A whole curved face of the design: the side of a cylinder or a cone, or the surface of a sphere, with the curve of
    // KernelProjection.Curves (the centre of the base, the axis to the other end or to the apex, and the radius) and one
    // vertex of its facets, which lies on the curve within the rounding of the turn. Item 17 (phase K15c, TASK-0444): Arc is
    // the part of the turn of the side of an arc, and null for a whole turn. Item 21 (phase K15d, TASK-0450): a part of the
    // side of a hole or of a tube (CutMass.Sides) is the side of a cylinder or of a frustum: Top is the radius at Base + Axis
    // of a frustum, below Radius, and null for a whole cylinder or cone; Inward is true for the side of a hole, whose outward
    // normals point to the axis. OnFacets is then a point of the curve in doubles.
    internal sealed record Surface(SideCurveKind Kind, Vector3R Base, Vector3R Axis, Rational Radius, Vector3R OnFacets, Sector? Arc = null, Rational? Top = null, bool Inward = false);

    // Item 17: the arc of a side, from the direction Start counter-clockwise about Normal to the direction End, each across
    // the axis. Holds is exact: the signs of the cross products about the normal.
    internal sealed record Sector(Vector3R Start, Vector3R End, Vector3R Normal)
    {
        public bool Holds(Vector3R w)
        {
            var (fromStart, toEnd, sweep) = (Start.Cross(w).Dot(Normal), w.Cross(End).Dot(Normal), Start.Cross(End).Dot(Normal));
            if (sweep.IsZero && Start.Dot(End).Sign > 0)
                return true;
            if (sweep.Sign > 0)
                return fromStart.Sign >= 0 && toEnd.Sign >= 0;
            return sweep.IsZero ? fromStart.Sign >= 0 : fromStart.Sign >= 0 || toEnd.Sign >= 0;
        }
    }

    // Item 10: the volume and the centre of the curve of the design, with no flag of facets, for a cylinder, a cone, a
    // sphere, a torus, or an extrude of a sketch with an arc or a circle, each with only Translate and Rotate steps after
    // it. Item 14 (phase K15c, TASK-0444): a body with a Subtract of a curved tool, from CutMass; item 20 (phase K15d,
    // TASK-0450) adds holes on one axis and a tube. Null for each other body:
    // GetMassProperties then gives the exact values of the solid of the kernel.
    public static MassProperties? DesignMass(Document document, Guid body)
    {
        if (KernelProjection.Placed(document, body) is not { } placed)
            return CutMass.Of(document, body);
        var zero = Rational.Zero;
        var origin = new Vector3R(zero, zero, zero);
        (double Volume, Vector3R Centre)? local = placed.Creator switch
        {
            CreateCylinderCommand cylinder => (Math.PI * cylinder.Radius * cylinder.Radius * cylinder.Height, origin),
            // The centre of a cone is a quarter of its height over its base, and the base is at -height / 2.
            CreateConeCommand cone => (Math.PI * cone.Radius * cone.Radius * cone.Height / 3, new Vector3R(zero, zero, -Rational.FromDouble(cone.Height) / 4)),
            CreateSphereCommand sphere => (4 * Math.PI * sphere.Radius * sphere.Radius * sphere.Radius / 3, origin),
            CreateTorusCommand torus => (2 * Math.PI * Math.PI * torus.Radius * torus.TubeRadius * torus.TubeRadius, origin),
            ExtrudeCommand extrude => Extruded(document, extrude),
            _ => null,
        };
        if (local is not { } mass)
            return null;
        var (x, y, z) = placed.Point(mass.Centre).ToDoubles();
        return new MassProperties(mass.Volume, new DatumPoint(x, y, z), false);
    }

    // Item 10: the area of the profile times the height of the extrude along its normal, and the centre of the profile at
    // half the height. The height is the exact product (u × v) · n d of the frame of the kernel, so a frame whose vectors are
    // not exactly of length 1 gives the volume of the solid that the kernel builds on it. A sketch of lines only gives null:
    // its exact solid has no curve.
    private static (double Volume, Vector3R Centre)? Extruded(Document document, ExtrudeCommand extrude)
    {
        if (KernelProjection.ExtrudeDesign(document, extrude) is not var (sketch, origin, u, v, n, distance)
            || (sketch.Arcs.Count == 0 && sketch.Circles.Count == 0) || Profile(sketch) is not var (area, cx, cy))
            return null;
        var height = u.Cross(v).Dot(n) * distance;
        var centre = origin + (u * Rational.FromDouble(cx)) + (v * Rational.FromDouble(cy)) + (n * (distance / 2));
        return (area * Math.Abs(height.ToDouble()), centre);
    }

    // Item 10: the area and the centre of the profile of a sketch with its exact arcs and circles, or null when the sketch
    // has no profile. The loop of SketchProfile gives the order of the edges and the direction of the walk. The polygon of
    // the first point of each edge is exact; each arc adds the signed segment between its chord and its curve: the area
    // r² (θ - sin θ) / 2, and the moment of that area about the centre of the circle, c³ / 12 along the bisector of the arc,
    // with c the length of the chord. The sign is + when the walk follows the arc counter-clockwise, from its start to its
    // end, and - when it walks the arc back.
    internal static (double Area, double X, double Y)? Profile(SketchRecord sketch)
    {
        ArgumentNullException.ThrowIfNull(sketch);
        var (loop, _) = SketchProfile.FindWithItems(sketch);
        if (loop is null)
            return null;
        var position = sketch.Points.ToDictionary(point => point.Id);
        if (sketch.Circles.Count == 1)
        {
            var circle = sketch.Circles[0];
            var c = position[circle.Center];
            return (Math.PI * circle.Radius * circle.Radius, c.X, c.Y);
        }

        var arcs = sketch.Arcs.ToDictionary(arc => arc.Id);
        var count = loop.Count;
        bool First(int i) => loop[i].Item != loop[(i + count - 1) % count].Item;
        var start = Enumerable.Range(0, count).FirstOrDefault(First, -1);
        if (start < 0)
        {
            // One arc alone, from a point back to the same point: a full turn.
            var arc = arcs[loop[0].Item];
            var (c, s) = (position[arc.Center], position[arc.Start]);
            return (Math.PI * (((s.X - c.X) * (s.X - c.X)) + ((s.Y - c.Y) * (s.Y - c.Y))), c.X, c.Y);
        }

        var corners = Enumerable.Range(0, count).Select(k => (start + k) % count).Where(First).Select(i => loop[i]).ToList();
        var (twice, momentX, momentY) = (Rational.Zero, Rational.Zero, Rational.Zero);
        for (var i = 0; i < corners.Count; i++)
        {
            var (p, q) = (corners[i], corners[(i + 1) % corners.Count]);
            var (px, py, qx, qy) = (Rational.FromDouble(p.X), Rational.FromDouble(p.Y), Rational.FromDouble(q.X), Rational.FromDouble(q.Y));
            var cross = (px * qy) - (qx * py);
            twice += cross;
            momentX += (px + qx) * cross;
            momentY += (py + qy) * cross;
        }

        var (area, mx, my) = ((twice / 2).ToDouble(), (momentX / 6).ToDouble(), (momentY / 6).ToDouble());
        for (var i = 0; i < corners.Count; i++)
        {
            if (!arcs.TryGetValue(corners[i].Item, out var arc))
                continue;
            var (c, s, e) = (position[arc.Center], position[arc.Start], position[arc.End]);
            var sign = corners[i].X == s.X && corners[i].Y == s.Y ? 1 : -1;
            var (ax, ay, bx, by) = (s.X - c.X, s.Y - c.Y, e.X - c.X, e.Y - c.Y);
            var squared = (ax * ax) + (ay * ay);
            var scale = Math.Sqrt(squared) * Math.Sqrt((bx * bx) + (by * by));
            var (cos, sin) = (((ax * bx) + (ay * by)) / scale, ((ax * by) - (ay * bx)) / scale);
            // ADR-0040 item 2: an end in the direction of the start is a full turn.
            var sweep = Angle(cos, sin);
            if (sweep == 0)
                sweep = 2 * Math.PI;
            var segment = squared / 2 * (sweep - sin);
            var (dx, dy) = (e.X - s.X, e.Y - s.Y);
            var chord = ((dx * dx) + (dy * dy)) / 12;
            area += sign * segment;
            mx += sign * ((segment * c.X) + (chord * dy));
            my += sign * ((segment * c.Y) - (chord * dx));
        }

        return (area, mx / area, my / area);
    }

    // Items 11 and 12: the whole curved face of the design under a face name of a live body, or the reason that K15b has no
    // curve for it. The face is whole when the body has only Translate and Rotate steps after its command, and the command
    // is a CreateCylinder, a CreateCone, a CreateSphere, or an extrude of a circle. A Subtract can leave a part of the curve,
    // a torus has no curve in KernelProjection.Curves, and the side of an arc is a part of a turn. Item 17 (phase K15c,
    // TASK-0444): with arcs true, the side of an arc of an extrude gives its curve and its sector, as the measure queries
    // read it; GetConnectors calls with arcs false, so it gives no axis for it.
    public static (Surface? Surface, string? Reason) Whole(Document document, Guid body, KernelSolid solid, FaceTag tag, bool arcs = false)
    {
        ArgumentNullException.ThrowIfNull(solid);
        if (KernelProjection.Placed(document, body) is not { } placed)
            return (null, "its body has a step after its first command that is not a Translate or a Rotate, so the face can be a part of the curve");
        if (placed.Creator is CreateTorusCommand)
            return (null, "it is the surface of a torus, which has no curve of the design");
        Sector? sector = null;
        if (placed.Creator is ExtrudeCommand extrude && SketchModel.Find(document, extrude.SketchId)?.Circles.Any(circle => circle.Id == tag.Item) != true)
        {
            if (!arcs || ArcOf(document, extrude, tag.Item, placed) is not { } arc)
                return (null, "it is the side of an arc, a part of a turn");
            sector = arc;
        }

        if (!KernelProjection.Curves(document, body).TryGetValue(tag, out var curve))
            return (null, "it has no curve of the design");
        var onFacets = solid.Polygons.First(polygon => polygon.Tag == tag).Vertices[0];
        return (new Surface(curve.Kind, curve.Base, curve.Axis, curve.Radius, onFacets, sector), null);
    }

    // Item 17: the sector of the arc of the sketch of an extrude, on the exact frame of the kernel, moved and turned with
    // the body. The arc of a sketch turns counter-clockwise in the frame (u, v), so its normal is u × v.
    private static Sector? ArcOf(Document document, ExtrudeCommand extrude, Guid? item, KernelProjection.Placement placed)
    {
        if (KernelProjection.ExtrudeDesign(document, extrude) is not var (sketch, _, u, v, _, _)
            || sketch.Arcs.FirstOrDefault(arc => arc.Id == item) is not { } arc)
            return null;
        var position = sketch.Points.ToDictionary(point => point.Id);
        if (!position.TryGetValue(arc.Center, out var c) || !position.TryGetValue(arc.Start, out var s) || !position.TryGetValue(arc.End, out var e))
            return null;
        Vector3R Direction(SketchPoint p)
            => placed.Direction((u * (Rational.FromDouble(p.X) - Rational.FromDouble(c.X))) + (v * (Rational.FromDouble(p.Y) - Rational.FromDouble(c.Y))));
        return new Sector(Direction(s), Direction(e), placed.Direction(u).Cross(placed.Direction(v)));
    }

    // Item 13: the angle in [0, 2π] of the direction (x, y), not both zero, counter-clockwise from the axis x. It uses + - *
    // / and sqrt only, so it gives the same bits on each platform, and the determinism gate scans Engine.Core for
    // Math.Atan2 (CLAUDE.md, determinism rule 1). TASK-0438 (finding E232): a direction just under the axis x, as (1, -1e-17),
    // gives exactly 2π, because 2π less a very small angle rounds up to 2π. The caller takes 2π as a full turn.
    internal static double Angle(double x, double y)
    {
        if (y == 0)
            return x > 0 ? 0 : Math.PI;
        if (x == 0)
            return y > 0 ? Math.PI / 2 : 3 * Math.PI / 2;
        var a = Atan(Math.Abs(y) / Math.Abs(x));
        return x > 0 ? (y > 0 ? a : (2 * Math.PI) - a) : (y > 0 ? Math.PI - a : Math.PI + a);
    }

    // The angle in (0, π/2) whose tangent is t > 0. Above 1 it is π/2 less the angle of 1 / t. Each step tan(φ / 2) =
    // tan φ / (1 + √(1 + tan² φ)) halves the angle, until the tangent is below 1e-3; there the series t - t³/3 + t⁵/5 - t⁷/7
    // has an error below 1e-24 of the angle. The halvings then double the angle back exactly. TASK-0438 (finding E232): each
    // halving rounds, so the result is at most about ten units of the last place from the true angle (8.8 in the probe of
    // the review against a reference of 240 bits); MeasureQueryTests holds ten units against Math.Atan.
    private static double Atan(double t)
    {
        if (t > 1)
            return (Math.PI / 2) - Atan(1 / t);
        var halvings = 0;
        while (t > 1e-3)
        {
            t /= 1 + Math.Sqrt(1 + (t * t));
            halvings++;
        }

        var t2 = t * t;
        return t * (1 - (t2 * ((1.0 / 3) - (t2 * ((1.0 / 5) - (t2 / 7)))))) * (1 << halvings);
    }
}
