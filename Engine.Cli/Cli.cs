using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Cli;

// Entry surface for the headless CLI client.
// Per ADR-0002: every user-visible feature must be reachable through the CLI.
// Per ADR-0008 §6: two verbs — `apply` (commands) and `query` (queries).
public static class Cli
{
    internal const int ExitApplied = 0;
    internal const int ExitRejected = 1;
    internal const int ExitInvalidUsage = 2;

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
        => RunAsync(args, stdout, stderr, CancellationToken.None).GetAwaiter().GetResult();

    internal static async Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (args.Length == 0)
            return InvalidUsage(stderr, reason: null);

        var verb = args[0];
        var rest = args[1..];

        return verb switch
        {
            "help" => Help(stdout),
            "apply" => await Apply(rest, stdout, stderr, ct).ConfigureAwait(false),
            "query" => Query(rest, stdout, stderr),
            _ => InvalidUsage(stderr, $"Unknown verb: {verb}"),
        };
    }

    private static int Help(TextWriter stdout)
    {
        stdout.Write(Usage.Text);
        return ExitApplied;
    }

    private static int InvalidUsage(TextWriter stderr, string? reason)
    {
        if (reason is not null)
            stderr.WriteLine(reason);
        stderr.Write(Usage.Text);
        return ExitInvalidUsage;
    }

    private static async Task<int> Apply(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct)
    {
        if (args.Length == 0)
            return InvalidUsage(stderr, "apply requires a command name.");

        var name = args[0];
        Dictionary<string, string> parameters;
        try
        {
            parameters = ArgParser.ParseParams(args[1..]);
        }
        catch (ArgParseException ex)
        {
            return InvalidUsage(stderr, ex.Message);
        }

        CommandResult result;
        if (name == "NoOp")
        {
            if (!parameters.TryGetValue("echo", out var echo))
                return InvalidUsage(stderr, "NoOp requires --param echo=<value>.");

            var (bus, _) = BuildEngine();
            result = await bus.Apply(new NoOpCommand { Echo = echo }, ct).ConfigureAwait(false);
        }
        else
        {
            // No sentinel command: produce the Rejected result client-side.
            // CommandBus.Apply requires a concrete Command; constructing one
            // for an unknown name would itself be the sentinel we are forbidden.
            result = new CommandResult(
                CommandId: Guid.NewGuid(),
                CommandName: name,
                Status: CommandStatus.Rejected,
                AppliedAtSeq: null,
                DocumentVersion: 0,
                Outputs: Outputs.Empty,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: new ErrorDetail(
                    DiagnosticCodes.CommandUnknown,
                    $"No handler registered for command '{name}'."),
                DurationMs: 0);
        }

        JsonRenderer.WriteCommandResult(result, stdout);
        return result.Status == CommandStatus.Applied ? ExitApplied : ExitRejected;
    }

    private static int Query(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0)
            return InvalidUsage(stderr, "query requires a query name.");

        var name = args[0];
        try
        {
            ArgParser.ParseParams(args[1..]);
        }
        catch (ArgParseException ex)
        {
            return InvalidUsage(stderr, ex.Message);
        }

        // Query registry is empty in P1. Same rationale as Apply for the
        // unknown branch: Query is abstract; without a sentinel we cannot
        // submit through QueryBus. We produce the Rejected result directly.
        var result = new QueryResult<object>(
            QueryName: name,
            AsOfDocumentVersion: 0,
            Result: null,
            Diagnostics: Array.Empty<Diagnostic>(),
            Error: new ErrorDetail(
                DiagnosticCodes.QueryUnknown,
                $"No handler registered for query '{name}'."),
            DurationMs: 0);

        JsonRenderer.WriteQueryResult(result, stdout);
        return ExitRejected;
    }

    private static (CommandBus Commands, QueryBus Queries) BuildEngine()
    {
        var document = new Document();
        var commandRegistry = new CommandRegistry();
        commandRegistry.Register(new NoOpCommandHandler());
        var sink = new InMemoryEventSink();
        var commandBus = new CommandBus(document, commandRegistry, sink);
        var queryBus = new QueryBus(document, new QueryRegistry());
        return (commandBus, queryBus);
    }
}
