using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;

namespace Engine.Api.Http;

// Single in-process engine owned by the host. Lives for the lifetime of the
// host process; restart resets state, because no persistence exists before phase
// P8a (ADR-0015).
//
// Wraps the in-memory event sink with a BroadcastingEventSink so that every
// committed event flows to connected WebSocket subscribers (TASK-0010 §2).
// Engine.Core stays untouched; the broadcaster + decorator live entirely in
// Engine.Api.Http.
//
// The host requires the native Manifold backend (TASK-0036). When its library does
// not load, the constructor throws BackendUnavailableException with
// E-GEOM-BACKEND-INIT, and Program.cs stops the host before it serves a request.
// Until TASK-0036 the host took the managed backend with no message, which holds
// boxes only; anti-objective 9 refuses that silent fallback. Only a test selects
// the managed backend, through HostBackendOptions in the service container.
//
// Each endpoint reaches the engine through Session (TASK-0034): commands,
// queries and the WebSocket handshake enter one serial section.
internal sealed class EngineHost : IDisposable
{
    public DocumentSession Session { get; }

    // The name and the version of the backend that the host composed, for
    // /schema/backend. Engine.Contracts does not change for them.
    public string BackendName { get; }
    public string BackendVersion { get; }
    public CommandRegistry CommandRegistry { get; }
    public QueryRegistry QueryRegistry { get; }
    public IGeometryBackend Backend { get; }

    // For the tests only. A read of the Document here has no lock, therefore an
    // endpoint must read through Session.Read. The identifier does not change,
    // and a test reads the version only when no command runs.
    public Document Document { get; }

    public EngineHost(EventBroadcaster broadcaster, HostBackendOptions options)
    {
        // Per ADR-0014 section 4 the host selects the backend at its composition
        // root, because only a composition root may name Engine.Geometry.Manifold.
        if (options.UseManagedBackend)
        {
            Backend = new InProcessMeshBackend();
            BackendName = "managed";
            BackendVersion = typeof(InProcessMeshBackend).Assembly.GetName().Version?.ToString() ?? "unknown";
        }
        else if (options.IsNativeAvailable())
        {
            Backend = new ManifoldGeometryBackend();
            BackendName = "manifold";
            BackendVersion = ManifoldGeometryBackend.NativeVersion;
        }
        else
        {
            throw new BackendUnavailableException(
                $"{DiagnosticCodes.GeomBackendInit}: {ManifoldGeometryBackend.UnavailableReason()}");
        }

        // One composition point for every other part, shared with the CLI.
        // Each registered handler comes from HandlerCatalog per ADR-0016.
        var kit = EngineHosting.CreateDefault(Backend);
        Document = kit.Document;
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;

        // The bus must see the decorated sink, so that every committed event
        // reaches each WebSocket subscriber (TASK-0010 section 2).
        Session = new DocumentSession(kit, new BroadcastingEventSink(kit.Events, broadcaster));
    }

    public void Dispose() => (Backend as IDisposable)?.Dispose();
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
