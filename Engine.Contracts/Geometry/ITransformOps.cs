namespace Engine.Contracts.Geometry;

// Transform capability per ADR-0012 (Amendment 1, P7c). Repositions a body.
// A backend that implements this moves an existing body, producing a NEW body
// (the source is left intact); the command handler computes the new body's
// handle deterministically from the CommandId (ADR-0012 §4) so replay is stable.
//
// Kept separate from IMeshOps because the managed InProcessMeshBackend stores
// only centered box dimensions (no positions) and genuinely cannot represent a
// moved solid. It does not implement this interface, so callers on that backend
// get a clean E-GEOM-CAP-MISSING rather than a silent or dishonest fallback.
public interface ITransformOps
{
    // Stores a copy of the body under `source`, translated by (dx, dy, dz),
    // under `result`. Throws if `source` is unknown to the backend, or if
    // `result` is already taken (mirrors IMeshOps.CreateBox's contract).
    void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz);
}
