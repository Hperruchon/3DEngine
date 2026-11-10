namespace Engine.Kernel;

// ADR-0057 item 4: the provenance of a face: the feature that made it, the element of the feature, and the item of the
// element (a line or a curve of a profile), as a reference of ADR-0051 names a face.
public sealed record FaceTag(Guid Feature, string Element, Guid? Item = null)
{
    public override string ToString() => Item is { } item ? $"{Feature}:{Element}:{item}" : $"{Feature}:{Element}";
}

// A convex or concave planar polygon of a solid, counter-clockwise seen from the front of its plane, with its tag.
public sealed record KernelPolygon(IReadOnlyList<Vector3R> Vertices, PlaneR Plane, FaceTag Tag)
{
    public KernelPolygon Flipped() => new([.. Vertices.Reverse()], Plane.Flipped(), Tag);

    // The normal of the plane does not change, so the plane of the moved polygon keeps its canonical form.
    public KernelPolygon Moved(Vector3R offset)
        => new([.. Vertices.Select(v => v + offset)], new PlaneR(Plane.Normal, Plane.Normal.Dot(Vertices[0] + offset)), Tag);

    // ADR-0112 item 5 (TASK-0335): the polygon under the linear map p -> x p.X + y p.Y + z p.Z, with a determinant
    // greater than zero. A normal goes by the cofactors (y × z, z × x, x × y), so the plane is exact also when the map is
    // not exactly a turn; the canonical form makes two equal planes equal values again.
    public KernelPolygon Mapped(Vector3R x, Vector3R y, Vector3R z)
    {
        Vector3R Map(Vector3R p) => (x * p.X) + (y * p.Y) + (z * p.Z);
        var n = Plane.Normal;
        var normal = (y.Cross(z) * n.X) + (z.Cross(x) * n.Y) + (x.Cross(y) * n.Z);
        var vertices = Vertices.Select(Map).ToList();
        return new([.. vertices], new PlaneR(normal, normal.Dot(vertices[0])).Canonical(), Tag);
    }
}
