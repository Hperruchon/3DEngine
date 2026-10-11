using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Geometry;
using Engine.Geometry.Manifold;

namespace Engine.Api.Http;

// The composition root of engine-api-http: one engine for each process, and
// restart resets state, because no persistence exists before phase P8a
// (ADR-0015). Per ADR-0014 section 4 only a composition root may name
// Engine.Geometry.Manifold, so the choice of the backend lives here and not in
// the surface library (ADR-0019 item 3, TASK-0038).
//
// The host requires the native Manifold backend (TASK-0036). When its library does
// not load, Create throws BackendUnavailableException with E-GEOM-BACKEND-INIT, and
// the program stops before it serves a request, with exit code 3. Until TASK-0036
// the host took the managed backend with no message, which holds boxes only;
// anti-objective 9 refuses that silent fallback. Only a test selects the managed
// backend, through HostBackendOptions in the service container.
internal static class ServerEngine
{
    public static EngineHost Create(HostBackendOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.UseManagedBackend)
        {
            var managed = new InProcessMeshBackend();
            return new EngineHost(managed, "managed", typeof(InProcessMeshBackend).Assembly.GetName().Version?.ToString() ?? "unknown");
        }

        if (options.IsNativeAvailable())
            return new EngineHost(new ManifoldGeometryBackend(), "manifold", ManifoldGeometryBackend.NativeVersion);

        throw new BackendUnavailableException(
            $"{DiagnosticCodes.GeomBackendInit}: {ManifoldGeometryBackend.UnavailableReason()}");
    }
}

// The backend of the HTTP host. A person has one choice: the native backend, or a
// stop with E-GEOM-BACKEND-INIT. A test replaces this record in the service
// container to select the managed backend, or to replace the probe and show the
// stop (TASK-0036). No argument and no setting of the host selects it.
internal sealed record HostBackendOptions(bool UseManagedBackend, Func<bool> IsNativeAvailable)
{
    public static HostBackendOptions Native { get; } = new(false, ManifoldGeometryBackend.IsNativeAvailable);

    public static HostBackendOptions ManagedForTests { get; } = new(true, () => false);
}

internal sealed class BackendUnavailableException(string message) : InvalidOperationException(message);
