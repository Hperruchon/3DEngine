using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;

namespace Engine.Api.Http;

// The document session that the surface serves, and what the surface needs to
// serve it. A host composes it and mounts the surface on it (ADR-0019 items 1 and
// 3, TASK-0038): engine-api-http in its program, and the desktop host in its own
// process (TASK-0054). The host owns the session, so a host can also send typed
// commands and queries to it; each one goes through the same bus as a command
// over HTTP (ADR-0019 item 2).
//
// The host chooses the backend and gives it here with its name and version. The
// library references Engine.Core and Engine.Contracts only (ADR-0019 item 3), so
// it cannot name the native backend; that choice belongs to a composition root
// (ADR-0014 section 4). Until TASK-0038 this class chose the backend itself.
//
// The session gets a decorated sink, so that each committed event reaches each
// WebSocket subscriber (TASK-0010 section 2). Each endpoint reaches the engine
// through Session (TASK-0034): commands, queries and the WebSocket handshake
// enter one serial section.
public sealed class EngineHost : IDisposable
{
    public EngineHost(IGeometryBackend backend, string backendName, string backendVersion)
    {
        ArgumentNullException.ThrowIfNull(backend);

        Backend = backend;
        BackendName = backendName;
        BackendVersion = backendVersion;
        Broadcaster = new EventBroadcaster();

        // One composition point for every other part, shared with the CLI.
        // Each registered handler comes from HandlerCatalog per ADR-0016.
        var kit = EngineHosting.CreateDefault(Backend);
        Document = kit.Document;
        CommandRegistry = kit.CommandRegistry;
        QueryRegistry = kit.QueryRegistry;
        Session = new DocumentSession(kit, new BroadcastingEventSink(kit.Events, Broadcaster));
    }

    public DocumentSession Session { get; }

    // The name and the version of the backend that the host composed, for
    // /schema/backend. Engine.Contracts does not change for them.
    public string BackendName { get; }

    public string BackendVersion { get; }

    public CommandRegistry CommandRegistry { get; }

    public QueryRegistry QueryRegistry { get; }

    // Internal, so that a host reaches the backend through the bus only (ADR-0019
    // item 2). It was public until TASK-0062 (finding E56 of the review of
    // 2026-10-11).
    internal IGeometryBackend Backend { get; }

    internal EventBroadcaster Broadcaster { get; }

    // For the tests only. A read of the Document here has no lock, therefore an
    // endpoint must read through Session.Read. The identifier does not change,
    // and a test reads the version only when no command runs.
    internal Document Document { get; }

    // Gives each later event of the session to the action, after its commit and in
    // the order of the commits: the same events that /events gives (ADR-0024 item 1,
    // ADR-0019 item 2). The action runs inside the commit, so it must not call the
    // session and must not block; a host puts the event in a queue. An action that
    // throws is removed, with the warning W-API-SUBSCRIPTION-REMOVED on the error
    // output. The payload of each event is read-only. To read the live bodies and
    // subscribe with no gap, call this inside Session.Read. Dispose the result to stop;
    // an event of a commit in progress can still arrive after Dispose returns.
    public IDisposable Subscribe(Action<EventRecord> action) => Broadcaster.Listen(action);

    public void Dispose() => (Backend as IDisposable)?.Dispose();
}
