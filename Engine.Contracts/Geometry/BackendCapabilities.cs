namespace Engine.Contracts.Geometry;

// Capability flags per ADR-0001 §1 and ADR-0012 §1. A backend declares
// which capability interfaces it implements via this enum; consumers
// (command handlers) negotiate the actual interface with
// IGeometryBackend.TryGet<T>().
//
// V1 ships Mesh + Query. Transform + Booleans land in ADR-0012 (Amendment 1,
// P7c). BRep, ExactBooleans (exact/BRep booleans, distinct from the mesh
// Booleans here), FaceIds, Offset, Fillet are reserved per ADR-0001 §1.
[Flags]
public enum BackendCapabilities
{
    None      = 0,
    Mesh      = 1 << 0,
    Query     = 1 << 1,
    Transform = 1 << 2,
    Booleans  = 1 << 3,
}
