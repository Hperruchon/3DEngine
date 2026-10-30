using Engine.Contracts.Sketch;

namespace Engine.Core.Sketch;

// ADR-0036 item 2 (Q1 A): the profile of a sketch is one closed loop of all its lines.
// The code uses + - * / and comparisons only (CLAUDE.md, determinism rules), because the
// loop decides the solid of a command of the log.
//
// ADR-0040 (TASK-0126): the loop can also hold arcs, or be one circle alone. The polygon of a
// curve uses + - * / sqrt and comparisons only: the step of an arc comes from halvings of its
// sweep, and each vertex is the one before it turned by the step, so a replay and a rebuild
// give the same bits on each platform.
public static class SketchProfile
{
    // ADR-0040 item 2: the step of a curve has a cosine of at least this number, the cosine of
    // about 5.67 degrees. It is a little below cos(2π / 64), so the 64 segments of a full turn do
    // not depend on the last bit of its six halvings.
    public const double StepCosine = 0.9951;

    // One edge of the loop: a line, or an arc with its index in the record.
    private sealed record Edge(Guid Id, Guid Start, Guid End, SketchArc? Arc);

    // The loop of the sketch, counter-clockwise, as the positions of its points in the
    // plane, or the reason that the lines do not form a profile. The last point does not
    // repeat the first one. A point with no line does not count.
    public static (IReadOnlyList<(double X, double Y)>? Loop, string? Reason) Find(SketchRecord sketch)
    {
        var position = sketch.Points.ToDictionary(point => point.Id);

        // ADR-0040 item 1: a circle is a profile alone.
        if (sketch.Circles.Count > 0)
        {
            if (sketch.Circles.Count > 1 || sketch.Lines.Count > 0 || sketch.Arcs.Count > 0)
                return (null, "A circle is a profile alone, and the sketch holds another circle, line or arc.");
            var circle = sketch.Circles[0];
            var c = position[circle.Center];
            return (Turn((c.X, c.Y), (c.X + circle.Radius, c.Y), null), null);
        }

        if (sketch.Lines.Count == 0 && sketch.Arcs.Count == 0)
            return (null, "The sketch has no line.");

        // With no arc each message says "line", as before ADR-0040; an edge is a line or an arc.
        var noun = sketch.Arcs.Count == 0 ? "line" : "edge";
        List<Edge> edges = [.. sketch.Lines.Select(line => new Edge(line.Id, line.Start, line.End, null)), .. sketch.Arcs.Select(arc => new Edge(arc.Id, arc.Start, arc.End, arc))];
        foreach (var arc in sketch.Arcs)
        {
            var (c, s, e) = (position[arc.Center], position[arc.Start], position[arc.End]);
            if ((s.X == c.X && s.Y == c.Y) || (e.X == c.X && e.Y == c.Y))
                return (null, $"The arc '{arc.Id}' has the radius zero, because its start or its end is at its centre.");
        }

        // Each point that an edge uses as an end must be the end of exactly two edges.
        var ends = new Dictionary<Guid, List<Edge>>();
        foreach (var edge in edges)
        {
            foreach (var id in new[] { edge.Start, edge.End })
            {
                if (!ends.TryGetValue(id, out var list))
                    ends[id] = list = [];
                list.Add(edge);
            }
        }

        foreach (var point in sketch.Points.Where(point => ends.ContainsKey(point.Id)))
        {
            var count = ends[point.Id].Count;
            if (count != 2)
                return (null, $"The point '{point.Id}' is the end of {count} {noun}{(count == 1 ? string.Empty : "s")}, and each point of a closed loop is the end of 2.");
        }

        // The loop starts at the start of the first edge and follows the other edge at each
        // point, so the order depends on the record only. An arc adds the vertices of its
        // polygon between its two points, in the direction of the walk.
        var first = edges[0];
        var loop = new List<(double X, double Y)>();
        var owners = new List<Guid>();
        var at = first.Start;
        var current = first;
        var walked = 0;
        do
        {
            var next = current.Start == at ? current.End : current.Start;
            var vertices = current.Arc is { } arc
                ? Turn((position[arc.Center].X, position[arc.Center].Y), (position[arc.Start].X, position[arc.Start].Y), (position[arc.End].X, position[arc.End].Y))
                : [(position[at].X, position[at].Y)];
            if (current.Arc is not null && current.Start != at)
                vertices = [(position[at].X, position[at].Y), .. vertices.Skip(1).Reverse()];
            loop.AddRange(vertices);
            owners.AddRange(vertices.Select(_ => current.Id));
            walked++;
            at = next;
            var pair = ends[at];
            current = pair[0] == current ? pair[1] : pair[0];
        }
        while (at != first.Start);

        if (walked != edges.Count)
            return (null, $"The {noun}s form more than one loop: the loop of the first {noun} has {walked} of the {edges.Count} {noun}s.");

        // Finding E92 of the codebase review of 2026-10-29: two points of a line at one
        // position give a line of length zero. Until TASK-0106 the check below gave the reason
        // "cross or touch" for it, which names two other lines.
        foreach (var line in sketch.Lines)
        {
            if (position[line.Start].X == position[line.End].X && position[line.Start].Y == position[line.End].Y)
                return (null, $"The line '{line.Id}' has the length zero, because its two points are at one position.");
        }

        // No two segments of the polygon that share no vertex may cross or touch. Segment i goes
        // from vertex i to vertex i + 1 and belongs to the edge of vertex i. With lines only, two
        // segments that share no vertex are two lines that share no point, as before ADR-0040.
        var n = loop.Count;
        for (var i = 0; i < n; i++)
        {
            for (var j = i + 2; j < n; j++)
            {
                if (i == 0 && j == n - 1)
                    continue;
                if (Meet(loop[i], loop[(i + 1) % n], loop[j], loop[(j + 1) % n]))
                    return (null, $"The {noun}s '{owners[i]}' and '{owners[j]}' cross or touch.");
            }
        }

        // Twice the signed area, summed in the order of the loop.
        var twiceArea = 0.0;
        for (var i = 0; i < loop.Count; i++)
        {
            var (x0, y0) = loop[i];
            var (x1, y1) = loop[(i + 1) % loop.Count];
            twiceArea += x0 * y1 - x1 * y0;
        }

        if (twiceArea == 0)
            return (null, "The area of the loop is zero.");
        if (twiceArea < 0)
            loop.Reverse();

        return (loop, null);
    }

    // ADR-0040 item 2: the vertices of a curve about c, counter-clockwise from s, up to the vertex
    // before e. With e null the curve is a full turn, as with an end in the direction of the
    // start. The cosine and the sine of the sweep come from the two directions, each step halves
    // the angle, and each vertex is the one before it turned by the step.
    internal static List<(double X, double Y)> Turn((double X, double Y) c, (double X, double Y) s, (double X, double Y)? e)
    {
        var (ax, ay) = (s.X - c.X, s.Y - c.Y);
        var (cos, sin, full) = (1.0, 0.0, true);
        if (e is { } end)
        {
            var (bx, by) = (end.X - c.X, end.Y - c.Y);
            var scale = Math.Sqrt((ax * ax) + (ay * ay)) * Math.Sqrt((bx * bx) + (by * by));
            (cos, sin) = (((ax * bx) + (ay * by)) / scale, ((ax * by) - (ay * bx)) / scale);
            full = sin == 0 && cos > 0;
        }

        var count = 1;
        while (full || sin < 0 || cos < StepCosine)
        {
            // The half of an angle in (0, 2π] has a sine that is not negative, and a cosine with
            // the sign of the sine of the angle; the half of a full turn is (−1, 0).
            (cos, sin) = full
                ? (-1.0, 0.0)
                : ((sin < 0 ? -1 : 1) * Math.Sqrt(Math.Max(0, (1 + cos) / 2)), Math.Sqrt(Math.Max(0, (1 - cos) / 2)));
            full = false;
            count *= 2;
        }

        var vertices = new List<(double X, double Y)>(count) { s };
        var (x, y) = (ax, ay);
        for (var i = 1; i < count; i++)
        {
            (x, y) = ((x * cos) - (y * sin), (x * sin) + (y * cos));
            vertices.Add((c.X + x, c.Y + y));
        }

        return vertices;
    }

    // True when the segments pq and rs have a common point.
    private static bool Meet((double X, double Y) p, (double X, double Y) q, (double X, double Y) r, (double X, double Y) s)
    {
        var d1 = Side(r, s, p);
        var d2 = Side(r, s, q);
        var d3 = Side(p, q, r);
        var d4 = Side(p, q, s);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
            return true;

        return (d1 == 0 && Within(r, s, p))
            || (d2 == 0 && Within(r, s, q))
            || (d3 == 0 && Within(p, q, r))
            || (d4 == 0 && Within(p, q, s));
    }

    // The cross product of (b − a) and (c − a): positive when c is left of the line ab.
    private static double Side((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    // True when c, on the line ab, is inside the box of the segment ab.
    private static bool Within((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        => Math.Min(a.X, b.X) <= c.X && c.X <= Math.Max(a.X, b.X)
        && Math.Min(a.Y, b.Y) <= c.Y && c.Y <= Math.Max(a.Y, b.Y);
}
