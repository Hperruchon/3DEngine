namespace Engine.Contracts.Geometry;

// ADR-0089 item 1 (phase K5c, TASK-0288): the capability of a backend that stores a closed triangle mesh as a solid, which
// then takes each operation of a solid (subtract, move, tessellate, export). The engine computes the mesh from the sizes of
// a command, with + - * / and sqrt only (ADR-0040 item 2), so each vertex has the same bits on each platform. A backend with
// no such capability gives E-GEOM-CAP-MISSING through the handler.
public interface ISolidMeshOps
{
    // Stores under `handle` the solid of the mesh: three doubles for each vertex (x, y and z), and three indices for each
    // triangle, counter-clockwise when seen from outside. The mesh is closed, and each edge belongs to two triangles.
    // Throws if `handle` is already taken, or if the backend cannot make a solid of the mesh.
    void CreateSolid(BodyHandle handle, ReadOnlySpan<double> positions, ReadOnlySpan<int> triangles);
}
