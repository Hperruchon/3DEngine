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

    // ADR-0164 item 1 (TASK-0400, findings E211 and E212 of the codebase review of 2026-11-17): the length L of Right and of
    // Up. It is exact when Right · Right is the square of a rational number, as for each frame of whole numbers; else it is
    // the double of sqrt(Right · Right). The square never becomes a double: the numerator and the denominator move by an
    // even power of two to a quotient near 1, so a length of 1e-200 or 1e200 does not underflow or overflow.
    public (Rational? Exact, double Value) Length
    {
        get
        {
            var square = Right.Dot(Right);
            var (n, d) = (square.Numerator, square.Denominator);
            var (rootN, rootD) = (Root(n), Root(d));
            if (rootN * rootN == n && rootD * rootD == d)
            {
                var exact = new Rational(rootN, rootD);
                return (exact, exact.ToDouble());
            }

            var shift = (int)((n.GetBitLength() - d.GetBitLength()) / 2);
            var near = shift >= 0 ? new Rational(n, d << (2 * shift)) : new Rational(n << (-2 * shift), d);
            return (null, Math.ScaleB(Math.Sqrt(near.ToDouble()), shift));
        }
    }

    // ADR-0164 item 1: the unit direction toward the eye in doubles, from TowardEye divided first by the largest absolute
    // value of its coordinates, an exact step, so a frame of any length gives a finite direction.
    public (double X, double Y, double Z) Eye
    {
        get
        {
            var largest = new[] { TowardEye.X, TowardEye.Y, TowardEye.Z }.Select(value => value.Sign < 0 ? Rational.Zero - value : value).Max()!;
            var (x, y, z) = ((TowardEye.X / largest).ToDouble(), (TowardEye.Y / largest).ToDouble(), (TowardEye.Z / largest).ToDouble());
            var length = Math.Sqrt((x * x) + (y * y) + (z * z));
            return (x / length, y / length, z / length);
        }
    }

    // The integer square root of a value that is not negative, by Newton's method on whole numbers.
    private static System.Numerics.BigInteger Root(System.Numerics.BigInteger value)
    {
        if (value.Sign <= 0)
            return System.Numerics.BigInteger.Zero;
        var x = System.Numerics.BigInteger.One << (int)((value.GetBitLength() + 1) / 2);
        while (true)
        {
            var y = (x + (value / x)) >> 1;
            if (y >= x)
                return x;
            x = y;
        }
    }
}

// ADR-0142 item 3: an edge between faces of two tags, or the outline of a curved side: an edge between two faces of one
// tag where one face looks toward the eye and the other does not.
internal enum ViewLineKind
{
    Edge,
    Outline,

    // ADR-0165 item 3 (phase R30g, TASK-0405): a centre line, a cross on a whole circle or the axis of a curved side.
    Centre,
}

// ADR-0142 items 3 to 5: one line of a view, on the plane of the view, visible or hidden, with the body and the tags of the
// two faces of its edge, so a later phase finds the values of the design that the line shows. ADR-0143 item 2: a line with
// a circle is the arc of that circle from From to To, counter-clockwise on the view; From equal to To is the whole circle.
// ADR-0145 item 1: World holds the exact ends of the line in the world, so the circles know where a line is along an axis.
internal sealed record ViewLine(
    Guid Body, (Rational X, Rational Y) From, (Rational X, Rational Y) To, bool Visible, ViewLineKind Kind, FaceTag First, FaceTag Second,
    ViewCircle? Circle = null, ViewEllipse? Ellipse = null, (Vector3R From, Vector3R To)? World = null);

// ADR-0143 item 2: a circle on the plane of the view, with the centre and the radius of the design.
internal sealed record ViewCircle((Rational X, Rational Y) Centre, Rational Radius);

// ADR-0144 item 1: an ellipse on the plane of the view: its centre, and its two half axes at a right angle, Major with the
// length of the radius of the design and Minor. A line with an ellipse is the arc of it from From to To, counter-clockwise
// on the view; From equal to To is the whole ellipse.
internal sealed record ViewEllipse((Rational X, Rational Y) Centre, (double X, double Y) Major, (double X, double Y) Minor);

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
        var (lines, skipped, _) = WithCurves(document, frame);
        return (lines, skipped);
    }

    // ADR-0165 item 3 (phase R30g, TASK-0405): the lines and the skipped bodies of Of, with the curves of the design, so a read
    // can add the centre lines of the view.
    public static (IReadOnlyList<ViewLine> Lines, IReadOnlyList<(Guid Body, string Reason)> Skipped, IReadOnlyDictionary<FaceTag, SideCurve> Curves) WithCurves(Document document, ViewFrame frame)
    {
        var (lines, skipped, curves, _) = Section(document, frame, null);
        return (lines, skipped, curves);
    }

    // ADR-0169 item 3 (phase R30h, TASK-0411): the lines of WithCurves, with each solid cut by the plane through the point whose
    // normal is the direction toward the eye when a cut is given, and the loops of the cut faces of each body on the view.
    public static (IReadOnlyList<ViewLine> Lines, IReadOnlyList<(Guid Body, string Reason)> Skipped, IReadOnlyDictionary<FaceTag, SideCurve> Curves,
        IReadOnlyList<(Guid Body, IReadOnlyList<IReadOnlyList<(Rational X, Rational Y)>> Loops)> CutFaces) Section(Document document, ViewFrame frame, (Vector3R Point, Guid View)? cut)
    {
        ArgumentNullException.ThrowIfNull(document);
        var cutFaces = new List<(Guid Body, IReadOnlyList<IReadOnlyList<(Rational X, Rational Y)>> Loops)>();
        var bodies = new List<(Guid Body, Brep Brep)>();
        var skipped = new List<(Guid Body, string Reason)>();
        var curves = new Dictionary<FaceTag, SideCurve>();
        // Finding E226 of the codebase review of 2026-11-20 (TASK-0426): the bodies that this read cuts, so the read keeps their
        // cuts and no other cut of the view.
        var cutBodies = new List<Guid>();
        var plainBodies = new List<Guid>();
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
                Brep brep;
                if (cut is { } plane)
                {
                    // Finding E222 (TASK-0416): the cut and its B-Rep come from the cuts that the read keeps.
                    cutBodies.Add(record.Handle.Id);
                    if (SectionCut.Cut(solid, plane.Point, frame, plane.View, record.Handle.Id) is not { } kept)
                    {
                        skipped.Add((record.Handle.Id, "The cut plane of the section leaves no material of the body (ADR-0169 item 3)."));
                        continue;
                    }

                    brep = kept.Brep;
                }
                else
                {
                    // TASK-0446 (register entry R-0069): the B-Rep comes from the B-Reps that the reads keep, so the views of
                    // one read and the later reads build it once.
                    plainBodies.Add(record.Handle.Id);
                    brep = ViewBreps.Of(document.DocumentId, record.Handle.Id, solid);
                }

                bodies.Add((record.Handle.Id, brep));
                if (cut is { } faces)
                {
                    var tag = SectionCut.Tag(faces.View);
                    var loops = brep.Faces.Where(face => face.Tag == tag).SelectMany(face => face.Loops)
                        .Select(loop => (IReadOnlyList<(Rational X, Rational Y)>)[.. loop.Vertices.Select(v => (brep.Vertices[v].Dot(frame.Right), brep.Vertices[v].Dot(frame.Up)))])
                        .ToList();
                    if (loops.Count > 0)
                        cutFaces.Add((record.Handle.Id, loops));
                }

                foreach (var (tag, curve) in KernelProjection.Curves(document, record.Handle.Id))
                    curves.TryAdd(tag, curve);
            }
            catch (InvalidOperationException ex)
            {
                skipped.Add((record.Handle.Id, ex.Message));
            }
        }

        if (cut is { } section)
            SectionCut.Keep(section.View, cutBodies);
        else
            ViewBreps.Keep(document.DocumentId, plainBodies);
        return (Of(bodies, frame, curves), skipped, curves, cutFaces);
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
                // TASK-0446 (register entry R-0069): the rounded middle comes once for each piece, not once for each face.
                var (x, y) = (at.X.ToDouble(), at.Y.ToDouble());
                var hidden = fronts.Any(front => front.Covers(at, x, y, point));
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
            var along = segment.End - segment.Start;
            lines.Add(new ViewLine(segment.Body, (start.From.X, start.From.Y), (end.To.X, end.To.Y), start.Visible, segment.Kind!.Value, segment.First, segment.Second,
                World: (segment.Start + (along * start.T0), segment.Start + (along * end.T1))));
        }

        return curves is null || curves.Count == 0 ? lines : Curved(lines, frame, curves);
    }

    // ADR-0143 items 2 and 3: a piece of an end of a cylinder or a cone whose axis is along the view, and a piece of the
    // outline of a sphere, is an arc of the circle of the design; the arcs of one circle with one visibility that follow each
    // other are one arc, or the whole circle. The pieces of an end whose axis is across the view lie on one line, and the
    // pieces that follow each other with one visibility are one line. Each other line stays. ADR-0164 item 1 (TASK-0400,
    // finding E211 of the codebase review of 2026-11-17): a frame of length L projects a length of the design to L times it,
    // so the radius of a circle on the view is L times the radius of the design. Until TASK-0400 a frame of another length
    // than 1 kept the facets, and each exact view at a slant has such a frame.
    private static List<ViewLine> Curved(List<ViewLine> lines, ViewFrame frame, IReadOnlyDictionary<FaceTag, SideCurve> curves)
    {
        var length = frame.Length;
        Rational OnView(Rational radius) => length.Exact is { } exact ? radius * exact : Rational.FromDouble(radius.ToDouble() * length.Value);
        var groups = new Dictionary<object, (int First, List<ViewLine> Members, bool Arc)>();
        var output = new List<(int Order, ViewLine Line)>();
        var outline = new List<(int Order, ViewLine Line, int Which, ((double X, double Y) Start, (double X, double Y) End) Segment, SideCurve Side)>();
        var across = new List<(int Order, ViewLine Line, object Key, double A, double B, (double X, double Y) Centre, (double X, double Y) Major)>();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            object? key = null;
            switch (CurveOf(line, curves))
            {
                case { Kind: SideCurveKind.Sphere } sphere when line.Kind == ViewLineKind.Outline:
                    line = Arc(line, new ViewCircle(Project(sphere.Base, frame), OnView(sphere.Radius)));
                    break;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } side when line.Kind == ViewLineKind.Outline && AlongView(side, frame):
                    // ADR-0145 item 2: a side whose axis is along the view has no outline; an outline of its facets there is
                    // a line of no length within the rounding of a turn.
                    continue;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } end when line.Kind == ViewLineKind.Edge && RadiusAt(end, line) is { } radius && AlongView(end, frame):
                    line = Arc(line, new ViewCircle(Project(end.Base, frame), OnView(radius)));
                    break;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } end when line.Kind == ViewLineKind.Edge && Level(end, line) is not null && end.Axis.Dot(frame.TowardEye).IsZero:
                    key = (line.Body, line.Visible, line.Kind, Tags(line), Support(line));
                    break;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } end when line.Kind == ViewLineKind.Edge && Level(end, line) is { } level
                    && RadiusAt(end, line) is not null && AcrossView(end, frame):
                    across.Add(OnDiameter(i, line, end, frame, level));
                    continue;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } end when line.Kind == ViewLineKind.Edge && RadiusAt(end, line) is not null && Level(end, line) is { } h:
                    line = Arc(line, EndEllipse(end, frame, h));
                    break;
                case { Kind: SideCurveKind.Cylinder or SideCurveKind.Cone } side when line.Kind == ViewLineKind.Outline && line.World is not null
                    && Outlines(side, frame) is { } outlines:
                    var which = Nearer(outlines, line.From, line.To);
                    outline.Add((i, line, which, outlines[which], side));
                    continue;
            }

            if (line.Circle is { } circle)
                key = (line.Body, line.Visible, line.Kind, Tags(line), circle);
            if (line.Ellipse is { } ellipse)
                key = (line.Body, line.Visible, line.Kind, Tags(line), ellipse);
            if (key is null)
            {
                output.Add((i, line));
                continue;
            }

            if (!groups.TryGetValue(key, out var group))
                groups[key] = group = (i, [], line.Circle is not null || line.Ellipse is not null);
            group.Members.Add(line);
        }

        foreach (var (first, members, arc) in groups.Values)
            output.AddRange((arc ? Chains(members) : Runs(members)).Select(line => (first, line)));
        foreach (var entry in outline)
            output.Add((entry.Order, OnOutline(entry.Line, entry.Segment, entry.Side)));
        foreach (var end in across.GroupBy(entry => entry.Key))
            output.AddRange(Diameter([.. end]));

        // ADR-0145 item 2: a whole visible circle is drawn one time, and each other arc of that circle lies under it, as a
        // hidden line under a line (ADR-0142 item 5): the far end of a side along the view within the rounding of a turn.
        var whole = output.Select(entry => entry.Line).Where(line => line.Visible && line.Circle is not null && line.From == line.To)
            .GroupBy(line => line.Circle!).ToDictionary(group => group.Key, group => group.First());
        output.RemoveAll(entry => entry.Line.Circle is { } circle && whole.TryGetValue(circle, out var kept) && !ReferenceEquals(kept, entry.Line));
        return [.. output.OrderBy(entry => entry.Order).Select(entry => entry.Line)];
    }

    // ADR-0145 item 1: the exact place of a point along the axis of a side: 0 at its base and 1 at its other end or apex.
    private static Rational Along(SideCurve side, Vector3R p) => (p - side.Base).Dot(side.Axis) / side.Axis.Dot(side.Axis);

    // ADR-0145 item 1: the place of a line along the axis when its two ends have one place, else null.
    private static Rational? Level(SideCurve side, ViewLine line)
    {
        if (line.World is not var (from, to) || side.Axis.IsZero)
            return null;
        var (a, b) = (Along(side, from), Along(side, to));
        return a == b ? a : null;
    }

    // ADR-0145 item 1: the radius of the circle of the side that holds the line at its place h: R for a cylinder and R (1 - h)
    // for a cone, or null for a line with no one place or at the apex, which keeps its facets. Finding E204 of the codebase
    // review of 2026-11-14 (e) (TASK-0379): until TASK-0379 each such line went onto an end, so the cut of a cone at its middle
    // was a circle of the radius of its base.
    private static Rational? RadiusAt(SideCurve side, ViewLine line)
    {
        if (Level(side, line) is not { } h)
            return null;
        var radius = side.Kind == SideCurveKind.Cone ? side.Radius * (Rational.One - h) : side.Radius;
        return radius.Sign > 0 ? radius : null;
    }

    // ADR-0147 item 1 (finding E205 of the codebase review of 2026-11-14 (e), TASK-0380): an end is across the view within the
    // rounding of a turn when the cosine of the angle between the axis and the view is at most 1e-9 and not exactly 0. Until
    // TASK-0380 such an end was a flat ellipse, and its hidden half lay under its visible half.
    private static bool AcrossView(SideCurve side, ViewFrame frame)
    {
        var (a, e) = (side.Axis.ToDoubles(), frame.Eye);
        var part = Dot(a, e);
        return part * part <= 1e-18 * Dot(a, a) * Dot(e, e);
    }

    // ADR-0147 item 1: a piece of such an end goes onto the diameter of its circle on the view, the line from C - Major to C +
    // Major at its place h along the axis, by the place t of each end along Major, from -1 to 1.
    private static (int Order, ViewLine Line, object Key, double A, double B, (double X, double Y) Centre, (double X, double Y) Major) OnDiameter(
        int order, ViewLine line, SideCurve end, ViewFrame frame, Rational h)
    {
        var (major, _) = HalfAxes(end, frame);
        major = Scale(major, end.Kind == SideCurveKind.Cone ? 1 - h.ToDouble() : 1);
        var c = D(Project(end.Base + (end.Axis * h), frame));
        var mm = Dot2(major, major);
        double Place((Rational X, Rational Y) q) => (((q.X.ToDouble() - c.X) * major.X) + ((q.Y.ToDouble() - c.Y) * major.Y)) / mm;
        var (from, to) = (Place(line.From), Place(line.To));
        return (order, line, (line.Body, Tags(line), h), Math.Min(from, to), Math.Max(from, to), c, major);
    }

    // ADR-0147 item 1: the pieces of one end as intervals on its diameter. Both halves of a flat end lie on the diameter, so
    // the union of the visible pieces is the visible line, and each part of a hidden piece outside that union, within 1e-9
    // of the radius, is a hidden line; a hidden line under a line is not drawn (ADR-0142 item 5).
    private static IEnumerable<(int Order, ViewLine Line)> Diameter(
        List<(int Order, ViewLine Line, object Key, double A, double B, (double X, double Y) Centre, (double X, double Y) Major)> pieces)
    {
        var (centre, major) = (pieces[0].Centre, pieces[0].Major);
        var limit = 1e-9;
        (Rational, Rational) At(double s) => (Rational.FromDouble(centre.X + (s * major.X)), Rational.FromDouble(centre.Y + (s * major.Y)));
        static List<(double A, double B)> Union(IEnumerable<(double A, double B)> ranges, double limit)
        {
            var union = new List<(double A, double B)>();
            foreach (var (a, b) in ranges.OrderBy(range => range.A))
            {
                if (union.Count > 0 && a <= union[^1].B + limit)
                    union[^1] = (union[^1].A, Math.Max(union[^1].B, b));
                else
                    union.Add((a, b));
            }

            return union;
        }

        var order = pieces.Min(piece => piece.Order);
        var visible = Union(pieces.Where(piece => piece.Line.Visible).Select(piece => (piece.A, piece.B)), limit);
        var shown = pieces.FirstOrDefault(piece => piece.Line.Visible).Line;
        foreach (var (a, b) in visible)
            yield return (order, shown! with { From = At(a), To = At(b), World = null });

        var hidden = pieces.FirstOrDefault(piece => !piece.Line.Visible).Line;
        foreach (var (a, b) in Union(pieces.Where(piece => !piece.Line.Visible).Select(piece => (piece.A, piece.B)), limit))
        {
            var start = a;
            foreach (var (c, d) in visible.Where(range => range.B > a + limit && range.A < b - limit))
            {
                if (c > start + limit)
                    yield return (order, hidden! with { From = At(start), To = At(c), World = null });
                start = Math.Max(start, d);
            }

            if (b > start + limit)
                yield return (order, hidden! with { From = At(start), To = At(b), World = null });
        }
    }

    // ADR-0145 item 2: the axis is along the view when the sine of the angle between them is at most 1e-9: exactly, or within
    // the rounding of a turn from a quaternion in doubles.
    private static bool AlongView(SideCurve side, ViewFrame frame)
    {
        if (side.Axis.Cross(frame.TowardEye).IsZero)
            return true;
        var (a, e) = (side.Axis.ToDoubles(), frame.Eye);
        var c = Cross(a, e);
        return Dot(c, c) <= 1e-18 * Dot(a, a) * Dot(e, e);
    }

    // ADR-0144 item 1: the ellipse of an end of a cylinder or a cone at a slant to the view. With a the unit axis and e the
    // unit direction toward the eye, P = a x e over its length lies on the plane of the end and across the view, and Q = a x
    // P lies on that plane too; the end is C + R cos t P + R sin t Q, so on the view Major = R P and Minor = R Q, at a
    // right angle. ADR-0145 item 1: the ellipse of a piece is at its exact place h along the axis, and a cone scales its half
    // axes by 1 - h.
    private static ViewEllipse EndEllipse(SideCurve end, ViewFrame frame, Rational h)
    {
        var (major, minor) = HalfAxes(end, frame);
        var f = end.Kind == SideCurveKind.Cone ? 1 - h.ToDouble() : 1;
        return new(Project(end.Base + (end.Axis * h), frame), Scale(major, f), Scale(minor, f));
    }

    // The two half axes of the ends of a side on the view, in doubles with + - * / and the square root only.
    private static ((double X, double Y) Major, (double X, double Y) Minor) HalfAxes(SideCurve side, ViewFrame frame)
    {
        var a = Unit(side.Axis.ToDoubles());
        var e = frame.Eye;
        var p = Unit(Cross(a, e));
        var q = Cross(a, p);
        var (r, u, radius) = (frame.Right.ToDoubles(), frame.Up.ToDoubles(), side.Radius.ToDouble());
        return ((radius * Dot(p, r), radius * Dot(p, u)), (radius * Dot(q, r), radius * Dot(q, u)));
    }

    // ADR-0144 item 2: the two lines of the outline of a side at a slant to the view, each from its start to its end. A
    // cylinder has the lines from C0 + s Major to C1 + s Major, for s = 1 and -1. A cone has the lines from its apex to the
    // two points where a tangent from the apex meets the ellipse of its base: in the coordinates (alpha, beta) of the
    // ellipse the apex is q with |q|² = ρ² > 1, and the points are (q ± sqrt(ρ² - 1) q⊥) / ρ². Null when the apex is inside the
    // ellipse, so the cone has no outline.
    private static ((double X, double Y) Start, (double X, double Y) End)[]? Outlines(SideCurve side, ViewFrame frame)
    {
        var (major, minor) = HalfAxes(side, frame);
        var c0 = D(Project(side.Base, frame));
        var c1 = D(Project(side.Base + side.Axis, frame));
        if (side.Kind == SideCurveKind.Cylinder)
            return [(Add(c0, major), Add(c1, major)), (Add(c0, Scale(major, -1)), Add(c1, Scale(major, -1)))];

        var (mm, nn) = (Dot2(major, major), Dot2(minor, minor));
        var d = (c1.X - c0.X, c1.Y - c0.Y);
        if (nn == 0)
            return [(c1, Add(c0, major)), (c1, Add(c0, Scale(major, -1)))];
        var q = (Dot2(d, major) / mm, Dot2(d, minor) / nn);
        var rho = Dot2(q, q);
        if (rho <= 1)
            return null;
        var s = Math.Sqrt(rho - 1);
        (double X, double Y) Point(double sign)
        {
            var (tx, ty) = ((q.Item1 - (sign * s * q.Item2)) / rho, (q.Item2 + (sign * s * q.Item1)) / rho);
            return Add(c0, Add(Scale(major, tx), Scale(minor, ty)));
        }

        return [(c1, Point(1)), (c1, Point(-1))];
    }

    // The index of the line of the outline nearer to the middle of the piece.
    private static int Nearer(((double X, double Y) Start, (double X, double Y) End)[] outlines, (Rational X, Rational Y) from, (Rational X, Rational Y) to)
    {
        var middle = ((from.X.ToDouble() + to.X.ToDouble()) / 2, (from.Y.ToDouble() + to.Y.ToDouble()) / 2);
        double Distance(((double X, double Y) Start, (double X, double Y) End) line)
        {
            var d = (line.End.X - line.Start.X, line.End.Y - line.Start.Y);
            var w = (middle.Item1 - line.Start.X, middle.Item2 - line.Start.Y);
            var cross = (w.Item1 * d.Item2) - (w.Item2 * d.Item1);
            return cross * cross / Dot2(d, d);
        }

        return Distance(outlines[0]) <= Distance(outlines[1]) ? 0 : 1;
    }

    // ADR-0144 item 2, amended by ADR-0145 item 1: a piece of the outline of the facets of a side goes onto the line of the
    // outline of the design at the exact place of each of its ends along the axis: from C0 at 0 to C1 at 1 for a cylinder,
    // and from the base at 0 to the apex at 1 for a cone. So the outline of a side that a Subtract cut ends where the side
    // ends, and no length on the view divides a value.
    private static ViewLine OnOutline(ViewLine line, ((double X, double Y) Start, (double X, double Y) End) segment, SideCurve side)
    {
        var (from, to) = line.World!.Value;
        var (start, end) = segment;
        (double X, double Y) At(Vector3R p)
        {
            var h = Along(side, p).ToDouble();
            var f = side.Kind == SideCurveKind.Cylinder ? h : 1 - h;
            return (start.X + (f * (end.X - start.X)), start.Y + (f * (end.Y - start.Y)));
        }

        var (a, b) = (At(from), At(to));
        return line with { From = (Rational.FromDouble(a.X), Rational.FromDouble(a.Y)), To = (Rational.FromDouble(b.X), Rational.FromDouble(b.Y)) };
    }

    private static (double X, double Y, double Z) Cross((double X, double Y, double Z) a, (double X, double Y, double Z) b)
        => ((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    private static double Dot((double X, double Y, double Z) a, (double X, double Y, double Z) b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    private static (double X, double Y, double Z) Unit((double X, double Y, double Z) a)
    {
        var length = Math.Sqrt(Dot(a, a));
        return (a.X / length, a.Y / length, a.Z / length);
    }

    private static double Dot2((double X, double Y) a, (double X, double Y) b) => (a.X * b.X) + (a.Y * b.Y);

    private static (double X, double Y) Add((double X, double Y) a, (double X, double Y) b) => (a.X + b.X, a.Y + b.Y);

    private static (double X, double Y) Scale((double X, double Y) a, double s) => (a.X * s, a.Y * s);

    private static (double X, double Y) D((Rational X, Rational Y) p) => (p.X.ToDouble(), p.Y.ToDouble());

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
        => Turn(line, circle.Centre) switch
        {
            > 0 => line with { Circle = circle },
            < 0 => Reversed(line) with { Circle = circle },
            _ => line,
        };

    // ADR-0144 item 1: the piece as an arc of the ellipse, as for a circle: an ellipse goes once about its centre.
    private static ViewLine Arc(ViewLine line, ViewEllipse ellipse)
        => Turn(line, ellipse.Centre) switch
        {
            > 0 => line with { Ellipse = ellipse },
            < 0 => Reversed(line) with { Ellipse = ellipse },
            _ => line,
        };

    private static ViewLine Reversed(ViewLine line)
        => line with { From = line.To, To = line.From, World = line.World is var (a, b) ? (b, a) : null };

    // The line from the start of the first to the end of the last, with its ends in the world.
    private static ViewLine Joined(ViewLine first, ViewLine last)
        => first with { To = last.To, World = first.World is var (a, _) && last.World is var (_, b) ? (a, b) : null };

    private static int Turn(ViewLine line, (Rational X, Rational Y) c)
        => (((line.From.X - c.X) * (line.To.Y - c.Y)) - ((line.From.Y - c.Y) * (line.To.X - c.X))).Sign;

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

            yield return Joined(first, last);
        }
    }

    // The pieces on one line, in their order along it, joined where one ends and the next one starts.
    private static IEnumerable<ViewLine> Runs(List<ViewLine> pieces)
    {
        var (dx, dy) = (pieces[0].To.X - pieces[0].From.X, pieces[0].To.Y - pieces[0].From.Y);
        Rational Along((Rational X, Rational Y) p) => (p.X * dx) + (p.Y * dy);
        var ordered = pieces.Select(piece => Along(piece.From) <= Along(piece.To) ? piece : Reversed(piece))
            .OrderBy(piece => Along(piece.From)).ToList();
        var current = ordered[0];
        foreach (var piece in ordered.Skip(1))
        {
            if (piece.From == current.To)
            {
                current = Joined(current, piece);
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
        // n · e > 0, so the face is nearer when n · p < d. The caller gives x and y, the rounded values of the point.
        public bool Covers(Point at, double x, double y, Vector3R p)
        {
            if (_box.Outside(x, y))
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

    // ADR-0165 item 3 (phase R30g, TASK-0405): the centre lines of a view, before the place on the paper. A whole circle gives a
    // cross: its diameter along the right and along the up of the view. A cylinder or a cone with lines of its outline on the
    // view gives its axis, from the lowest to the highest place of those lines along the axis, so the axis of a hole ends
    // where the hole ends. Each centre line has the kind Centre, is visible, and has the tags of its curve; the place on the
    // paper adds 3 mm at each end. The order is the order of the lines, and the code uses + − × ÷ on exact numbers only.
    // Finding E218 of the codebase review of 2026-11-18 (TASK-0410): a circle gives its cross when its arcs, visible and hidden,
    // cover the whole turn (Whole), so a hole that another body covers in part keeps its cross. Until TASK-0410 only one line
    // that was the whole circle gave a cross.
    public static IReadOnlyList<ViewLine> Centres(IReadOnlyList<ViewLine> lines, ViewFrame frame, IReadOnlyDictionary<FaceTag, SideCurve> curves)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(curves);
        var centres = new List<ViewLine>();
        var whole = Whole(lines);
        var circles = new HashSet<ViewCircle>();
        var axes = new List<(Guid Body, FaceTag Tag, SideCurve Side, Rational Low, Rational High)>();
        foreach (var line in lines)
        {
            if (line.Circle is { } circle && whole.Contains(circle) && circles.Add(circle))
            {
                var ((x, y), r) = (circle.Centre, circle.Radius);
                centres.Add(new ViewLine(line.Body, (x - r, y), (x + r, y), true, ViewLineKind.Centre, line.First, line.Second));
                centres.Add(new ViewLine(line.Body, (x, y - r), (x, y + r), true, ViewLineKind.Centre, line.First, line.Second));
            }

            if (line.World is not var (from, to) || AxisSide(line, curves) is not var (tag, side))
                continue;
            var (a, b) = (Along(side, from), Along(side, to));
            var (low, high) = a <= b ? (a, b) : (b, a);
            var index = axes.FindIndex(axis => axis.Tag == tag);
            if (index < 0)
                axes.Add((line.Body, tag, side, low, high));
            else
                axes[index] = axes[index] with { Low = low < axes[index].Low ? low : axes[index].Low, High = high > axes[index].High ? high : axes[index].High };
        }

        foreach (var (body, tag, side, low, high) in axes)
        {
            if (low < high)
                centres.Add(new ViewLine(body, Project(side.Base + (side.Axis * low), frame), Project(side.Base + (side.Axis * high), frame), true, ViewLineKind.Centre, tag, tag));
        }

        return centres;
    }

    // Finding E218 (TASK-0410): the circles whose arcs cover the whole turn. Each arc goes counter-clockwise from From to To
    // (Arc), and a start equal to the end is the whole circle. A circle is whole when a line of it is the whole circle, or when
    // the arcs that follow each other from the end of one arc come back to its start: the arc and that path then go once about
    // the centre or more. The test compares exact points only.
    private static HashSet<ViewCircle> Whole(IReadOnlyList<ViewLine> lines)
    {
        var whole = new HashSet<ViewCircle>();
        foreach (var group in lines.Where(line => line.Circle is not null).GroupBy(line => line.Circle!))
        {
            var arcs = group.ToList();
            if (arcs.Any(arc => arc.From == arc.To) || arcs.Any(arc => Reaches(arcs, arc.To, arc.From)))
                whole.Add(group.Key);
        }

        return whole;
    }

    // True when a path of arcs, each from the end of the one before, goes from the point start to the point goal.
    private static bool Reaches(List<ViewLine> arcs, (Rational X, Rational Y) start, (Rational X, Rational Y) goal)
    {
        var seen = new HashSet<(Rational X, Rational Y)> { start };
        var next = new Queue<(Rational X, Rational Y)>([start]);
        while (next.TryDequeue(out var at))
        {
            if (at == goal)
                return true;
            foreach (var arc in arcs)
            {
                if (arc.From == at && seen.Add(arc.To))
                    next.Enqueue(arc.To);
            }
        }

        return false;
    }

    // ADR-0165 item 3 and ADR-0169 item 3 (phase R30h, TASK-0411): the tag and the curve of a cylinder or a cone that a line shows
    // from the side: a line of its outline, or an edge between its side and a face of another tag along its axis, as the edge
    // of a cut face. Until TASK-0411 only an outline gave the axis, so a section through the axis of a hole, whose cut goes
    // through the corners of the facets, had no axis. Else null.
    //
    // Finding E223 of the codebase review of 2026-11-18 (b) (TASK-0416, Q1 A of the owner): the edge lies in one plane with the
    // axis, (to − from) × axis · (from − base) = 0, so the slanted edge of a cut through the axis of a cone gives the axis in
    // each look. Until TASK-0416 the edge had to be parallel to the axis, which only a cylinder gives.
    private static (FaceTag Tag, SideCurve Side)? AxisSide(ViewLine line, IReadOnlyDictionary<FaceTag, SideCurve> curves)
    {
        if (line.World is not var (from, to))
            return null;
        var (first, second) = (curves.TryGetValue(line.First, out var a) ? a : null, curves.TryGetValue(line.Second, out var b) ? b : null);
        (FaceTag Tag, SideCurve Side)? found = line.Kind switch
        {
            ViewLineKind.Outline when first is not null => (line.First, first),
            ViewLineKind.Edge when first is not null && second is null => (line.First, first),
            ViewLineKind.Edge when first is null && second is not null => (line.Second, second),
            _ => null,
        };
        if (found is not var (_, side) || side.Kind == SideCurveKind.Sphere || side.Axis.IsZero)
            return null;
        return line.Kind == ViewLineKind.Edge && !(to - from).Cross(side.Axis).Dot(from - side.Base).IsZero ? null : found;
    }
}
