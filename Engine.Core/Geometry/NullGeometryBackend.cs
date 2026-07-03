using Engine.Contracts.Geometry;

namespace Engine.Core.Geometry;

// Empty backend for hosts that don't (yet) wire a real one. CommandBus
// requires a non-null IGeometryBackend per ADR-0012 §2; this satisfies
// the constructor without claiming any capability. Any handler that
// asks for a capability via TryGet<T>() gets null and is responsible
// for surfacing E-GEOM-CAP-MISSING.
public sealed class NullGeometryBackend : IGeometryBackend
{
    public static NullGeometryBackend Instance { get; } = new();

    public BackendCapabilities Capabilities => BackendCapabilities.None;

    public T? TryGet<T>() where T : class => null;
}
