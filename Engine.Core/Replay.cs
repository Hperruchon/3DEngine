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
// idempotency cache (finding E17). A CommandId that is in the log two times is
// applied two times only for a command that creates nothing; for a command that
// creates a body or a sketch item the second entry is a divergence (E31, E104).
//
// ADR-0015 item 6 (KD3, anti-objective 10): a replay of log entries uses the
// backend that each entry records, so an entry of another backend stops the replay
// before it applies. A list of commands with no entries, for example a fixture or a
// script, records no backend and replays on the backend that the caller gives.
//
// Finding E112 of the codebase review of 2026-10-31 (b) (TASK-0142): a replay applies each
// command as an entry of a log (CommandBus, a bus with no cache), also a command of a fixture or a
// script. So a rule that a live session has and an open does not have (E103, ApplyingEntry) does
// not act in a replay: a script can replay an arc with its start on its centre.
public static class Replay
{
    // Finding E30 of the codebase review of 2026-10-08 (TASK-0136): the backend was optional, and the
    // default null backend made each log with geometry diverge at its first body. The caller names it.
    public static Task<ReplayResult> ReplayLog(
        IEnumerable<LogEntry> log,
        CommandRegistry registry,
        IGeometryBackend backend,
        CancellationToken ct = default)
    {
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (backend is null) throw new ArgumentNullException(nameof(backend));

        return ReplayLog(Checked(log, backend.Identity), registry, backend, ct);
    }

    private static IEnumerable<Command> Checked(IEnumerable<LogEntry> log, BackendIdentity active)
    {
        var index = 0;
        foreach (var entry in log)
        {
            if (entry.Backend.Name != active.Name)
                throw new BackendMismatchException(index, entry, active);
            yield return entry.Command;
            index++;
        }
    }

    public static async Task<ReplayResult> ReplayLog(
        IEnumerable<Command> log,
        CommandRegistry registry,
        IGeometryBackend backend,
        CancellationToken ct = default)
    {
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (registry is null) throw new ArgumentNullException(nameof(registry));
        if (backend is null) throw new ArgumentNullException(nameof(backend));

        var document = new Document();
        var sink = new InMemoryEventSink();
        var bus = CommandBus.ForReplay(document, registry, sink, backend);

        var index = 0;
        foreach (var command in log)
        {
            CommandResult result;
            // Finding E28 of the codebase review of 2026-10-08 (TASK-0136): the bus catches a cancellation
            // only, so an exception of a handler left the replay with no entry and no command. It is a
            // divergence at that entry, and the exception is its inner exception.
            try
            {
                result = await bus.Apply(command, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new ReplayDivergenceException(index, command, ex);
            }
            // Finding E29 of the codebase review of 2026-10-08 (TASK-0135): the bus turns a cancellation
            // into Cancelled, and the replay reported it as a divergence of the log. A cancellation of
            // the caller stays a cancellation.
            if (result.Status == CommandStatus.Cancelled && ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);
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
            + (result.Error is { } error ? $" with {error.Code}: {ErrorText.Clause(error.Message)}" : string.Empty)
            + ". Each entry of a log was applied one time, so a replay must apply it too.")
    {
        Index = index;
        Command = command;
        Result = result;
    }

    public ReplayDivergenceException(int index, Command command, Exception exception)
        : base(
            $"The replay diverged at entry {index} of the log: {command.Name} {command.CommandId} threw "
            + $"{exception.GetType().Name}: {ErrorText.Clause(exception.Message)}. Each entry of a log was applied one time, so a "
            + "replay must apply it too.",
            exception)
    {
        Index = index;
        Command = command;
    }

    // The position of the entry in the log, from 0.
    public int Index { get; }

    public Command Command { get; }

    // Null when the entry threw; InnerException then holds the exception.
    public CommandResult? Result { get; }
}

// ADR-0015 item 6: the code E-IO-BACKEND-MISMATCH.
public sealed class BackendMismatchException : InvalidOperationException
{
    public BackendMismatchException(int index, LogEntry entry, BackendIdentity active)
        : base(
            $"{DiagnosticCodes.IoBackendMismatch}: entry {index} of the log ({entry.Command.Name} {entry.Command.CommandId}) "
            + $"records the backend '{entry.Backend.Name}', and the replay has the backend '{active.Name}'. A replay uses "
            + "the recorded backend (anti-objective 10).")
    {
        Index = index;
        Entry = entry;
    }

    // The position of the entry in the log, from 0.
    public int Index { get; }

    public LogEntry Entry { get; }
}

public sealed record ReplayResult(Document Document, IEventSink Events);

// Finding E31 of the codebase review of 2026-10-08 (TASK-0137): a message of an error ends with a
// full stop, and the text after it gave "..". The clause drops its own full stop. The replay and the
// open of a document file share it (finding E107 of the codebase review of 2026-10-31 (b), TASK-0142).
internal static class ErrorText
{
    internal static string Clause(string message) => message.TrimEnd().TrimEnd('.');
}
