namespace Engine.Contracts.Geometry;

// The only top-level kernel type that command handlers see. Per ADR-0001 §1
// and ADR-0012 §1. Handlers negotiate the actual capability interface they
// need via TryGet<T>(); the backend declares which capabilities are present
// via the Capabilities flags.
public interface IGeometryBackend
{
    BackendCapabilities Capabilities { get; }

    // Returns the capability interface implementation if this backend
    // implements it, or null. No silent fallbacks (ADR-0001 §4).
    T? TryGet<T>() where T : class;
}
