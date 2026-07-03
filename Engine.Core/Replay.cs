using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Geometry;

namespace Engine.Core;

// Per TASK-0001: Replay(IEnumerable<Command>) → Document reconstructs an equivalent Document
// from a log. Equivalence is asserted modulo Timestamp and DocumentId.
// Per ADR-0012 §2: backends are caches; replay against a fresh backend
// reconstructs Document state and backend state in lockstep.
public static class Replay
{
    public static async Task<ReplayResult> ReplayLog(
        IEnumerable<Command> log,
        CommandRegistry registry,
        IGeometryBackend? backend = null,
        CancellationToken ct = default)
    {
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (registry is null) throw new ArgumentNullException(nameof(registry));

        var document = new Document();
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(document, registry, sink, backend ?? NullGeometryBackend.Instance);

        foreach (var command in log)
        {
            await bus.Apply(command, ct).ConfigureAwait(false);
        }

        return new ReplayResult(document, sink);
    }
}

public sealed record ReplayResult(Document Document, IEventSink Events);
