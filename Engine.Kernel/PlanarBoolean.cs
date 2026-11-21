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

    // ADR-0181 item 5 (phase K16, TASK-0433): a and b together, in the steps of the union of csg.js: the parts of a outside
    // b, and the parts of b outside a. A face that the two solids share on one plane with opposite normals goes, and a
    // face that they share with one normal stays one time. No part turns, and each part keeps the tag of its face.
    public static KernelSolid? Union(KernelSolid a, KernelSolid b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var left = new Bsp(a.Polygons);
        var right = new Bsp(b.Polygons);
        left.ClipTo(right);
        right.ClipTo(left);
        right.Invert();
        right.ClipTo(left);
        right.Invert();
        left.Build(right.AllPolygons());
        return Solid(left.AllPolygons());
    }

    // ADR-0181 item 5: the volume that a and b share, in the steps of the intersection of csg.js: the parts of a inside b,
    // and the parts of b inside a. No part turns, and each part keeps the tag of its face. Two solids that only touch, or
    // that are apart, share no volume, so the result is null, which the caller refuses, as an empty subtract is.
    public static KernelSolid? Intersect(KernelSolid a, KernelSolid b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var left = new Bsp(a.Polygons);
        var right = new Bsp(b.Polygons);
        left.Invert();
        right.ClipTo(left);
        right.Invert();
        left.ClipTo(right);
        right.ClipTo(left);
        left.Build(right.AllPolygons());
        left.Invert();
        return Solid(left.AllPolygons());
    }

    // The solid of the polygons, or null when they hold no volume.
    private static KernelSolid? Solid(List<KernelPolygon> polygons)
    {
        if (polygons.Count == 0)
            return null;
        var result = new KernelSolid(polygons);
        return result.Volume().Sign > 0 ? result : null;
    }
}
