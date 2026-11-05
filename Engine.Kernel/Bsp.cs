namespace Engine.Kernel;

// ADR-0057 item 5: a binary space partition of the polygons of a solid, in the form of the algorithm of csg.js (Evan
// Wallace, MIT licence; https://github.com/evanw/csg.js). Each plane and each split point is exact, so each side of
// each vertex is the true side, and a coplanar polygon goes to the side of the sign of the dot product of the normals.
internal sealed class Bsp
{
    private PlaneR? _plane;
    private Bsp? _front;
    private Bsp? _back;
    private readonly List<KernelPolygon> _polygons = [];

    public Bsp(IEnumerable<KernelPolygon> polygons) => Build([.. polygons]);

    private Bsp()
    {
    }

    // Each node turns inside out: each polygon and each plane flips, and the two sides swap.
    public void Invert()
    {
        for (var i = 0; i < _polygons.Count; i++)
            _polygons[i] = _polygons[i].Flipped();
        if (_plane is { } plane)
            _plane = plane.Flipped();
        _front?.Invert();
        _back?.Invert();
        (_front, _back) = (_back, _front);
    }

    // The parts of the polygons that are outside this solid. A part behind a plane with no back node is inside.
    public List<KernelPolygon> Clip(List<KernelPolygon> polygons)
    {
        if (_plane is not { } plane)
            return [.. polygons];
        var front = new List<KernelPolygon>();
        var back = new List<KernelPolygon>();
        foreach (var polygon in polygons)
            Split(plane, polygon, front, back, front, back);
        front = _front is not null ? _front.Clip(front) : front;
        back = _back is not null ? _back.Clip(back) : [];
        return [.. front, .. back];
    }

    // The polygons of this tree that are outside the other tree.
    public void ClipTo(Bsp other)
    {
        var kept = other.Clip(_polygons);
        _polygons.Clear();
        _polygons.AddRange(kept);
        _front?.ClipTo(other);
        _back?.ClipTo(other);
    }

    public List<KernelPolygon> AllPolygons()
    {
        var all = new List<KernelPolygon>(_polygons);
        if (_front is not null)
            all.AddRange(_front.AllPolygons());
        if (_back is not null)
            all.AddRange(_back.AllPolygons());
        return all;
    }

    // Adds polygons to the tree: the first polygon of a new node gives its plane.
    public void Build(List<KernelPolygon> polygons)
    {
        if (polygons.Count == 0)
            return;
        _plane ??= polygons[0].Plane;
        var plane = _plane.Value;
        var front = new List<KernelPolygon>();
        var back = new List<KernelPolygon>();
        foreach (var polygon in polygons)
            Split(plane, polygon, _polygons, _polygons, front, back);
        if (front.Count > 0)
            (_front ??= new Bsp()).Build(front);
        if (back.Count > 0)
            (_back ??= new Bsp()).Build(back);
    }

    // The split of a polygon by a plane into its parts in front of and behind the plane. A coplanar polygon goes to the
    // coplanar list of the side to which its normal points.
    internal static void Split(
        PlaneR plane, KernelPolygon polygon,
        List<KernelPolygon> coplanarFront, List<KernelPolygon> coplanarBack, List<KernelPolygon> front, List<KernelPolygon> back)
    {
        var sides = polygon.Vertices.Select(plane.Side).ToArray();
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
}
