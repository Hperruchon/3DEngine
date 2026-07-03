namespace Engine.Contracts.Geometry;

// Read-only geometry queries per ADR-0001 and ADR-0012 §1. V1 ships
// GetBoundingBox only.
public interface IGeometryQuery
{
    // Returns the axis-aligned bounding box of the body stored under `handle`.
    // Throws if the handle is unknown to this backend — the caller (query
    // handler) is responsible for surfacing that as a structured error.
    Aabb GetBoundingBox(BodyHandle handle);
}
