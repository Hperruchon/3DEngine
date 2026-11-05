namespace Engine.Kernel;

public static class PlanarBoolean
{
    // ADR-0057 item 5: a minus b, in the steps of csg.js: the parts of a outside b, and the parts of b inside a, turned.
    // Each part keeps the tag of its face. Null when the result is empty, which the caller refuses.
    public static KernelSolid? Subtract(KernelSolid a, KernelSolid b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var left = new Bsp(a.Polygons);
        var right = new Bsp(b.Polygons);
        left.Invert();
        left.ClipTo(right);
        right.ClipTo(left);
        right.Invert();
        right.ClipTo(left);
        right.Invert();
        left.Build(right.AllPolygons());
        left.Invert();
        var polygons = left.AllPolygons();
        if (polygons.Count == 0)
            return null;
        var result = new KernelSolid(polygons);
        return result.Volume().Sign > 0 ? result : null;
    }
}
