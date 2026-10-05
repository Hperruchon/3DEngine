using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Geometry;

namespace Engine.Core;

// Per TASK-0001: Replay(IEnumerable<Command>) → Document reconstructs an equivalent Document
// from a log. Equivalence is asserted modulo Timestamp and DocumentId.
// Per ADR-0012 §2: backends are caches; replay against a fresh backend
// reconstructs Document state and backend state in lockstep.
//
// Each entry of a log was applied one time, so a result that is not Applied is
// a divergence, and the replay stops there with an exception that names the
// entry, the command and the error. Until TASK-0035 the replay discarded each
// result and returned a Document that looked correct (finding E7 of the codebase
// review of 2026-10-04). The replay also applies each entry, with no
// idempotency cache, so a CommandId that is in the log two times is applied two
// times (finding E17).
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
        var bus = CommandBus.ForReplay(document, registry, sink, backend ?? NullGeometryBackend.Instance);

        var index = 0;
        foreach (var command in log)
        {
            var result = await bus.Apply(command, ct).ConfigureAwait(false);
            if (result.Status != CommandStatus.Applied)
                throw new ReplayDivergenceException(index, command, result);
            index++;
        }

        return new ReplayResult(document, sink);
    }
}

public sealed class ReplayDivergenceException : InvalidOperationException
{
    public ReplayDivergenceException(int index, Command command, CommandResult result)
        : base(
            $"The replay diverged at entry {index} of the log: {command.Name} {command.CommandId} gave "
            + $"{result.Status}"
            + (result.Error is { } error ? $" with {error.Code}: {error.Message}" : string.Empty)
            + ". Each entry of a log was applied one time, so a replay must apply it too.")
    {
        Index = index;
        Command = command;
        Result = result;
    }

    // The position of the entry in the log, from 0.
    public int Index { get; }

    public Command Command { get; }

    public CommandResult Result { get; }
}

public sealed record ReplayResult(Document Document, IEventSink Events);
