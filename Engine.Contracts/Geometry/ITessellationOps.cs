namespace Engine.Contracts.Geometry;

// Tessellation capability per ADR-0018. Gives the triangles of a body, so that a
// client can draw it (roadmap phase R2). A presentation output: it never enters
// a command, an event, the Document or the log (CLAUDE.md, determinism rule 6).
//
// Kept separate from IMeshOps and IGeometryQuery, as ITransformOps and
// IBooleanOps are (ADR-0012 Amendment 1). The managed InProcessMeshBackend does
// not implement it, so a caller on that backend gets E-GEOM-CAP-MISSING
// (ADR-0018 item 8).
public interface ITessellationOps
{
    // Returns the triangles of the body stored under `handle`, wound
    // counter-clockwise when seen from outside the body (ADR-0018 item 9).
    // Throws if the handle is unknown to this backend; the query handler
    // surfaces that as a structured error.
    Tessellation Tessellate(BodyHandle handle);
}
