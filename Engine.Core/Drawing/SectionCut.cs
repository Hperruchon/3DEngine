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
    // material stays. The box holds the points P + a R' + b U' + c E' with |a| <= A, |b| <= A and 0 <= c <= A, where A is 1
    // plus the largest of |a|, |b| and |c| over the vertices of the solid. Each number is exact.
    public static KernelSolid? Apply(KernelSolid solid, Vector3R point, ViewFrame frame, Guid view)
    {
        ArgumentNullException.ThrowIfNull(solid);
        var (r, u, e) = (frame.Right, frame.Up, frame.TowardEye);
        var (rr, uu, ee) = (r.Dot(r), u.Dot(u), e.Dot(e));
        var extent = Rational.Zero;
        foreach (var vertex in solid.Polygons.SelectMany(polygon => polygon.Vertices))
        {
            var d = vertex - point;
            foreach (var value in new[] { d.Dot(r) / rr, d.Dot(u) / uu, d.Dot(e) / ee })
            {
                var size = value.Sign < 0 ? -value : value;
                if (size > extent)
                    extent = size;
            }
        }

        var a = extent + Rational.One;
        var corners = new Vector3R[8];
        for (var i = 0; i < 8; i++)
        {
            var (x, y, z) = ((i & 1) != 0 ? a : -a, (i & 2) != 0 ? a : -a, (i & 4) != 0 ? a : Rational.Zero);
            corners[i] = point + (r * x) + (u * y) + (e * z);
        }

        // The six faces of KernelSolid.Box, each as two triangles; the corner i has its bits 1, 2 and 4 for +a, +b and +c.
        int[][] quads = [[1, 3, 7, 5], [0, 4, 6, 2], [2, 6, 7, 3], [0, 1, 5, 4], [4, 5, 7, 6], [0, 2, 3, 1]];
        var triangles = quads.SelectMany(q => new[] { q[0], q[1], q[2], q[0], q[2], q[3] }).ToArray();
        var box = KernelSolid.Mesh(Tag(view).Feature, Tag(view).Element, corners, triangles);
        return PlanarBoolean.Subtract(solid, box);
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
