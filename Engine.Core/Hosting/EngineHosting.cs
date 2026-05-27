using Engine.Contracts;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;

namespace Engine.Core.Hosting;

// Shared engine-wiring factory. Eliminates the duplicate bring-up
// sequences that lived in Engine.Cli/Cli.BuildEngine and
// Engine.Api.Http/EngineHost (TASK-0013). Both clients now consume the
// same set of defaults (Document, registries with V1.x handlers, in-memory
// event sink, InProcessMeshBackend per ADR-0012 §7).
//
// The factory does NOT construct CommandBus/QueryBus — callers compose
// them from the kit. Rationale: the HTTP host wraps Events in
// BroadcastingEventSink (TASK-0010) before passing to the bus; the CLI
// uses Events directly. Returning a bus from the factory would force a
// single sink choice and defeat the abstraction.
//
// Adding a new default command/query handler updates one place: this
// file's RegisterDefaultCommands / RegisterDefaultQueries.
public static class EngineHosting
{
    public static EngineKit CreateDefault()
    {
        var document = new Document();
        var commandRegistry = new CommandRegistry();
        RegisterDefaultCommands(commandRegistry);
        var queryRegistry = new QueryRegistry();
        RegisterDefaultQueries(queryRegistry);
        var events = new InMemoryEventSink();
        var backend = new InProcessMeshBackend();
        return new EngineKit(document, commandRegistry, queryRegistry, events, backend);
    }

    // Exposed separately so a host that already has a registry (perhaps
    // with host-specific handlers added) can call this to layer in the
    // V1.x defaults.
    public static void RegisterDefaultCommands(CommandRegistry registry)
    {
        if (registry is null) throw new ArgumentNullException(nameof(registry));
        registry.Register(new NoOpCommandHandler());
        registry.Register(new CreateBoxCommandHandler());
    }

    public static void RegisterDefaultQueries(QueryRegistry registry)
    {
        if (registry is null) throw new ArgumentNullException(nameof(registry));
        registry.Register(new GetBoundingBoxQueryHandler());
    }
}

// EngineKit exposes concrete types (InProcessMeshBackend, InMemoryEventSink)
// rather than interfaces. Rationale per TASK-0013: the kit is the *default*
// wiring; hosts that want polymorphic access have the concrete instance
// and can pass it where the interface is wanted. Premature abstraction is
// rejected per ADR-0012's "the capability TryGet<T>() already does the job."
public sealed record EngineKit(
    Document Document,
    CommandRegistry CommandRegistry,
    QueryRegistry QueryRegistry,
    InMemoryEventSink Events,
    InProcessMeshBackend Backend);
