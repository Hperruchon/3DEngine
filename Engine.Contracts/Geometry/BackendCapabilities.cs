namespace Engine.Contracts.Geometry;

// Capability flags per ADR-0001 §1 and ADR-0012 §1. A backend declares
// which capability interfaces it implements via this enum; consumers
// (command handlers) negotiate the actual interface with
// IGeometryBackend.TryGet<T>().
//
// V1 ships Mesh + Query. BRep, ExactBooleans, FaceIds, Offset, Fillet
// are reserved per ADR-0001 §1.
[Flags]
public enum BackendCapabilities
{
    None  = 0,
    Mesh  = 1 << 0,
    Query = 1 << 1,
}
