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
// host process; restart resets state. Per V1 clamp "No persistence — in-memory
// only until persistence ADR + TASK."
//
// Wraps the in-memory event sink with a BroadcastingEventSink so that every
// committed event flows to connected WebSocket subscribers (TASK-0010 §2).
// Engine.Core stays untouched; the broadcaster + decorator live entirely in
// Engine.Api.Http.
//
// Per ADR-0014 §4: the host selects the native Manifold backend when its native
// library is loadable, else falls back to the managed InProcessMeshBackend so the
// host runs on any platform. CommandBus + QueryBus both receive the chosen backend.
internal sealed class EngineHost : IDisposable
{
    public Document Document { get; }
    public CommandRegistry CommandRegistry { get; }
    public QueryRegistry QueryRegistry { get; }
    public CommandBus CommandBus { get; }
    public QueryBus QueryBus { get; }
    public InMemoryEventSink Events { get; }
    public IGeometryBackend Backend { get; }

    public EngineHost(EventBroadcaster broadcaster)
    {
        // Per ADR-0014 section 4 the host selects the backend at its composition
        // root, because only a composition root may name Engine.Geometry.Manifold.
        Backend = ManifoldGeometryBackend.IsNativeAvailable()
            ? new ManifoldGeometryBackend()
            : new InProcessMeshBackend();

        // One composition point for every other part, shared with the CLI.
        // Each registered handler comes from HandlerCatalog per ADR-0016.
        var kit = EngineHosting.CreateDefault(Backend);
        Document = kit.Document;
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;
        Events = kit.Events;

        // The bus must see the decorated sink, so that every committed event
        // reaches each WebSocket subscriber (TASK-0010 section 2).
        CommandBus = kit.CreateCommandBus(new BroadcastingEventSink(Events, broadcaster));
        QueryBus = kit.CreateQueryBus();
    }

    public void Dispose() => (Backend as IDisposable)?.Dispose();
}
