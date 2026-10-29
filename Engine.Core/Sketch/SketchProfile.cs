using Engine.Contracts.Sketch;

namespace Engine.Core.Sketch;

// ADR-0036 item 2 (Q1 A): the profile of a sketch is one closed loop of all its lines.
// The code uses + - * / and comparisons only (CLAUDE.md, determinism rules), because the
// loop decides the solid of a command of the log.
public static class SketchProfile
{
    // The loop of the sketch, counter-clockwise, as the positions of its points in the
    // plane, or the reason that the lines do not form a profile. The last point does not
    // repeat the first one. A point with no line does not count.
    public static (IReadOnlyList<(double X, double Y)>? Loop, string? Reason) Find(SketchRecord sketch)
    {
        if (sketch.Circles.Count > 0 || sketch.Arcs.Count > 0)
            return (null, "The sketch holds a circle or an arc, and an extrude reads lines only.");
        if (sketch.Lines.Count == 0)
            return (null, "The sketch has no line.");

        // Each point that a line uses must be the end of exactly two lines.
        var ends = new Dictionary<Guid, List<SketchLine>>();
        foreach (var line in sketch.Lines)
        {
            foreach (var id in new[] { line.Start, line.End })
            {
                if (!ends.TryGetValue(id, out var lines))
                    ends[id] = lines = [];
                lines.Add(line);
            }
        }

        foreach (var point in sketch.Points.Where(point => ends.ContainsKey(point.Id)))
        {
            var count = ends[point.Id].Count;
            if (count != 2)
                return (null, $"The point '{point.Id}' is the end of {count} line{(count == 1 ? string.Empty : "s")}, and each point of a closed loop is the end of 2.");
        }

        // The loop starts at the start of the first line and follows the other line at
        // each point, so the order depends on the record only.
        var first = sketch.Lines[0];
        var order = new List<Guid> { first.Start };
        var at = first.End;
        var previous = first;
        while (at != first.Start)
        {
            order.Add(at);
            var pair = ends[at];
            previous = pair[0] == previous ? pair[1] : pair[0];
            at = previous.Start == at ? previous.End : previous.Start;
        }

        if (order.Count != sketch.Lines.Count)
            return (null, $"The lines form more than one loop: the loop of the first line has {order.Count} of the {sketch.Lines.Count} lines.");

        var position = sketch.Points.ToDictionary(point => point.Id);
        var loop = order.Select(id => (position[id].X, position[id].Y)).ToList();

        // Finding E92 of the codebase review of 2026-10-29: two points of a line at one
        // position give a line of length zero. Until TASK-0106 the check below gave the reason
        // "cross or touch" for it, which names two other lines.
        foreach (var line in sketch.Lines)
        {
            if (position[line.Start].X == position[line.End].X && position[line.Start].Y == position[line.End].Y)
                return (null, $"The line '{line.Id}' has the length zero, because its two points are at one position.");
        }

        // No two lines that share no point may cross or touch.
        for (var i = 0; i < sketch.Lines.Count; i++)
        {
            for (var j = i + 1; j < sketch.Lines.Count; j++)
            {
                var a = sketch.Lines[i];
                var b = sketch.Lines[j];
                if (a.Start == b.Start || a.Start == b.End || a.End == b.Start || a.End == b.End)
                    continue;
                if (Meet(position[a.Start], position[a.End], position[b.Start], position[b.End]))
                    return (null, $"The lines '{a.Id}' and '{b.Id}' cross or touch.");
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

    // True when the segments pq and rs have a common point.
    private static bool Meet(SketchPoint p, SketchPoint q, SketchPoint r, SketchPoint s)
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
    private static double Side(SketchPoint a, SketchPoint b, SketchPoint c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    // True when c, on the line ab, is inside the box of the segment ab.
    private static bool Within(SketchPoint a, SketchPoint b, SketchPoint c)
        => Math.Min(a.X, b.X) <= c.X && c.X <= Math.Max(a.X, b.X)
        && Math.Min(a.Y, b.Y) <= c.Y && c.Y <= Math.Max(a.Y, b.Y);
}
