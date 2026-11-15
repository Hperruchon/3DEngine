using Engine.Contracts;
using Engine.Core.Features;
using Engine.Kernel;

namespace Engine.Core.Drawing;

// ADR-0142 item 2 (phase K9a, TASK-0375): the six standard views, with the axes of the flat views of the window: Front
// from the side of negative y with z up, Right from positive x, Back from positive y, Left from negative x, Top from above
// with y up, and Bottom from below.
internal enum StandardView
{
    Front,
    Back,
    Right,
    Left,
    Top,
    Bottom,
}

// ADR-0142 item 2: a view as three exact directions: the right and the up of the view, and the direction toward the eye.
// A point p goes to (p · Right, p · Up) on the view, and p · TowardEye is its depth: a greater depth is nearer to the eye.
internal readonly record struct ViewFrame(Vector3R Right, Vector3R Up, Vector3R TowardEye)
{
    public static ViewFrame Of(StandardView view)
    {
        static Vector3R V(long x, long y, long z) => new(x, y, z);
        return view switch
        {
            StandardView.Front => new(V(1, 0, 0), V(0, 0, 1), V(0, -1, 0)),
            StandardView.Back => new(V(-1, 0, 0), V(0, 0, 1), V(0, 1, 0)),
            StandardView.Right => new(V(0, 1, 0), V(0, 0, 1), V(1, 0, 0)),
            StandardView.Left => new(V(0, -1, 0), V(0, 0, 1), V(-1, 0, 0)),
            StandardView.Top => new(V(1, 0, 0), V(0, 1, 0), V(0, 0, 1)),
            StandardView.Bottom => new(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1)),
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "A standard view is one of the six views."),
        };
    }

    // A frame projects with one scale on the two axes and no shear: Right and Up are orthogonal with one length, and
    // Right x Up points toward the eye. The test uses + - * only, so it is exact.
    public bool IsValid
    {
        get
        {
            var cross = Right.Cross(Up);
            return !Right.IsZero && Right.Dot(Up).IsZero && Right.Dot(Right) == Up.Dot(Up)
                && cross.Cross(TowardEye).IsZero && cross.Dot(TowardEye).Sign > 0;
        }
    }
}

// ADR-0142 item 3: an edge between faces of two tags, or the outline of a curved side: an edge between two faces of one
// tag where one face looks toward the eye and the other does not.
internal enum ViewLineKind
{
    Edge,
    Outline,
}

// ADR-0142 items 3 to 5: one line of a view, on the plane of the view, visible or hidden, with the body and the tags of the
// two faces of its edge, so a later phase finds the values of the design that the line shows. ADR-0143 item 2: a line with
// a circle is the arc of that circle from From to To, counter-clockwise on the view; From equal to To is the whole circle.
internal sealed record ViewLine(
    Guid Body, (Rational X, Rational Y) From, (Rational X, Rational Y) To, bool Visible, ViewLineKind Kind, FaceTag First, FaceTag Second,
    ViewCircle? Circle = null);

// ADR-0143 item 2: a circle on the plane of the view, with the centre and the radius of the design.
internal sealed record ViewCircle((Rational X, Rational Y) Centre, Rational Radius);

// ADR-0143 item 1: the kind of a curved side of the design.
internal enum SideCurveKind
{
    Cylinder,
    Cone,
    Sphere,
}

// ADR-0143 item 1: the curve of the design of a curved side: for a cylinder or a cone the centre of its base, the axis from
// the base to the other end or to the apex, and the radius of the base; for a sphere the centre and the radius.
internal sealed record SideCurve(SideCurveKind Kind, Vector3R Base, Vector3R Axis, Rational Radius);

// ADR-0142 (phase K9a, TASK-0375): the visible and the hidden lines of the live bodies seen along a view, computed outside
// the owned kernel from the faces and the edges that it gives (Brep), so the six operations of the closed feature boundary
// of the charter stay. The lines are presentation, as a tessellation is (ADR-0007, ADR-0142 item 1): no command, no event
// and no Document holds them. Each test of a point, a crossing and a depth is exact; a filter of rounded values goes
// first, and rounding to the nearest keeps the order, so the filter rejects only what the exact test rejects.
internal static class ViewEdges
{
    // ADR-0142 item 6: the lines of the live bodies of the Document, in the order of their handles, and each body that
    // gives no faces of the kernel with its reason: a mesh body of an STL file, or a body with a fault.
    public static (IReadOnlyList<ViewLine> Lines, IReadOnlyList<(Guid Body, string Reason)> Skipped) Of(Document document, ViewFrame frame)
    {
        ArgumentNullException.ThrowIfNull(document);
        var bodies = new List<(Guid Body, Brep Brep)>();
        var skipped = new List<(Guid Body, string Reason)>();
        var curves = new Dictionary<FaceTag, SideCurve>();
        foreach (var record in document.Bodies.OrderBy(body => body.Handle.Id))
        {
            if (record.Kind == Commands.ImportStlCommandHandler.MeshKind)
            {
                skipped.Add((record.Handle.Id, "The body is a mesh body of an STL file, which has no faces of the owned kernel (ADR-0142 item 6)."));
                continue;
            }

            var (solid, reason) = KernelProjection.OfRecord(document, record);
            if (solid is null)
            {
                skipped.Add((record.Handle.Id, reason!));
                continue;
            }

            try
            {
                bodies.Add((record.Handle.Id, Brep.Of(solid)));
                foreach (var (tag, curve) in KernelProjection.Curves(document, record.Handle.Id))
                    curves.TryAdd(tag, curve);
            }
            catch (InvalidOperationException ex)
            {
                skipped.Add((record.Handle.Id, ex.Message));
            }
        }

        return (Of(bodies, frame, curves), skipped);
    }

    // ADR-0142 items 3 to 5: the lines of the bodies along the frame, in the order of the bodies, their edges and the
    // place on each edge. ADR-0143: with the curves of the design, the edges of a curved side become circles and arcs.
    public static IReadOnlyList<ViewLine> Of(IReadOnlyList<(Guid Body, Brep Brep)> bodies, ViewFrame frame, IReadOnlyDictionary<FaceTag, SideCurve>? curves = null)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        if (!frame.IsValid)
            throw new ArgumentException("A view has a right and an up of one length at a right angle, and right x up toward the eye.", nameof(frame));

        var segments = new List<Segment>();
        var fronts = new List<Front>();
        foreach (var (body, brep) in bodies)
        {
            var points = brep.Vertices.Select(v => new Point(v.Dot(frame.Right), v.Dot(frame.Up))).ToArray();
            var facing = brep.Faces.Select(face => face.Plane.Normal.Dot(frame.TowardEye).Sign).ToArray();

            foreach (var edge in brep.Edges)
            {
                var (a, b) = (points[edge.From], points[edge.To]);

                // An edge along the direction of the view is a point on it: it is no line and no border of a face.
                if (a == b)
                    continue;
                var (first, second) = edge.Faces.Count == 2 ? (edge.Faces[0], edge.Faces[1]) : (edge.Faces[0], edge.Faces[0]);
                var (tagA, tagB) = (brep.Faces[first].Tag, brep.Faces[second].Tag);
                ViewLineKind? kind = tagA != tagB || edge.Faces.Count != 2 ? ViewLineKind.Edge
                    : (facing[first] > 0) != (facing[second] > 0) ? ViewLineKind.Outline
                    : null;
                segments.Add(new Segment(body, a, b, brep.Vertices[edge.From], brep.Vertices[edge.To], kind, tagA, tagB));
            }

            // ADR-0142 item 4: only a face that looks toward the eye covers a point. A ray from a point to the eye leaves
            // each closed solid that it crosses through such a face.
            for (var f = 0; f < brep.Faces.Count; f++)
            {
                if (facing[f] <= 0)
                    continue;
                var face = brep.Faces[f];
                var loops = face.Loops.Select(loop => loop.Vertices.Select(v => points[v]).ToArray()).ToArray();
                fronts.Add(new Front(face.Plane, loops));
            }
        }

        // 1. Each line is cut at each place where another edge crosses it or lies on it, so the visibility of a piece is
        // one value along it.
        var pieces = new List<Piece>();
        for (var s = 0; s < segments.Count; s++)
        {
            var segment = segments[s];
            if (segment.Kind is null)
                continue;
            var cuts = new SortedSet<Rational> { Rational.Zero, Rational.One };
            for (var o = 0; o < segments.Count; o++)
            {
                if (o != s)
                    Cuts(segment, segments[o], cuts);
            }

            var places = cuts.ToArray();
            for (var i = 0; i + 1 < places.Length; i++)
            {
                var (t0, t1) = (places[i], places[i + 1]);
                var middle = (t0 + t1) / 2;
                var at = segment.At(middle);
                var point = segment.Start + ((segment.End - segment.Start) * middle);

                // 2. ADR-0142 item 4: a piece is hidden when the middle of it is inside a face that looks toward the eye or
                // on its border, and that face is strictly nearer to the eye there.
                var hidden = fronts.Any(front => front.Covers(at, point));
                pieces.Add(new Piece(s, t0, t1, segment.At(t0), segment.At(t1), point.Dot(frame.TowardEye), !hidden));
            }
        }

        // 3. ADR-0142 item 5: pieces with the same ends are one line. A visible piece goes before a hidden one, and the
        // piece nearest to the eye goes first; a hidden line under a line is not drawn.
        var kept = new bool[pieces.Count];
        foreach (var group in pieces.Select((piece, index) => (piece, index)).GroupBy(entry => Key(entry.piece.From, entry.piece.To)))
        {
            var best = group.OrderByDescending(entry => entry.piece.Visible).ThenByDescending(entry => entry.piece.Depth).ThenBy(entry => entry.index).First();
            kept[best.index] = true;
        }

        // 4. The kept pieces of one edge that follow each other with one visibility are one line again.
        var lines = new List<ViewLine>();
        for (var i = 0; i < pieces.Count; i++)
        {
            if (!kept[i])
                continue;
            var (start, end) = (pieces[i], pieces[i]);
            while (i + 1 < pieces.Count && kept[i + 1] && pieces[i + 1].Segment == start.Segment && pieces[i + 1].Visible == start.Visible
                && pieces[i + 1].T0 == end.T1)
                end = pieces[++i];
            var segment = segments[start.Segment];
            lines.Add(new ViewLine(segment.Body, (start.From.X, start.From.Y), (end.To.X, end.To.Y), start.Visible, segment.Kind!.Value, segment.First, segment.Second));
        }

        return curves is null || curves.Count == 0 ? lines : Curved(lines, frame, curves);
    }

    // ADR-0143 items 2 and 3: with a frame of length 1, a piece of an end of a cylinder or a cone whose axis is along the
    // view, and a piece of the outline of a sphere, is an arc of the circle of the design; the arcs of one circle with one
    // visibility that follow each other are one arc, or the whole circle. The pieces of an end whose axis is across the
    // view lie on one line, and the pieces that follow each other with one visibility are one line. Each other line stays.
    private static List<ViewLine> Curved(List<ViewLine> lines, ViewFrame frame, IReadOnlyDictionary<FaceTag, SideCurve> curves)
    {
        if (frame.Right.Dot(frame.Right) != Rational.One)
            return lines;
        var groups = new Dictionary<object, (int First, List<ViewLine> Members, bool Arc)>();
        var output = new List<(int Order, ViewLine Line)>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            object? key = null;
            switch (CurveOf(line, curves))
            {
                case { Kind: SideCurveKind.Sphere } sphere when line.Kind == ViewLineKind.Outline:
                    line = Arc(line, new ViewCircle(Project(sphere.Base, frame), sphere.Radius));
                    break;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } end when line.Kind == ViewLineKind.Edge && end.Axis.Cross(frame.TowardEye).IsZero:
                    line = Arc(line, new ViewCircle(Project(end.Base, frame), end.Radius));
                    break;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } end when line.Kind == ViewLineKind.Edge && end.Axis.Dot(frame.TowardEye).IsZero:
                    key = (line.Body, line.Visible, line.Kind, Tags(line), Support(line));
                    break;
            }

            if (line.Circle is { } circle)
                key = (line.Body, line.Visible, line.Kind, Tags(line), circle);
            if (key is null)
            {
                output.Add((i, line));
                continue;
            }

            if (!groups.TryGetValue(key, out var group))
                groups[key] = group = (i, [], line.Circle is not null);
            group.Members.Add(line);
        }

        foreach (var (first, members, arc) in groups.Values)
            output.AddRange((arc ? Chains(members) : Runs(members)).Select(line => (first, line)));
        return [.. output.OrderBy(entry => entry.Order).Select(entry => entry.Line)];
    }

    // The curve of the curved side of a line: the one curved tag of an edge, or the tag of an outline.
    private static SideCurve? CurveOf(ViewLine line, IReadOnlyDictionary<FaceTag, SideCurve> curves)
    {
        if (line.Kind == ViewLineKind.Outline)
            return curves.TryGetValue(line.First, out var outline) ? outline : null;
        var (first, second) = (curves.TryGetValue(line.First, out var a) ? a : null, curves.TryGetValue(line.Second, out var b) ? b : null);
        return (first, second) switch
        {
            ({ } only, null) => only,
            (null, { } only) => only,
            _ => null,
        };
    }

    private static (Rational X, Rational Y) Project(Vector3R p, ViewFrame frame) => (p.Dot(frame.Right), p.Dot(frame.Up));

    private static (FaceTag, FaceTag) Tags(ViewLine line)
        => string.CompareOrdinal(line.First.ToString(), line.Second.ToString()) <= 0 ? (line.First, line.Second) : (line.Second, line.First);

    // The line through a piece as a x + b y = c, scaled so that its first coefficient that is not zero is 1.
    private static (Rational A, Rational B, Rational C) Support(ViewLine line)
    {
        var (a, b) = (line.To.Y - line.From.Y, line.From.X - line.To.X);
        var scale = Rational.One / (!a.IsZero ? a : b);
        return (a * scale, b * scale, ((a * line.From.X) + (b * line.From.Y)) * scale);
    }

    // The piece as an arc counter-clockwise from From to To about the centre; a piece through the centre stays a line.
    private static ViewLine Arc(ViewLine line, ViewCircle circle)
    {
        var (c, from, to) = (circle.Centre, line.From, line.To);
        var turn = ((from.X - c.X) * (to.Y - c.Y)) - ((from.Y - c.Y) * (to.X - c.X));
        return turn.Sign switch
        {
            > 0 => line with { Circle = circle },
            < 0 => line with { From = to, To = from, Circle = circle },
            _ => line,
        };
    }

    // The arcs of one circle, joined where one ends and the next one starts: an open chain from its first arc to its last,
    // and a closed chain as the whole circle.
    private static IEnumerable<ViewLine> Chains(List<ViewLine> arcs)
    {
        var next = new Dictionary<(Rational, Rational), int>();
        var ends = new HashSet<(Rational, Rational)>();
        for (var i = 0; i < arcs.Count; i++)
        {
            next.TryAdd(arcs[i].From, i);
            ends.Add(arcs[i].To);
        }

        var used = new bool[arcs.Count];
        var starts = Enumerable.Range(0, arcs.Count).Where(i => !ends.Contains(arcs[i].From)).Concat(Enumerable.Range(0, arcs.Count));
        foreach (var start in starts)
        {
            if (used[start])
                continue;
            var (first, last) = (arcs[start], arcs[start]);
            used[start] = true;
            while (next.TryGetValue(last.To, out var follow) && !used[follow])
            {
                used[follow] = true;
                last = arcs[follow];
            }

            yield return first with { To = last.To };
        }
    }

    // The pieces on one line, in their order along it, joined where one ends and the next one starts.
    private static IEnumerable<ViewLine> Runs(List<ViewLine> pieces)
    {
        var (dx, dy) = (pieces[0].To.X - pieces[0].From.X, pieces[0].To.Y - pieces[0].From.Y);
        Rational Along((Rational X, Rational Y) p) => (p.X * dx) + (p.Y * dy);
        var ordered = pieces.Select(piece => Along(piece.From) <= Along(piece.To) ? piece : piece with { From = piece.To, To = piece.From })
            .OrderBy(piece => Along(piece.From)).ToList();
        var current = ordered[0];
        foreach (var piece in ordered.Skip(1))
        {
            if (piece.From == current.To)
            {
                current = current with { To = piece.To };
                continue;
            }

            yield return current;
            current = piece;
        }

        yield return current;
    }

    // The places in (0, 1) on the segment where the other one crosses it, or where an end of the other one lies on it.
    private static void Cuts(Segment segment, Segment other, SortedSet<Rational> cuts)
    {
        if (segment.Box.MaxX < other.Box.MinX || other.Box.MaxX < segment.Box.MinX
            || segment.Box.MaxY < other.Box.MinY || other.Box.MaxY < segment.Box.MinY)
            return;
        var d1 = segment.B - segment.A;
        var d2 = other.B - other.A;
        var w = other.A - segment.A;
        var denominator = Point.Cross(d1, d2);
        if (!denominator.IsZero)
        {
            var t = Point.Cross(w, d2) / denominator;
            var u = Point.Cross(w, d1) / denominator;
            if (t.Sign > 0 && t < Rational.One && u.Sign >= 0 && u <= Rational.One)
                cuts.Add(t);
            return;
        }

        // Parallel: an end of the other one on the segment cuts it when the two lie on one line.
        if (!Point.Cross(w, d1).IsZero)
            return;
        var length = Point.Dot(d1, d1);
        foreach (var end in new[] { other.A, other.B })
        {
            var t = Point.Dot(end - segment.A, d1) / length;
            if (t.Sign > 0 && t < Rational.One)
                cuts.Add(t);
        }
    }

    private static (Rational, Rational, Rational, Rational) Key(Point from, Point to)
        => (from.X, from.Y).CompareTo((to.X, to.Y)) <= 0 ? (from.X, from.Y, to.X, to.Y) : (to.X, to.Y, from.X, from.Y);

    // A point on the plane of the view.
    private readonly record struct Point(Rational X, Rational Y)
    {
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);

        public static Rational Cross(Point a, Point b) => (a.X * b.Y) - (a.Y * b.X);

        public static Rational Dot(Point a, Point b) => (a.X * b.X) + (a.Y * b.Y);
    }

    // The rounded box of a set of points, for the filter before an exact test.
    private readonly record struct Box(double MinX, double MinY, double MaxX, double MaxY)
    {
        public static Box Of(IEnumerable<Point> points)
        {
            var (minX, minY, maxX, maxY) = (double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);
            foreach (var p in points)
            {
                var (x, y) = (p.X.ToDouble(), p.Y.ToDouble());
                (minX, minY, maxX, maxY) = (Math.Min(minX, x), Math.Min(minY, y), Math.Max(maxX, x), Math.Max(maxY, y));
            }

            return new Box(minX, minY, maxX, maxY);
        }

        public bool Outside(double x, double y) => x < MinX || x > MaxX || y < MinY || y > MaxY;
    }

    // An edge on the plane of the view, with its ends in the world, its kind or null when it is not drawn, and its tags.
    private sealed record Segment(Guid Body, Point A, Point B, Vector3R Start, Vector3R End, ViewLineKind? Kind, FaceTag First, FaceTag Second)
    {
        public Box Box { get; } = Box.Of([A, B]);

        public Point At(Rational t) => new(A.X + ((B.X - A.X) * t), A.Y + ((B.Y - A.Y) * t));
    }

    private sealed record Piece(int Segment, Rational T0, Rational T1, Point From, Point To, Rational Depth, bool Visible);

    // A face that looks toward the eye: its plane, and its loops on the plane of the view, the outer loop first.
    private sealed class Front(PlaneR plane, Point[][] loops)
    {
        private readonly Box _box = Box.Of(loops[0]);

        // True when the point of the view is inside the face or on its border, and the face is strictly nearer to the eye
        // than the point of the world p there. A point behind a seam between two facets of a curved side is on the border
        // of each of them and behind both, so the border counts; an edge of the face itself is at the same depth, so it
        // is not covered. The line p + λ e meets the plane n · x = d at λ = (d - n · p) / (n · e), and
        // n · e > 0, so the face is nearer when n · p < d.
        public bool Covers(Point at, Vector3R p)
        {
            if (_box.Outside(at.X.ToDouble(), at.Y.ToDouble()))
                return false;
            if (Where(loops[0], at) < 0)
                return false;
            for (var h = 1; h < loops.Length; h++)
            {
                if (Where(loops[h], at) > 0)
                    return false;
            }

            return plane.Normal.Dot(p) < plane.Offset;
        }
    }

    // 1 when the point is strictly inside the closed polygon, 0 when it is on its border, -1 when it is outside, by the
    // count of crossings of a ray toward positive x, with each sign exact.
    private static int Where(Point[] ring, Point p)
    {
        var inside = false;
        for (var i = 0; i < ring.Length; i++)
        {
            var (a, b) = (ring[i], ring[(i + 1) % ring.Length]);
            if (Point.Cross(b - a, p - a).IsZero
                && Point.Dot(p - a, p - b).Sign <= 0)
                return 0;
            if ((a.Y > p.Y) != (b.Y > p.Y))
            {
                var x = a.X + ((p.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y));
                if (p.X < x)
                    inside = !inside;
            }
        }

        return inside ? 1 : -1;
    }
}
