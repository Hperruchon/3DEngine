using Engine.Contracts.Sketch;
using Engine.Core.Drawing;
using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 items 15 and 16 (phase K15c, TASK-0444): the distance from the side of a cylinder or a cone when the nearest
// point can be past an end of the side or anywhere on the side of a cone, and the distance between two curved faces other
// than two spheres. A side of revolution has a profile: the segment from (R0, 0) to (R1, H) in the half-plane (ρ, z) of a
// point, with ρ the distance to the axis and z the height along the axis from the base; a cylinder has R1 = R0 and a cone
// R1 = 0. The distance from a point to the side is the distance from its (ρ, z) to the profile. Each step that decides a
// case uses + - * / and comparisons on exact rationals, except the decisions of a sphere (item 16), which compare doubles;
// the values then leave as doubles from + - * / and sqrt. A piece with no closed form gives null, and the caller refuses
// the case with E-MEASURE-CURVED when that piece can be nearer than the best one.
internal static class RevolvedDistance
{
    // The side from P to P + A, with the radius R0 at P and R1 at P + A. Length is A · A and H its root.
    internal sealed class Revolution
    {
        public Revolution(Vector3R p, Vector3R a, Rational r0, Rational r1)
        {
            (P, A, R0, R1, Length) = (p, a, r0, r1, a.Dot(a));
            H = Math.Sqrt(Length.ToDouble());
        }

        public Vector3R P { get; }

        public Vector3R A { get; }

        public Rational R0 { get; }

        public Rational R1 { get; }

        public Rational Length { get; }

        public double H { get; }

        public static Revolution Of(MeasureCurve.Surface side)
        {
            ArgumentNullException.ThrowIfNull(side);
            return new(side.Base, side.Axis, side.Radius, side.Kind == SideCurveKind.Cone ? Rational.Zero : side.Radius);
        }

        // The place of x along the axis, as a part of A: 0 at the base and 1 at the other end.
        public Rational S(Vector3R x) => (x - P).Dot(A) / Length;

        // The square of the distance from x to the line of the axis.
        public Rational Radial(Vector3R x)
        {
            var along = (x - P).Dot(A);
            return (x - P).Dot(x - P) - (along * along / Length);
        }

        // The radius of the profile at the height s, clamped to the side: the ρ of the nearest point of the profile to each
        // point of the plane across the axis at s. Under the base it is R0, over the other end R1.
        public Rational Target(Rational s)
        {
            var clamped = s.Sign < 0 ? Rational.Zero : s > Rational.One ? Rational.One : s;
            return R0 + ((R1 - R0) * clamped);
        }

        // A lower bound of the distance from a piece to the side: its distance to the segment of the axis less R0, because
        // each point of the side is within R0 of that segment.
        public double Lower(NearestPoints.Piece piece)
        {
            var near = NearestPoints.Between(new NearestPoints.Piece([P, P + A]), piece);
            return Math.Max(0, Math.Sqrt(near.Squared.ToDouble()) - Math.Max(R0.ToDouble(), R1.ToDouble()));
        }

        // The nearest point of the profile to (rho, z), in doubles: the distance and the part t of the profile, 0 at
        // (R0, 0) and 1 at (R1, H).
        public (double Distance, double T) Profile(double rho, double z)
        {
            var (r0, r1) = (R0.ToDouble(), R1.ToDouble());
            var dr = r1 - r0;
            var t = Math.Clamp((((rho - r0) * dr) + (z * H)) / ((dr * dr) + (H * H)), 0, 1);
            var (pr, pz) = (r0 + (t * dr), t * H);
            return (Math.Sqrt(((rho - pr) * (rho - pr)) + ((z - pz) * (z - pz))), t);
        }

        // The point of the side at the part t of the profile, in the half-plane of the point x, or of the direction across
        // the axis when x is on the axis.
        public DatumPoint OnSide(Vector3R x, double t, double sign = 1)
        {
            var (ex, ey, ez) = Outward(x);
            var rho = R0.ToDouble() + (t * (R1.ToDouble() - R0.ToDouble()));
            var (px, py, pz) = P.ToDoubles();
            var (ax, ay, az) = A.ToDoubles();
            return new DatumPoint(px + (t * ax) + (sign * rho * ex), py + (t * ay) + (sign * rho * ey), pz + (t * az) + (sign * rho * ez));
        }

        // The unit vector from the axis to x, at a right angle to the axis, in doubles from the exact vector.
        public (double X, double Y, double Z) Outward(Vector3R x)
        {
            var away = (x - P) - (A * ((x - P).Dot(A) / Length));
            if (away.IsZero)
                return CurvedDistance.Across(A);
            var (dx, dy, dz) = away.ToDoubles();
            var length = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
            return (dx / length, dy / length, dz / length);
        }
    }

    // Item 15: the nearest pair of the side and a piece, or null when the piece has no closed form. A piece in a plane
    // across the axis, a segment along the axis, and a polygon in a plane along the axis whose edges are each across or
    // along the axis have one; each other piece has none.
    public static Candidate? Nearest(Revolution side, IReadOnlyList<Vector3R> vertices)
    {
        ArgumentNullException.ThrowIfNull(side);
        ArgumentNullException.ThrowIfNull(vertices);
        var s = vertices.Select(side.S).ToList();
        if (s.All(value => value == s[0]))
            return Level(side, vertices, s[0]);
        if (vertices.Count == 2)
            return (vertices[1] - vertices[0]).Cross(side.A).IsZero ? Along(side, vertices[0], vertices[1]) : null;
        var piece = new NearestPoints.Piece(vertices);
        if (!piece.IsPolygon || !piece.Normal.Dot(side.A).IsZero)
            return null;

        // A polygon in a plane along the axis. In the coordinates (w, z) of the plane, with w = 0 on the line of the plane
        // nearest to the axis, the distance is the distance of (√(δ² + w²), z) to the profile. Its gradient is 0 only on
        // the line w = 0, and off the side. On that line the distance is a convex function of z, so its minimum over the
        // polygon is at an end of the line, on an edge, or it is 0 where the line meets the side. The side meets the plane
        // in a curve that goes down to the rim of the base or to the apex on the axis; the caller cuts the piece at the
        // plane of the base, so that curve leaves the polygon through an edge. So the minimum is on an edge.
        Candidate? best = null;
        for (var i = 0; i < vertices.Count; i++)
        {
            var (from, to) = (vertices[i], vertices[(i + 1) % vertices.Count]);
            var found = s[i] == s[(i + 1) % vertices.Count] ? Level(side, [from, to], s[i])
                : (to - from).Cross(side.A).IsZero ? Along(side, from, to) : null;
            if (found is null)
                return null;
            best = Better(best, found);
        }

        return best;
    }

    // A piece in the plane across the axis at the height s. The distance to the profile, at the fixed height, is a convex
    // function of ρ, and the piece holds each ρ from its nearest point to the axis to its farthest vertex. So the minimum is
    // at the target radius of the height, clamped to that range.
    private static Candidate Level(Revolution side, IReadOnlyList<Vector3R> vertices, Rational s)
    {
        var foot = side.P + (side.A * s);
        var near = NearestPoints.Between(new NearestPoints.Piece([foot, foot]), new NearestPoints.Piece(vertices));
        var (far, vertex) = CurvedDistance.Farthest(vertices, side.Radial);
        var target = side.Target(s);
        var squared = target * target;
        var z = s.ToDouble() * side.H;
        if (squared <= near.Squared || squared >= far)
        {
            var (rho2, x) = squared <= near.Squared ? (near.Squared, near.B) : (far, vertex);
            var (distance, t) = side.Profile(Math.Sqrt(rho2.ToDouble()), z);
            return new Candidate(distance, side.OnSide(x, t), CurvedDistance.Point(x));
        }

        var point = CurvedDistance.CrossingPoint(near.B, vertex, x => side.Radial(x) - squared);
        if (s.Sign >= 0 && s <= Rational.One)
            return new Candidate(0, point, point);
        var exact = Vector3R.FromDoubles(point.X, point.Y, point.Z);
        var (d, at) = side.Profile(target.ToDouble(), z);
        return new Candidate(d, side.OnSide(exact, at), point);
    }

    // A segment along the axis at the distance ρ from it. The distance to the profile is a convex function of z, so the
    // minimum is at the nearest height of the profile to the line, clamped to the segment: each height of the side for a
    // cylinder, and the height where the cone has the radius ρ, or its base or its apex, for a cone.
    private static Candidate Along(Revolution side, Vector3R first, Vector3R second)
    {
        var (s0, s1) = (side.S(first), side.S(second));
        var (from, to, low, high) = s0 <= s1 ? (first, second, s0, s1) : (second, first, s1, s0);
        var rho2 = side.Radial(from);
        Vector3R At(Rational s) => low == high ? from : from + ((to - from) * ((s - low) / (high - low)));

        Rational chosen;
        if (high.Sign < 0)
        {
            chosen = high;
        }
        else if (low > Rational.One)
        {
            chosen = low;
        }
        else
        {
            var (lo, hi) = (low.Sign < 0 ? Rational.Zero : low, high > Rational.One ? Rational.One : high);
            if (side.R0 == side.R1)
            {
                chosen = lo;
            }
            else
            {
                // The height where the radius is ρ is (R0 - ρ) / (R0 - R1); it is at most lo when ρ ≥ R0 - lo (R0 - R1),
                // and at least hi when ρ ≤ R0 - hi (R0 - R1), a radius of the profile and so not below 0.
                var (atLow, atHigh) = (side.R0 - (lo * (side.R0 - side.R1)), side.R0 - (hi * (side.R0 - side.R1)));
                if (atLow.Sign <= 0 || rho2 >= atLow * atLow)
                {
                    chosen = lo;
                }
                else if (rho2 <= atHigh * atHigh)
                {
                    chosen = hi;
                }
                else
                {
                    var height = (side.R0.ToDouble() - Math.Sqrt(rho2.ToDouble())) / (side.R0 - side.R1).ToDouble();
                    var part = (height - low.ToDouble()) / (high - low).ToDouble();
                    var (fx, fy, fz) = from.ToDoubles();
                    var (dx, dy, dz) = (to - from).ToDoubles();
                    var point = new DatumPoint(fx + (part * dx), fy + (part * dy), fz + (part * dz));
                    return new Candidate(0, point, point);
                }
            }
        }

        var x = At(chosen);
        var (distance, t) = side.Profile(Math.Sqrt(rho2.ToDouble()), chosen.ToDouble() * side.H);
        return new Candidate(distance, side.OnSide(x, t), CurvedDistance.Point(x));
    }

    // Item 16: a sphere with the centre c and the radius s against a side. With the distance m from c to the side and the
    // distance M from c to its farthest point, the sphere is m - s from the side, s - M around it, or it crosses it. The
    // farthest point is an end of the profile on the other side of the axis. The pair is on the side, then on the sphere.
    public static DistanceInfo Sphere(Revolution side, Vector3R c, Rational radius)
    {
        ArgumentNullException.ThrowIfNull(side);
        var rho = Math.Sqrt(side.Radial(c).ToDouble());
        var z = side.S(c).ToDouble() * side.H;
        var s = radius.ToDouble();
        var (m, t) = side.Profile(rho, z);
        var (r0, r1) = (side.R0.ToDouble(), side.R1.ToDouble());
        var (far0, far1) = (Math.Sqrt(((rho + r0) * (rho + r0)) + (z * z)), Math.Sqrt(((rho + r1) * (rho + r1)) + ((z - side.H) * (z - side.H))));
        var (big, end, endRadius, endZ) = far0 >= far1 ? (far0, 0.0, r0, 0.0) : (far1, 1.0, r1, side.H);
        var (cx, cy, cz) = c.ToDoubles();
        DatumPoint OnSphere(DatumPoint q, double length)
            => new(cx + ((q.X - cx) * s / length), cy + ((q.Y - cy) * s / length), cz + ((q.Z - cz) * s / length));

        if (s <= m)
        {
            var near = side.OnSide(c, t);
            return new DistanceInfo(m - s, near, OnSphere(near, m));
        }

        if (s >= big)
        {
            var farthest = side.OnSide(c, end, -1);
            return new DistanceInfo(s - big, farthest, OnSphere(farthest, big));
        }

        // The sphere crosses the side. On the half-plane of c the distance grows from m at the nearest point of the profile
        // to the end of the farthest point; then on the circle of that end it grows to M on the other side of the axis.
        var (pr, pz) = (r0 + (t * (r1 - r0)), t * side.H);
        var (er, ez) = (endRadius, endZ);
        var reach = Math.Sqrt(((er - rho) * (er - rho)) + ((ez - z) * (ez - z)));
        DatumPoint point;
        if (s <= reach)
        {
            // |q(λ) - (ρ, z)|² = s² on q(λ) = (pr, pz) + λ (er - pr, ez - pz): the larger root, where the square passes s².
            var (dr, dz, qr, qz) = (er - pr, ez - pz, pr - rho, pz - z);
            var (aa, bb, cc) = ((dr * dr) + (dz * dz), 2 * ((dr * qr) + (dz * qz)), (qr * qr) + (qz * qz) - (s * s));
            var lambda = aa == 0 ? 0 : (-bb + Math.Sqrt(Math.Max(0, (bb * bb) - (4 * aa * cc)))) / (2 * aa);
            var part = (pz + (lambda * dz)) / side.H;
            point = Place(side, c, part, pr + (lambda * dr), 1, 0);
        }
        else
        {
            var cos = Math.Clamp(((er * er) + (rho * rho) + ((ez - z) * (ez - z)) - (s * s)) / (2 * er * rho), -1, 1);
            point = Place(side, c, end, er, cos, Math.Sqrt(1 - (cos * cos)));
        }

        return new DistanceInfo(0, point, point);
    }

    // The point at the part t along the axis, at the radius rho in the direction cos e + sin (â × e), with e the outward
    // direction of x.
    private static DatumPoint Place(Revolution side, Vector3R x, double t, double rho, double cos, double sin)
    {
        var (ex, ey, ez) = side.Outward(x);
        var (ax, ay, az) = side.A.ToDoubles();
        var (ux, uy, uz) = (ax / side.H, ay / side.H, az / side.H);
        var (vx, vy, vz) = ((uy * ez) - (uz * ey), (uz * ex) - (ux * ez), (ux * ey) - (uy * ex));
        var (px, py, pz) = side.P.ToDoubles();
        return new DatumPoint(
            px + (t * ax) + (rho * ((cos * ex) + (sin * vx))),
            py + (t * ay) + (rho * ((cos * ey) + (sin * vy))),
            pz + (t * az) + (rho * ((cos * ez) + (sin * vz))));
    }

    // Item 16: the sides of two cylinders with parallel axes. Each side is a circle times a span along the axis, so the
    // square of the distance is the square of the distance of the two circles plus the square of the gap of the two spans.
    // The circles are D - r - s apart, |r - s| - D one inside the other, or they cross; each case is exact. Null when the
    // axes are not parallel.
    public static DistanceInfo? Cylinders(MeasureCurve.Surface first, MeasureCurve.Surface second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var (p, a, r, q, b, s) = (first.Base, first.Axis, first.Radius, second.Base, second.Axis, second.Radius);
        if (!a.Cross(b).IsZero)
            return null;
        var length = a.Dot(a);
        var along = (q - p).Dot(a);
        var across = (q - p) - (a * (along / length));
        var squared = across.Dot(across);

        // The spans along a, as multiples of a · a: [0, a · a] and the span of the second side.
        var (low, high) = (Min(along, along + b.Dot(a)), Max(along, along + b.Dot(a)));
        var (atFirst, atSecond) = high.Sign < 0 ? (Rational.Zero, high) : low > length ? (length, low) : (Max(low, Rational.Zero), Max(low, Rational.Zero));
        var gap = (atSecond - atFirst) * (atSecond - atFirst) / length;

        var distance = Math.Sqrt(squared.ToDouble());
        var (ux, uy, uz) = squared.IsZero ? CurvedDistance.Across(a) : Unit(across.ToDoubles(), distance);
        var (rd, sd) = (r.ToDouble(), s.ToDouble());
        double circles;
        (double First, double Second, double Height) place;
        if (squared >= (r + s) * (r + s))
        {
            (circles, place) = (distance - rd - sd, (rd, distance - sd, 0));
        }
        else if (squared <= (r - s) * (r - s))
        {
            circles = Math.Abs(rd - sd) - distance;
            place = r >= s ? (rd, distance + sd, 0) : (-rd, distance - sd, 0);
        }
        else
        {
            // The circles meet at along = (D² + r² - s²) / 2D from the first axis, at the height √(r² - along²) across.
            var meet = ((squared + (r * r) - (s * s)) / 2).ToDouble() / distance;
            (circles, place) = (0, (meet, meet, Math.Sqrt(Math.Max(0, (rd * rd) - (meet * meet)))));
        }

        var (ax, ay, az) = a.ToDoubles();
        var h = Math.Sqrt(length.ToDouble());
        var (vx, vy, vz) = ((ay * uz) - (az * uy), (az * ux) - (ax * uz), (ax * uy) - (ay * ux));
        DatumPoint At(Rational height, double radial)
        {
            var (bx, by, bz) = (p + (a * (height / length))).ToDoubles();
            return new DatumPoint(
                bx + (radial * ux) + (place.Height * vx / h),
                by + (radial * uy) + (place.Height * vy / h),
                bz + (radial * uz) + (place.Height * vz / h));
        }

        return new DistanceInfo(Math.Sqrt((circles * circles) + gap.ToDouble()), At(atFirst, place.First), At(atSecond, place.Second));
    }

    private static (double X, double Y, double Z) Unit((double X, double Y, double Z) v, double length) => (v.X / length, v.Y / length, v.Z / length);

    private static Rational Min(Rational x, Rational y) => x <= y ? x : y;

    private static Rational Max(Rational x, Rational y) => x >= y ? x : y;

    private static Candidate Better(Candidate? best, Candidate found) => best is null || found.Distance < best.Distance ? found : best;
}

// One pair of points at the distance, on the curve and on the other side.
internal sealed record Candidate(double Distance, DatumPoint OnCurve, DatumPoint OnOther);
