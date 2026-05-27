using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Geometry;
using Engine.Core.Hosting;

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
// Default wiring (Document, registries with V1.x handlers, in-memory event
// sink, InProcessMeshBackend per ADR-0012 §7) comes from
// EngineHosting.CreateDefault (TASK-0013). The host's sole responsibility
// here is to wrap the kit's Events in BroadcastingEventSink before
// constructing CommandBus.
internal sealed class EngineHost
{
    public Document Document { get; }
    public CommandRegistry CommandRegistry { get; }
    public QueryRegistry QueryRegistry { get; }
    public CommandBus CommandBus { get; }
    public QueryBus QueryBus { get; }
    public InMemoryEventSink Events { get; }
    public InProcessMeshBackend Backend { get; }

    public EngineHost(EventBroadcaster broadcaster)
    {
        var kit = EngineHosting.CreateDefault();
        Document = kit.Document;
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;
        Events = kit.Events;
        Backend = kit.Backend;
        var broadcastingSink = new BroadcastingEventSink(kit.Events, broadcaster);
        CommandBus = new CommandBus(kit.Document, kit.CommandRegistry, broadcastingSink, kit.Backend);
        QueryBus = new QueryBus(kit.Document, kit.QueryRegistry, kit.Backend);
    }
}
