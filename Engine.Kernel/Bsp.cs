using System.Runtime.CompilerServices;

namespace Engine.Kernel;

// ADR-0057 item 5: a binary space partition of the polygons of a solid, in the form of the algorithm of csg.js (Evan
// Wallace, MIT licence; https://github.com/evanw/csg.js). Each plane and each split point is exact, so each side of
// each vertex is the true side, and a coplanar polygon goes to the side of the sign of the dot product of the normals.
internal sealed class Bsp
{
    private PlaneR? _plane;
    private Approximate _approximate;
    private Bsp? _front;
    private Bsp? _back;
    private readonly List<KernelPolygon> _polygons = [];

    public Bsp(IEnumerable<KernelPolygon> polygons) => Build([.. polygons]);

    private Bsp()
    {
    }

    // ADR-0089 item 6 (TASK-0289): each walk of the tree uses a stack of its own and no recursion. The tree of a convex
    // solid is a chain with one node for each polygon, so a sphere of 3968 triangles gave a recursion as deep, and the
    // stack of the thread overflowed. Each walk keeps the order of the recursive form: a node, then its front, then its back.

    // Each node turns inside out: each polygon and each plane flips, and the two sides swap.
    public void Invert()
    {
        var stack = new Stack<Bsp>([this]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            for (var i = 0; i < node._polygons.Count; i++)
                node._polygons[i] = node._polygons[i].Flipped();
            if (node._plane is { } plane)
            {
                node._plane = plane.Flipped();
                node._approximate = node._approximate.Flipped();
            }
            (node._front, node._back) = (node._back, node._front);
            if (node._front is not null)
                stack.Push(node._front);
            if (node._back is not null)
                stack.Push(node._back);
        }
    }

    // The parts of the polygons that are outside this solid. A part behind a plane with no back node is inside. The parts
    // come in the order of the recursive form: the parts of the front branch before the parts of the back branch.
    public List<KernelPolygon> Clip(List<KernelPolygon> polygons)
    {
        var result = new List<KernelPolygon>();
        var stack = new Stack<(Bsp? Node, List<KernelPolygon> Polygons, bool Keep)>([(this, polygons, true)]);
        while (stack.Count > 0)
        {
            var (node, parts, keep) = stack.Pop();
            if (node is null)
            {
                // A branch with no node: the front keeps its parts, and the back drops them.
                if (keep)
                    result.AddRange(parts);
                continue;
            }

            if (node._plane is not { } plane)
            {
                result.AddRange(parts);
                continue;
            }

            var front = new List<KernelPolygon>();
            var back = new List<KernelPolygon>();
            foreach (var polygon in parts)
                Split(plane, node._approximate, polygon, front, back, front, back);
            stack.Push((node._back, back, false));
            stack.Push((node._front, front, true));
        }

        return result;
    }

    // The polygons of this tree that are outside the other tree.
    public void ClipTo(Bsp other)
    {
        var stack = new Stack<Bsp>([this]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            var kept = other.Clip(node._polygons);
            node._polygons.Clear();
            node._polygons.AddRange(kept);
            if (node._back is not null)
                stack.Push(node._back);
            if (node._front is not null)
                stack.Push(node._front);
        }
    }

    public List<KernelPolygon> AllPolygons()
    {
        var all = new List<KernelPolygon>();
        var stack = new Stack<Bsp>([this]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            all.AddRange(node._polygons);
            if (node._back is not null)
                stack.Push(node._back);
            if (node._front is not null)
                stack.Push(node._front);
        }

        return all;
    }

    // Adds polygons to the tree: the first polygon of a new node gives its plane.
    public void Build(List<KernelPolygon> polygons)
    {
        var stack = new Stack<(Bsp Node, List<KernelPolygon> Polygons)>([(this, polygons)]);
        while (stack.Count > 0)
        {
            var (node, parts) = stack.Pop();
            if (parts.Count == 0)
                continue;
            if (node._plane is null)
                (node._plane, node._approximate) = (parts[0].Plane, Approximate.Of(parts[0].Plane));
            var plane = node._plane.Value;
            var front = new List<KernelPolygon>();
            var back = new List<KernelPolygon>();
            foreach (var polygon in parts)
                Split(plane, node._approximate, polygon, node._polygons, node._polygons, front, back);
            if (back.Count > 0)
                stack.Push((node._back ??= new Bsp(), back));
            if (front.Count > 0)
                stack.Push((node._front ??= new Bsp(), front));
        }
    }

    // The split of a polygon by a plane into its parts in front of and behind the plane. A coplanar polygon goes to the
    // coplanar list of the side to which its normal points.
    internal static void Split(
        PlaneR plane, Approximate approximate, KernelPolygon polygon,
        List<KernelPolygon> coplanarFront, List<KernelPolygon> coplanarBack, List<KernelPolygon> front, List<KernelPolygon> back)
    {
        var doubles = Approximations.GetValue(polygon.Vertices, Approximate.Of);
        var sides = new int[polygon.Vertices.Count];
        for (var i = 0; i < sides.Length; i++)
            sides[i] = approximate.Side(plane, polygon.Vertices[i], doubles[3 * i], doubles[(3 * i) + 1], doubles[(3 * i) + 2]);
        var hasFront = sides.Any(side => side > 0);
        var hasBack = sides.Any(side => side < 0);
        if (!hasFront && !hasBack)
        {
            (plane.Normal.Dot(polygon.Plane.Normal).Sign > 0 ? coplanarFront : coplanarBack).Add(polygon);
            return;
        }
        if (!hasBack)
        {
            front.Add(polygon);
            return;
        }
        if (!hasFront)
        {
            back.Add(polygon);
            return;
        }

        var f = new List<Vector3R>();
        var b = new List<Vector3R>();
        var count = polygon.Vertices.Count;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            var (vi, vj) = (polygon.Vertices[i], polygon.Vertices[j]);
            var (si, sj) = (sides[i], sides[j]);
            if (si >= 0)
                f.Add(vi);
            if (si <= 0)
                b.Add(vi);
            if (si * sj < 0)
            {
                // The exact point of the edge on the plane.
                var t = (plane.Offset - plane.Normal.Dot(vi)) / plane.Normal.Dot(vj - vi);
                var point = vi + ((vj - vi) * t);
                f.Add(point);
                b.Add(point);
            }
        }

        if (f.Count >= 3)
            front.Add(polygon with { Vertices = f });
        if (b.Count >= 3)
            back.Add(polygon with { Vertices = b });
    }

    // ADR-0089 item 6 (K5d, TASK-0289): the doubles of the vertices of each list, made once. A list does not change after
    // its polygon is made, so the table keys it by its reference.
    private static readonly ConditionalWeakTable<IReadOnlyList<Vector3R>, double[]> Approximations = new();

    // ADR-0089 item 6 (K5d, TASK-0289): a filter of doubles before the exact sign of a side. The doubles of the plane and of
    // the vertex are the nearest doubles of the exact values, so each has a relative error of at most 2^-53, and the sum of
    // three products and a difference adds at most six such errors of the sum of the magnitudes. A double result larger
    // than 1e-14 times that sum, about ten times the bound, has the exact sign; else the exact sign of Rational decides.
    // The filter decides only where the exact sign is certain, so each side, each tree and each result stay the same.
    // Finding E195 of the codebase review of 2026-11-08 (b) (TASK-0303): the bound holds for a normal double only. A value
    // that is not zero and whose double is subnormal, zero or infinite becomes NaN, and the exact sign decides. Until
    // TASK-0303 a subnormal coefficient times a large coordinate gave the opposite side.
    internal readonly record struct Approximate(double A, double B, double C, double D, bool Known)
    {
        public static Approximate Of(PlaneR plane)
            => new(Near(plane.Normal.X), Near(plane.Normal.Y), Near(plane.Normal.Z), Near(plane.Offset), true);

        // The nearest double of a value when it is zero or a normal double, else NaN.
        internal static double Near(Rational value)
        {
            if (value.IsZero)
                return 0;
            var near = value.ToDouble();
            return double.IsFinite(near) && Math.Abs(near) >= SmallestNormal ? near : double.NaN;
        }

        private const double SmallestNormal = 2.2250738585072014E-308;

        public static double[] Of(IReadOnlyList<Vector3R> vertices)
        {
            var doubles = new double[3 * vertices.Count];
            for (var i = 0; i < vertices.Count; i++)
                (doubles[3 * i], doubles[(3 * i) + 1], doubles[(3 * i) + 2]) = (Near(vertices[i].X), Near(vertices[i].Y), Near(vertices[i].Z));
            return doubles;
        }

        public Approximate Flipped() => this with { A = -A, B = -B, C = -C, D = -D };

        public int Side(PlaneR plane, Vector3R vertex, double x, double y, double z)
        {
            if (Known)
            {
                var (ax, by, cz) = (A * x, B * y, C * z);
                var sum = ax + by + cz - D;
                var magnitude = Math.Abs(ax) + Math.Abs(by) + Math.Abs(cz) + Math.Abs(D);
                if (double.IsFinite(sum) && double.IsFinite(magnitude) && magnitude > 1e-280 && Math.Abs(sum) > 1e-14 * magnitude)
                    return sum > 0 ? 1 : -1;
            }

            return plane.Side(vertex);
        }
    }
}
