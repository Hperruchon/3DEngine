using Engine.Contracts.Drawing;
using Engine.Core.Queries;
using Engine.Kernel;

namespace Engine.Core.Drawing;

// ADR-0169 (phase R30h, TASK-0411): the frame of a section from its parent, the cut of a solid by the plane of a section, and
// the hatch of the cut faces on the paper. The cut and the hatch are presentation (ADR-0169 items 3 and 4): a read computes
// them, and no command, event or Document holds them.
internal static class SectionCut
{
    // ADR-0169 item 2: the four directions of sight on the parent, the direction of the arrows.
    public static readonly IReadOnlyList<string> Looks = ["left", "right", "up", "down"];

    // ADR-0169 item 2: the letters that a section takes by default, from A to Z without I, O and Q.
    public const string DefaultLetters = "ABCDEFGHJKLMNPRSTUVWXYZ";

    // ADR-0169 item 4: the distance between two hatch lines, in millimetres of the paper (ISO 128-50).
    public const double HatchSpace = 2.5;

    // ADR-0169 item 2: the frame of a section from the frame of its parent and the direction of sight, or null when the look is
    // not one of the four or the length of the parent is not a rational number.
    public static ViewFrame? Frame(ViewFrame parent, string look)
    {
        if (parent.Length.Exact is not { } length)
            return null;
        var (r, u, inverse) = (parent.Right, parent.Up, Rational.One / length);
        ViewFrame? frame = look switch
        {
            "left" => new ViewFrame(u.Cross(r) * inverse, u, r),
            "right" => new ViewFrame(r.Cross(u) * inverse, u, -r),
            "down" => new ViewFrame(r, u.Cross(r) * inverse, u),
            "up" => new ViewFrame(r, r.Cross(u) * inverse, -u),
            _ => null,
        };
        return frame is { IsValid: true } ? frame : null;
    }

    // ADR-0169 item 2: the nine numbers of a frame, or null when a double does not hold one of them exactly.
    public static IReadOnlyList<double>? Direction(ViewFrame frame)
    {
        var values = new[] { frame.Right, frame.Up, frame.TowardEye }.SelectMany(v => new[] { v.X, v.Y, v.Z }).ToArray();
        var direction = new double[9];
        for (var i = 0; i < 9; i++)
        {
            direction[i] = values[i].ToDouble();
            if (!double.IsFinite(direction[i]) || Rational.FromDouble(direction[i]) != values[i])
                return null;
        }

        return direction;
    }

    // ADR-0169 item 2: the first default letter that no section of the sheet has, or null.
    public static string? FreeLetter(SheetRecord sheet)
    {
        var used = sheet.Views.Where(view => view.Section is not null).Select(view => view.Section!.Letter).ToHashSet(StringComparer.Ordinal);
        return DefaultLetters.Select(letter => letter.ToString()).FirstOrDefault(letter => !used.Contains(letter));
    }

    // ADR-0169 item 3: the tag of each face of the box of the cut of a view.
    public static FaceTag Tag(Guid view) => new(view, "cut");

    // ADR-0169 item 3: the solid minus the half space on the side of the eye of the plane through the point, or null when no
    // material stays. Each number is exact. Finding E222 (TASK-0445): the cut of the owned kernel by a plane (ADR-0057 item
    // 10) gives it, with the cap faces of the tag of the view. Until TASK-0445 the read subtracted a box that held the half
    // space; the cut gives the same faces of the B-Rep, the same lines and the same hatch, with no partition of the solid.
    public static KernelSolid? Apply(KernelSolid solid, Vector3R point, ViewFrame frame, Guid view)
    {
        ArgumentNullException.ThrowIfNull(solid);
        return PlanarBoolean.Cut(solid, point, frame.TowardEye, Tag(view));
    }

    // Finding E222 of the codebase review of 2026-11-18 (b) (TASK-0416): the cut of a solid of many facets and its B-Rep take
    // seconds (a torus of 4,096 polygons: 11.7 s and 10.9 s), and the sheet page reads the sheet after each command. The read
    // keeps the cuts with their B-Rep, under the exact content of the solid, the point, the frame and the view,
    // so a read after a command that changes no body makes no new cut. The cut is a function of that content only, so a kept
    // result is the result that a new cut gives, in each document of the process. A cut that leaves no material is kept as
    // null. A cut that throws is not kept.
    // Finding E226 of the codebase review of 2026-11-20 (TASK-0426): the read keeps one cut for each pair of a view and a body,
    // the last one, and Keep drops the pairs of a view that its last read did not use. The cuts of the last ViewLimit views stay.
    // Until TASK-0426 the read kept the last 16 cuts of each view and body together, so a read that needed more than 16 cuts
    // removed each kept cut before its next lookup, and missed each one.
    public const int ViewLimit = 64;

    private static readonly object Gate = new();
    private static readonly LinkedList<(Guid View, Dictionary<Guid, (CutKey Key, (KernelSolid Solid, Brep Brep)? Value)> Bodies)> Kept = new();
    private static readonly Dictionary<Guid, int> Made = [];

    // The count of the cuts that the read made for a view, for a test of the cache. The count goes back to 0 for each view
    // when it holds 1,024 views.
    internal static int CutsOf(Guid view)
    {
        lock (Gate)
            return Made.GetValueOrDefault(view);
    }

    // Finding E226 (TASK-0426): the count of the kept cuts of a view, for a test.
    internal static int KeptOf(Guid view)
    {
        lock (Gate)
            return Kept.FirstOrDefault(entry => entry.View == view).Bodies?.Count ?? 0;
    }

    // ADR-0169 item 3: the cut of Apply with its B-Rep, from the kept cut of the view and the body when it has the same content.
    public static (KernelSolid Solid, Brep Brep)? Cut(KernelSolid solid, Vector3R point, ViewFrame frame, Guid view, Guid body)
    {
        ArgumentNullException.ThrowIfNull(solid);
        var key = new CutKey(solid.Polygons, point, frame, view);
        lock (Gate)
        {
            if (Bodies(view).TryGetValue(body, out var kept) && kept.Key.Equals(key))
                return kept.Value;
        }

        (KernelSolid Solid, Brep Brep)? value = Apply(solid, point, frame, view) is { } cut ? (cut, Brep.Of(cut)) : null;
        lock (Gate)
        {
            if (Made.Count >= 1024 && !Made.ContainsKey(view))
                Made.Clear();
            Made[view] = Made.GetValueOrDefault(view) + 1;
            Bodies(view)[body] = (key, value);
        }

        return value;
    }

    // Finding E226 (TASK-0426): after a read of a section, the cuts of the bodies that the read did not cut or look up leave.
    public static void Keep(Guid view, IReadOnlyCollection<Guid> bodies)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        lock (Gate)
        {
            var kept = Bodies(view);
            foreach (var body in kept.Keys.Where(body => !bodies.Contains(body)).ToList())
                kept.Remove(body);
        }
    }

    // The kept cuts of a view, the view first in the list; the view of the oldest use leaves past ViewLimit views. The caller
    // holds the gate.
    private static Dictionary<Guid, (CutKey Key, (KernelSolid Solid, Brep Brep)? Value)> Bodies(Guid view)
    {
        for (var node = Kept.First; node is not null; node = node.Next)
        {
            if (node.Value.View != view)
                continue;
            Kept.Remove(node);
            Kept.AddFirst(node);
            return node.Value.Bodies;
        }

        var bodies = new Dictionary<Guid, (CutKey Key, (KernelSolid Solid, Brep Brep)? Value)>();
        Kept.AddFirst((view, bodies));
        while (Kept.Count > ViewLimit)
            Kept.RemoveLast();
        return bodies;
    }

    // The exact content of a cut: each polygon of the solid with its vertices, its plane and its tag, in order; the point; the
    // frame; and the view, whose identifier is the tag of the cut faces.
    private sealed class CutKey(IReadOnlyList<KernelPolygon> polygons, Vector3R point, ViewFrame frame, Guid view) : IEquatable<CutKey>
    {
        private readonly int _hash = Hash(polygons, point, frame, view);

        private static int Hash(IReadOnlyList<KernelPolygon> polygons, Vector3R point, ViewFrame frame, Guid view)
        {
            var hash = new HashCode();
            hash.Add(point);
            hash.Add(frame);
            hash.Add(view);
            hash.Add(polygons.Count);
            foreach (var polygon in polygons)
            {
                hash.Add(polygon.Plane);
                hash.Add(polygon.Tag);
                foreach (var vertex in polygon.Vertices)
                    hash.Add(vertex);
            }

            return hash.ToHashCode();
        }

        public bool Equals(CutKey? other)
        {
            if (other is null || other._hash != _hash || other.Point != Point || other.Frame != Frame || other.View != View || other.Polygons.Count != Polygons.Count)
                return false;
            for (var i = 0; i < Polygons.Count; i++)
            {
                var (a, b) = (Polygons[i], other.Polygons[i]);
                if (!ReferenceEquals(a, b) && (a.Plane != b.Plane || a.Tag != b.Tag || !a.Vertices.SequenceEqual(b.Vertices)))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as CutKey);

        public override int GetHashCode() => _hash;

        private IReadOnlyList<KernelPolygon> Polygons { get; } = polygons;

        private Vector3R Point { get; } = point;

        private ViewFrame Frame { get; } = frame;

        private Guid View { get; } = view;
    }

    // ADR-0169 item 4: the hatch of the loops of the cut faces of one body on the paper: lines that go up to the right
    // (x - y = n s) or down to the right (x + y = n s), with s = 2.5 sqrt 2, clipped by the rule of even and odd.
    public static IEnumerable<SheetLine> Hatch(Guid body, IReadOnlyList<IReadOnlyList<(double X, double Y)>> loops, bool rising)
    {
        double Across((double X, double Y) p) => rising ? p.X - p.Y : p.X + p.Y;
        double Along((double X, double Y) p) => p.X + (rising ? p.Y : -p.Y);
        var step = HatchSpace * Math.Sqrt(2);
        var points = loops.SelectMany(loop => loop).ToList();
        if (points.Count == 0)
            yield break;
        var (low, high) = (points.Min(Across), points.Max(Across));
        // A region that is not finite or wider than a million lines gives no hatch, so a read always ends.
        if (!double.IsFinite(low) || !double.IsFinite(high) || (high - low) / step > 1e6)
            yield break;
        for (var n = Math.Ceiling(low / step); n * step <= high; n++)
        {
            var c = n * step;
            var crossings = new List<(double Key, double X, double Y)>();
            foreach (var loop in loops)
            {
                for (var i = 0; i < loop.Count; i++)
                {
                    var (p, q) = (loop[i], loop[(i + 1) % loop.Count]);
                    var (gp, gq) = (Across(p) - c, Across(q) - c);
                    if ((gp > 0) == (gq > 0))
                        continue;
                    var t = gp / (gp - gq);
                    var at = (p.X + (t * (q.X - p.X)), p.Y + (t * (q.Y - p.Y)));
                    crossings.Add((Along(at), at.Item1, at.Item2));
                }
            }

            crossings.Sort((first, second) => first.Key.CompareTo(second.Key));
            for (var i = 0; i + 1 < crossings.Count; i += 2)
            {
                var (from, to) = (crossings[i], crossings[i + 1]);
                if (from.Key < to.Key)
                    yield return new SheetLine(body, from.X, from.Y, to.X, to.Y, true, "hatch");
            }
        }
    }
}
