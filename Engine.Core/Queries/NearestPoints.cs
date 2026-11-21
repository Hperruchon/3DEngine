using Engine.Kernel;

namespace Engine.Core.Queries;

// ADR-0180 item 4 (phase K15a, TASK-0432): the minimum distance between two sets of pieces of the owned kernel, and one
// pair of points at that distance. A piece is a segment of two vertices or a convex polygon of three or more vertices.
// Each step uses + - * / and comparisons on exact rationals, so the square of the distance and the two points are exact.
// The caller takes the root of the square as a double. Nothing here enters the log.
internal static class NearestPoints
{
    // One piece, with its box and, for a polygon, its Newell normal. The vertices of a polygon turn counter-clockwise
    // about that normal by its construction, so the test of a point inside does not depend on the plane of the kernel.
    internal sealed class Piece
    {
        public Piece(IReadOnlyList<Vector3R> vertices)
        {
            ArgumentNullException.ThrowIfNull(vertices);
            if (vertices.Count < 2)
                throw new ArgumentException("A piece has two vertices or more.", nameof(vertices));
            Vertices = vertices;
            var (min, max) = (vertices[0], vertices[0]);
            foreach (var v in vertices)
            {
                min = new Vector3R(Min(min.X, v.X), Min(min.Y, v.Y), Min(min.Z, v.Z));
                max = new Vector3R(Max(max.X, v.X), Max(max.Y, v.Y), Max(max.Z, v.Z));
            }

            (Low, High) = (min, max);
            var n = new Vector3R(Rational.Zero, Rational.Zero, Rational.Zero);
            if (vertices.Count >= 3)
            {
                for (var i = 0; i < vertices.Count; i++)
                    n += vertices[i].Cross(vertices[(i + 1) % vertices.Count]);
            }

            Normal = n;
        }

        public IReadOnlyList<Vector3R> Vertices { get; }

        public Vector3R Low { get; }

        public Vector3R High { get; }

        public Vector3R Normal { get; }

        // A polygon whose Newell normal is zero has no area, so its edges alone hold its points.
        public bool IsPolygon => Vertices.Count >= 3 && !Normal.IsZero;
    }

    // The square of a distance, the point on the first piece and the point on the second piece.
    internal readonly record struct Pair(Rational Squared, Vector3R A, Vector3R B)
    {
        public Pair Swapped() => new(Squared, B, A);
    }

    // The nearest pair of points between a piece of a and a piece of b, the first in the order of a, then of b, when
    // several pairs give the minimum. A pair whose boxes are not nearer than the best pair cannot give a nearer pair.
    public static Pair? Nearest(IReadOnlyList<Piece> a, IReadOnlyList<Piece> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        Pair? best = null;
        foreach (var x in a)
        {
            foreach (var y in b)
            {
                if (best is { } known && BoxGap(x, y) >= known.Squared)
                    continue;
                var pair = Between(x, y);
                if (best is null || pair.Squared < best.Value.Squared)
                    best = pair;
                if (best.Value.Squared.IsZero)
                    return best;
            }
        }

        return best;
    }

    // The square of the distance between the boxes of two pieces, the lower bound of the distance between the pieces.
    internal static Rational BoxGap(Piece x, Piece y)
    {
        Rational Axis(Rational lowX, Rational highX, Rational lowY, Rational highY)
        {
            var gap = lowY > highX ? lowY - highX : lowX > highY ? lowX - highY : Rational.Zero;
            return gap * gap;
        }

        return Axis(x.Low.X, x.High.X, y.Low.X, y.High.X)
            + Axis(x.Low.Y, x.High.Y, y.Low.Y, y.High.Y)
            + Axis(x.Low.Z, x.High.Z, y.Low.Z, y.High.Z);
    }

    // The nearest pair between two pieces. Between two convex polygons, a nearest pair has a point on an edge of one of
    // them: two points inside both polygons are nearest only when the planes are parallel, and then an edge point gives
    // the same distance. So the minimum over the edges of each polygon against the other polygon is the distance.
    internal static Pair Between(Piece x, Piece y)
    {
        if (!x.IsPolygon && !y.IsPolygon)
            return EdgesAgainstEdges(x, y);
        if (!x.IsPolygon)
            return EdgesAgainst(x, y);
        if (!y.IsPolygon)
            return EdgesAgainst(y, x).Swapped();

        var best = EdgesAgainst(x, y);
        if (best.Squared.IsZero)
            return best;
        var other = EdgesAgainst(y, x).Swapped();
        return other.Squared < best.Squared ? other : best;
    }

    // The nearest pair between each edge of a piece and a polygon. A segment is a piece with one edge.
    private static Pair EdgesAgainst(Piece edges, Piece polygon)
    {
        Pair? best = null;
        foreach (var (start, end) in Edges(edges))
        {
            var pair = SegmentPolygon(start, end, polygon);
            if (best is null || pair.Squared < best.Value.Squared)
                best = pair;
            if (best.Value.Squared.IsZero)
                break;
        }

        return best!.Value;
    }

    private static Pair EdgesAgainstEdges(Piece x, Piece y)
    {
        Pair? best = null;
        foreach (var (p1, q1) in Edges(x))
        {
            foreach (var (p2, q2) in Edges(y))
            {
                var pair = SegmentSegment(p1, q1, p2, q2);
                if (best is null || pair.Squared < best.Value.Squared)
                    best = pair;
            }
        }

        return best!.Value;
    }

    // The edges of a piece: one segment for two vertices, else each side of the closed polygon.
    private static IEnumerable<(Vector3R Start, Vector3R End)> Edges(Piece piece)
    {
        var count = piece.Vertices.Count;
        if (count == 2)
        {
            yield return (piece.Vertices[0], piece.Vertices[1]);
            yield break;
        }

        for (var i = 0; i < count; i++)
            yield return (piece.Vertices[i], piece.Vertices[(i + 1) % count]);
    }

    // The nearest pair between the segment from a to b and a convex polygon. A segment that crosses the polygon gives
    // the crossing point twice. Else a nearest point on the polygon is on one of its edges, or inside it under an end of
    // the segment: a point inside the segment over a point inside the polygon is nearest only when the segment is
    // parallel to the plane, and then an end or an edge gives the same distance.
    internal static Pair SegmentPolygon(Vector3R a, Vector3R b, Piece polygon)
    {
        var n = polygon.Normal;
        var offset = n.Dot(polygon.Vertices[0]);
        var sa = n.Dot(a) - offset;
        var sb = n.Dot(b) - offset;
        if (!(sa.IsZero && sb.IsZero) && sa.Sign * sb.Sign <= 0)
        {
            var crossing = a + ((b - a) * (sa / (sa - sb)));
            if (Inside(polygon, crossing))
                return new Pair(Rational.Zero, crossing, crossing);
        }

        Pair? best = null;
        void Take(Pair pair)
        {
            if (best is null || pair.Squared < best.Value.Squared)
                best = pair;
        }

        if (Under(a, polygon) is { } fromA)
            Take(fromA);
        if (Under(b, polygon) is { } fromB)
            Take(fromB);
        foreach (var (start, end) in Edges(polygon))
            Take(SegmentSegment(a, b, start, end));
        return best!.Value;
    }

    // The point and its foot on the plane of the polygon, when the foot is inside the polygon or on its edge; else null.
    private static Pair? Under(Vector3R p, Piece polygon)
    {
        var n = polygon.Normal;
        var side = n.Dot(p) - n.Dot(polygon.Vertices[0]);
        var length = n.Dot(n);
        var foot = p - (n * (side / length));
        return Inside(polygon, foot) ? new Pair(side * side / length, p, foot) : null;
    }

    // True when a point of the plane of a convex polygon is inside it or on its edge: on the left of each side, or on it,
    // seen from the side to which the Newell normal points.
    private static bool Inside(Piece polygon, Vector3R q)
    {
        var count = polygon.Vertices.Count;
        for (var i = 0; i < count; i++)
        {
            var v = polygon.Vertices[i];
            var w = polygon.Vertices[(i + 1) % count];
            if ((w - v).Cross(q - v).Dot(polygon.Normal).Sign < 0)
                return false;
        }

        return true;
    }

    // The nearest pair between the segments p1 q1 and p2 q2, by the method of C. Ericson, "Real-Time Collision
    // Detection", section 5.1.9, in exact rationals: the parameters s and t of the two points, each clamped to [0, 1].
    internal static Pair SegmentSegment(Vector3R p1, Vector3R q1, Vector3R p2, Vector3R q2)
    {
        var d1 = q1 - p1;
        var d2 = q2 - p2;
        var r = p1 - p2;
        var a = d1.Dot(d1);
        var e = d2.Dot(d2);
        var f = d2.Dot(r);
        Rational s, t;
        if (a.IsZero && e.IsZero)
        {
            (s, t) = (Rational.Zero, Rational.Zero);
        }
        else if (a.IsZero)
        {
            (s, t) = (Rational.Zero, Clamp(f / e));
        }
        else
        {
            var c = d1.Dot(r);
            if (e.IsZero)
            {
                (s, t) = (Clamp(-c / a), Rational.Zero);
            }
            else
            {
                var b = d1.Dot(d2);
                var denominator = (a * e) - (b * b);
                s = denominator.IsZero ? Rational.Zero : Clamp(((b * f) - (c * e)) / denominator);
                var tn = (b * s) + f;
                if (tn.Sign < 0)
                    (s, t) = (Clamp(-c / a), Rational.Zero);
                else if (tn > e)
                    (s, t) = (Clamp((b - c) / a), Rational.One);
                else
                    t = tn / e;
            }
        }

        var x = p1 + (d1 * s);
        var y = p2 + (d2 * t);
        var g = x - y;
        return new Pair(g.Dot(g), x, y);
    }

    private static Rational Clamp(Rational value) => value.Sign < 0 ? Rational.Zero : value > Rational.One ? Rational.One : value;

    private static Rational Min(Rational a, Rational b) => a < b ? a : b;

    private static Rational Max(Rational a, Rational b) => a > b ? a : b;
}
