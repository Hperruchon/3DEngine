using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Api.Http;

// Single in-process engine owned by the host. Lives for the lifetime of the
// host process; restart resets state. Per V1 clamp "No persistence — in-memory
// only until persistence ADR + TASK."
//
// Mirrors the engine wiring in Engine.Cli (TASK-0002 §BuildEngine) and in the
// in-process bus tests (TASK-0001). Only NoOp is registered; query registry
// is empty (TASK-0001 / TASK-0002).
internal sealed class EngineHost
{
    public Document Document { get; }
    public CommandRegistry CommandRegistry { get; }
    public QueryRegistry QueryRegistry { get; }
    public CommandBus CommandBus { get; }
    public QueryBus QueryBus { get; }
    public InMemoryEventSink Events { get; }

    public EngineHost()
    {
        Document = new Document();
        CommandRegistry = new CommandRegistry();
        CommandRegistry.Register(new NoOpCommandHandler());
        QueryRegistry = new QueryRegistry();
        Events = new InMemoryEventSink();
        CommandBus = new CommandBus(Document, CommandRegistry, Events);
        QueryBus = new QueryBus(Document, QueryRegistry);
    }
}
