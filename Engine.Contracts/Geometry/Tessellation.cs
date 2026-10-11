namespace Engine.Contracts.Geometry;

// Query-output value type for the triangles of a body, per ADR-0018 item 2. It
// has no identity and no operation, in the same way as Aabb.
//
// Positions holds x, y and z of each vertex in sequence, in double precision
// (ADR-0018 item 3); a host narrows each value for the graphics processor.
// Indices holds three vertex indices for each triangle, wound counter-clockwise
// when seen from outside the body (ADR-0018 item 9).
public sealed record Tessellation(double[] Positions, uint[] Indices)
{
    public int VertexCount => Positions.Length / 3;

    public int TriangleCount => Indices.Length / 3;
}
