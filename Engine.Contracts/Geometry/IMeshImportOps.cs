namespace Engine.Contracts.Geometry;

// ADR-0083 item 8 (phase P9a, TASK-0257): the capability of a backend that holds a mesh body, the triangles of an STL
// file that the command ImportStl read. The backend keeps the floats as they are, with no arithmetic and no repair, and
// its Tessellate gives them back: each float as the double of the same value, the three vertices of each triangle in the
// order of the file, with no shared vertex. A mesh body is not a solid of the kernel, so no other capability takes it
// (ADR-0083 item 4).
public interface IMeshImportOps
{
    // Stores the triangles under the handle: nine floats for each triangle, x, y and z of its three vertices. The count
    // of the floats is a multiple of nine and greater than zero; the caller checks the limit of ADR-0083 item 6.
    void ImportMesh(BodyHandle handle, ReadOnlySpan<float> triangles);
}
