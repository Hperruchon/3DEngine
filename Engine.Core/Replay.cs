using Engine.Contracts;

namespace Engine.Core;

// Per TASK-0001: Replay(IEnumerable<Command>) → Document reconstructs an equivalent Document
// from a log. Equivalence is asserted modulo Timestamp and DocumentId.
public static class Replay
{
    public static async Task<ReplayResult> ReplayLog(
        IEnumerable<Command> log,
        CommandRegistry registry,
        CancellationToken ct = default)
    {
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (registry is null) throw new ArgumentNullException(nameof(registry));

        var document = new Document();
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(document, registry, sink);

        foreach (var command in log)
        {
            await bus.Apply(command, ct).ConfigureAwait(false);
        }

        return new ReplayResult(document, sink);
    }
}

public sealed record ReplayResult(Document Document, IEventSink Events);
