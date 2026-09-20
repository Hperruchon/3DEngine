using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Queries;

namespace Engine.Core.Hosting;

// One composition point for the default engine. Salvaged from branch
// claude/happy-booth-1cef3f, which held a working factory that nobody merged.
// Register entry R-0012 recorded that branch.
//
// The factory does not choose the geometry backend. The caller gives it. The
// reason is a boundary rule: ADR-0014 section 4 permits a reference to
// Engine.Geometry.Manifold only at the composition root of a host, and
// CLAUDE.md permits Engine.Core to reference only Engine.Contracts. A factory
// that selected the native backend would break both rules, and the dependency
// direction gate of v0.19 would fail. Each host therefore keeps its own three
// lines of backend selection, and that duplication is correct.
//
// The factory does not construct a bus either. The original branch gave the
// reason and it still holds: the HTTP host wraps the event sink in
// BroadcastingEventSink before the bus sees it, and the command-line host uses
// the sink directly. The kit gives two helpers instead, so each host names the
// sink that it wants and nothing else.
//
// Handler registration belongs to HandlerCatalog per ADR-0016. The branch
// version carried its own RegisterDefaultCommands and RegisterDefaultQueries.
// Those methods are deliberately not salvaged: a second registration surface is
// the exact defect that register entry R-0003 recorded.
public static class EngineHosting
{
    public static EngineKit CreateDefault(IGeometryBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        var document = new Document();
        var commands = new CommandRegistry();
        var queries = new QueryRegistry();
        HandlerCatalog.RegisterAll(commands, queries);

        return new EngineKit(document, commands, queries, new InMemoryEventSink(), backend);
    }
}

// The kit holds the parts. A host composes a bus from them.
//
// The branch version exposed the concrete backend type. This version exposes
// IGeometryBackend, because ADR-0014 section 4 gives each host two
// implementations to choose between, therefore the choice is polymorphic and the
// kit must carry it as such.
public sealed record EngineKit(
    Document Document,
    CommandRegistry CommandRegistry,
    QueryRegistry QueryRegistry,
    InMemoryEventSink Events,
    IGeometryBackend Backend)
{
    // The sink parameter is the decorated sink when a host decorates one, and
    // Events when a host does not.
    public CommandBus CreateCommandBus(IEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        return new CommandBus(Document, CommandRegistry, sink, Backend);
    }

    public CommandBus CreateCommandBus() => CreateCommandBus(Events);

    public QueryBus CreateQueryBus() => new(Document, QueryRegistry, Backend);
}
