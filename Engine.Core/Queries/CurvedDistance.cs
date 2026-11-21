using Engine.Contracts;
using Engine.Contracts.Sketch;
using Engine.Core.Drawing;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 item 11 (phase K15b, TASK-0434): the minimum distance when a side of GetDistance is a whole curved face of the
// design (MeasureCurve.Whole). Each step that decides a case uses + - * / and comparisons on exact rationals, from the
// exact nearest points of K15a; the distance and the points then leave as doubles from + - * / and sqrt. A case with no
// closed form here gives the reason, and the caller refuses it with E-MEASURE-CURVED.
internal static class CurvedDistance
{
    // One pair of points at the distance, on the curve and on the other side.
    private sealed record Candidate(double Distance, DatumPoint OnCurve, DatumPoint OnOther);

    public static (DistanceInfo? Info, ErrorDetail? Error) Between(MeasureTarget.Resolved a, MeasureTarget.Resolved b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Curve is null && b.Curve is null)
            return (GetDistanceQueryHandler.Between(a, b), null);
        if (a.Curve is { } first && b.Curve is { } second)
        {
            return first.Kind == SideCurveKind.Sphere && second.Kind == SideCurveKind.Sphere
                ? (Spheres(first, second), null)
                : (null, MeasureTarget.Curved("The two sides are curved faces, and only two spheres have a closed form."));
        }

        var swap = a.Curve is null;
        var (curve, other) = swap ? (b.Curve!, a) : (a.Curve!, b);
        if (curve.Kind == SideCurveKind.Cone)
            return (null, MeasureTarget.Curved("The side of a cone has no closed form for a distance in K15b."));
        var (best, bound) = curve.Kind == SideCurveKind.Sphere ? (Sphere(curve, other), null) : Cylinder(curve, other);
        var onFace = curve.Kind == SideCurveKind.Sphere ? curve.Base + new Vector3R(curve.Radius, Rational.Zero, Rational.Zero) : curve.OnFacets;

        // K15a item 4: a body is a solid. When the curved face keeps a distance above 0 from each piece of a body, the face
        // is all inside the solid or all outside it, so one point decides: the point of the sphere at +x, which is exact,
        // or a vertex of the facets of the side, which lies on the curve within the rounding of the turn. The lowest bound
        // of the pieces gives the room: the cube of the test has a half side below a quarter of it, so it does not reach
        // the surface of the solid.
        var lowest = bound is { } low && (best is null || low < best.Distance) ? low : best?.Distance;
        if (lowest > 0 && other.Solid is { } solid)
        {
            var half = Rational.FromDouble(lowest.Value / 2);
            if (half.Sign > 0 && GetDistanceQueryHandler.Inside(solid, onFace, half * half))
                return (new DistanceInfo(0, Point(onFace), Point(onFace)), null);
        }

        // A part past an end of a cylinder whose bound is below the best distance can be nearer.
        if (best is null || bound < best.Distance)
            return (null, MeasureTarget.Curved("The nearest point of the side of the cylinder can be past an end of the side, where K15b has no closed form."));
        var info = new DistanceInfo(best.Distance, best.OnCurve, best.OnOther);
        return (swap ? new DistanceInfo(info.Distance, info.PointB, info.PointA) : info, null);
    }

    // The surface of a sphere against each piece P of the other side. With the exact nearest point q of P to the centre and
    // the farthest vertex v of P: P is outside the ball when |q - c| ≥ r, and its distance is |q - c| - r; P is inside the
    // ball when |v - c| ≤ r, and its distance is r - |v - c|, because the farthest point of a convex piece is a vertex; else
    // P crosses the surface, at the point of the segment from q to v at the distance r.
    private static Candidate Sphere(MeasureCurve.Surface sphere, MeasureTarget.Resolved other)
    {
        var (c, r) = (sphere.Base, sphere.Radius);
        var squared = r * r;
        var centre = new NearestPoints.Piece([c, c]);
        Candidate? best = null;
        foreach (var piece in other.Pieces)
        {
            var near = NearestPoints.Between(centre, piece);
            Candidate found;
            if (near.Squared >= squared)
            {
                found = new(Math.Sqrt(near.Squared.ToDouble()) - r.ToDouble(), Toward(c, near.B, r, near.Squared), Point(near.B));
            }
            else
            {
                var (far, vertex) = Farthest(piece, v => (v - c).Dot(v - c));
                found = far <= squared
                    ? new(r.ToDouble() - Math.Sqrt(far.ToDouble()), Toward(c, vertex, r, far), Point(vertex))
                    : Crossing(near.B, vertex, q => (q - c).Dot(q - c) - squared);
            }

            if (best is null || found.Distance < best.Distance)
                best = found;
        }

        return best!;
    }

    // The side of a cylinder from the base p to p + a, of radius r, against each piece P of the other side. For a point in
    // the slab 0 ≤ (x - p) · a ≤ a · a the nearest point of the side is along the radius, at the distance |ρ - r|, with ρ
    // the distance to the axis. So the part of P in the slab gives a closed form as for a sphere: the nearest point to the
    // axis, the farthest vertex from it, or a crossing. A part of P past an end has no closed form here; its distance to
    // the axis less r is a lower bound of its distance to the side. When that bound is below the best distance, the nearest
    // point can be past an end, and the case is refused.
    private static (Candidate? Best, double? Bound) Cylinder(MeasureCurve.Surface cylinder, MeasureTarget.Resolved other)
    {
        var (p, a, r) = (cylinder.Base, cylinder.Axis, cylinder.Radius);
        var (length, squared) = (a.Dot(a), r * r);
        var axis = new NearestPoints.Piece([p, p + a]);
        Rational Along(Vector3R x) => (x - p).Dot(a);
        Rational Radial(Vector3R x) => (x - p).Dot(x - p) - (Along(x) * Along(x) / length);
        Vector3R Foot(Vector3R x) => p + (a * (Along(x) / length));

        Candidate? best = null;
        double? bound = null;
        foreach (var piece in other.Pieces)
        {
            if (Clip(Clip(piece.Vertices, Along), x => length - Along(x)) is { } inSlab)
            {
                var near = NearestPoints.Between(axis, inSlab);
                Candidate found;
                if (near.Squared >= squared)
                {
                    found = new(Math.Sqrt(near.Squared.ToDouble()) - r.ToDouble(), Toward(near.A, near.B, r, near.Squared), Point(near.B));
                }
                else
                {
                    var (far, vertex) = Farthest(inSlab, Radial);
                    found = far <= squared
                        ? new(r.ToDouble() - Math.Sqrt(far.ToDouble()), Toward(Foot(vertex), vertex, r, far, Across(a)), Point(vertex))
                        : Crossing(near.B, vertex, x => Radial(x) - squared);
                }

                if (best is null || found.Distance < best.Distance)
                    best = found;
            }

            foreach (var past in new[] { Clip(piece.Vertices, x => -Along(x)), Clip(piece.Vertices, x => Along(x) - length) })
            {
                if (past is null)
                    continue;
                var lower = Math.Max(0, Math.Sqrt(NearestPoints.Between(axis, past).Squared.ToDouble()) - r.ToDouble());
                bound = bound is { } known ? Math.Min(known, lower) : lower;
            }
        }

        return (best, bound);
    }

    // Two spheres with the centres c and d and the radii r and s, at the exact distance D of the centres: apart (D - r - s),
    // one inside the other (|r - s| - D), or crossing (0, at a point of the circle where they meet).
    private static DistanceInfo Spheres(MeasureCurve.Surface first, MeasureCurve.Surface second)
    {
        var (c, r, d, s) = (first.Base, first.Radius, second.Base, second.Radius);
        var between = d - c;
        var squared = between.Dot(between);
        var (rd, sd) = (r.ToDouble(), s.ToDouble());
        var distance = Math.Sqrt(squared.ToDouble());
        var (x, y, z) = between.ToDoubles();
        var (ux, uy, uz) = squared.IsZero ? (1.0, 0.0, 0.0) : (x / distance, y / distance, z / distance);
        var (cx, cy, cz) = c.ToDoubles();
        var (dx, dy, dz) = d.ToDoubles();
        if (squared >= (r + s) * (r + s))
            return new(distance - rd - sd, new(cx + (x * rd / distance), cy + (y * rd / distance), cz + (z * rd / distance)), new(dx - (x * sd / distance), dy - (y * sd / distance), dz - (z * sd / distance)));
        if (squared <= (r - s) * (r - s))
        {
            // The farthest point of the inner sphere from the centre of the outer one, and the point of the outer sphere
            // beyond it.
            var sign = r >= s ? 1 : -1;
            return new(Math.Abs(rd - sd) - distance, new(cx + (sign * ux * rd), cy + (sign * uy * rd), cz + (sign * uz * rd)), new(dx + (sign * ux * sd), dy + (sign * uy * sd), dz + (sign * uz * sd)));
        }

        // The circle where they meet is at along = (D² + r² - s²) / 2D from c, with the radius √(r² - along²).
        var along = ((squared + (r * r) - (s * s)) / 2).ToDouble() / distance;
        var radius = Math.Sqrt(Math.Max(0, (rd * rd) - (along * along)));
        var (ex, ey, ez) = Across(between);
        var point = new DatumPoint(cx + (ux * along) + (ex * radius), cy + (uy * along) + (ey * radius), cz + (uz * along) + (ez * radius));
        return new(0, point, point);
    }

    // The vertex of a piece with the largest value of the exact measure, the first one when several have it.
    private static (Rational Value, Vector3R Vertex) Farthest(NearestPoints.Piece piece, Func<Vector3R, Rational> measure)
    {
        var (value, vertex) = (measure(piece.Vertices[0]), piece.Vertices[0]);
        foreach (var v in piece.Vertices.Skip(1))
        {
            var m = measure(v);
            if (m > value)
                (value, vertex) = (m, v);
        }

        return (value, vertex);
    }

    // The point of the segment from q to v where an exact quadratic measure f, below 0 at q and not below 0 at v, is 0. With
    // f(q + λ (v - q)) = A λ² + B λ + C, A is not negative and C is below 0, so the root in (0, 1] is the larger one.
    private static Candidate Crossing(Vector3R q, Vector3R v, Func<Vector3R, Rational> f)
    {
        var d = v - q;
        var (low, high, middle) = (f(q), f(v), f(q + (d * new Rational(1, 2))));
        // From three values: C = f(0), A + B + C = f(1), A / 4 + B / 2 + C = f(1/2).
        var aa = (2 * (high + low)) - (4 * middle);
        var bb = high - low - aa;
        var lambda = aa.IsZero
            ? (-low / bb).ToDouble()
            : (-bb.ToDouble() + Math.Sqrt(((bb * bb) - (4 * aa * low)).ToDouble())) / (2 * aa.ToDouble());
        var (qx, qy, qz) = q.ToDoubles();
        var (dx, dy, dz) = d.ToDoubles();
        var point = new DatumPoint(qx + (lambda * dx), qy + (lambda * dy), qz + (lambda * dz));
        return new Candidate(0, point, point);
    }

    // The point at the distance r from the point from, toward the point to, at the exact squared distance between them. When
    // the two points are one, the direction is across, a unit vector in doubles.
    private static DatumPoint Toward(Vector3R from, Vector3R to, Rational r, Rational squared, (double X, double Y, double Z)? across = null)
    {
        var (fx, fy, fz) = from.ToDoubles();
        var rd = r.ToDouble();
        if (squared.IsZero)
        {
            var (ex, ey, ez) = across ?? (1.0, 0.0, 0.0);
            return new DatumPoint(fx + (ex * rd), fy + (ey * rd), fz + (ez * rd));
        }

        var (dx, dy, dz) = (to - from).ToDoubles();
        var length = Math.Sqrt(squared.ToDouble());
        return new DatumPoint(fx + (dx * rd / length), fy + (dy * rd / length), fz + (dz * rd / length));
    }

    // A unit vector at a right angle to a, in doubles: a × e for the axis e along which a is shortest.
    private static (double X, double Y, double Z) Across(Vector3R a)
    {
        var (x, y, z) = a.ToDoubles();
        var (ax, ay, az) = (Math.Abs(x), Math.Abs(y), Math.Abs(z));
        var (cx, cy, cz) = ax <= ay && ax <= az ? (0.0, z, -y) : ay <= az ? (-z, 0.0, x) : (y, -x, 0.0);
        var length = Math.Sqrt((cx * cx) + (cy * cy) + (cz * cz));
        return (cx / length, cy / length, cz / length);
    }

    // The part of a convex piece where the exact linear measure f is not below 0, as a piece, or null when no part is. A
    // vertex on the boundary is kept once.
    private static NearestPoints.Piece? Clip(IReadOnlyList<Vector3R> vertices, Func<Vector3R, Rational> f)
    {
        var kept = new List<Vector3R>();
        Vector3R Cut(Vector3R s, Vector3R e, Rational fs, Rational fe) => s + ((e - s) * (fs / (fs - fe)));
        if (vertices.Count == 2)
        {
            var (s, e) = (vertices[0], vertices[1]);
            var (fs, fe) = (f(s), f(e));
            if (fs.Sign >= 0 && fe.Sign >= 0)
                kept.AddRange([s, e]);
            else if (fs.Sign >= 0)
                kept.AddRange([s, Cut(s, e, fs, fe)]);
            else if (fe.Sign >= 0)
                kept.AddRange([Cut(s, e, fs, fe), e]);
        }
        else
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                var (s, e) = (vertices[i], vertices[(i + 1) % vertices.Count]);
                var (fs, fe) = (f(s), f(e));
                if (fe.Sign >= 0)
                {
                    if (fs.Sign < 0 && fe.Sign > 0)
                        kept.Add(Cut(s, e, fs, fe));
                    kept.Add(e);
                }
                else if (fs.Sign > 0)
                {
                    kept.Add(Cut(s, e, fs, fe));
                }
            }
        }

        return kept.Count switch
        {
            0 => null,
            1 => new NearestPoints.Piece([kept[0], kept[0]]),
            _ => new NearestPoints.Piece(kept),
        };
    }

    // Overload for a piece.
    private static NearestPoints.Piece? Clip(NearestPoints.Piece? piece, Func<Vector3R, Rational> f)
        => piece is null ? null : Clip(piece.Vertices, f);

    private static DatumPoint Point(Vector3R v)
    {
        var (x, y, z) = v.ToDoubles();
        return new DatumPoint(x, y, z);
    }
}
