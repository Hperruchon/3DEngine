namespace Engine.Contracts.Geometry;

// Mesh capability per ADR-0001 and ADR-0012 §1. V1 ships CreateBox only;
// additional methods (translate, boolean, tessellate-export, etc.) land
// in their own sized TASK with an ADR-0012 amendment.
public interface IMeshOps
{
    // Stores a box under the supplied handle. The handle is computed by the
    // command handler (deterministically from CommandId per ADR-0012 §4) so
    // replay produces identical state across runs.
    void CreateBox(BodyHandle handle, BoxParameters parameters);
}
