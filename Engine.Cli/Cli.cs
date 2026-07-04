using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;

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
            "query" => await Query(rest, stdout, stderr, ct).ConfigureAwait(false),
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

        Command? command;
        switch (name)
        {
            case "NoOp":
                if (!parameters.TryGetValue("echo", out var echo))
                    return InvalidUsage(stderr, "NoOp requires --param echo=<value>.");
                command = new NoOpCommand { Echo = echo };
                break;

            case "CreateBox":
                if (!TryParseRequiredDouble(parameters, "sizeX", stderr, out var sx)) return ExitInvalidUsage;
                if (!TryParseRequiredDouble(parameters, "sizeY", stderr, out var sy)) return ExitInvalidUsage;
                if (!TryParseRequiredDouble(parameters, "sizeZ", stderr, out var sz)) return ExitInvalidUsage;
                command = new CreateBoxCommand { SizeX = sx, SizeY = sy, SizeZ = sz };
                break;

            default:
                command = null;
                break;
        }

        CommandResult result;
        if (command is not null)
        {
            var (bus, _) = BuildEngine();
            result = await bus.Apply(command, ct).ConfigureAwait(false);
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

    private static async Task<int> Query(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct)
    {
        if (args.Length == 0)
            return InvalidUsage(stderr, "query requires a query name.");

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

        // GetBoundingBox is the only registered query. Without a preceding
        // CreateBox in this process the Document has no bodies — the query
        // returns a structured "body not found" rejection.
        if (name == "GetBoundingBox")
        {
            if (!parameters.TryGetValue("bodyId", out var idStr))
                return InvalidUsage(stderr, "GetBoundingBox requires --param bodyId=<guid>.");
            if (!Guid.TryParse(idStr, out var bodyId))
                return InvalidUsage(stderr, $"--param bodyId='{idStr}' is not a valid GUID.");

            var (_, queryBus) = BuildEngine();
            var typed = await queryBus.Query<Engine.Contracts.Geometry.Aabb>(
                new GetBoundingBoxQuery { BodyId = bodyId }, ct).ConfigureAwait(false);
            JsonRenderer.WriteQueryResult(typed, stdout);
            return typed.Error is null ? ExitApplied : ExitRejected;
        }

        // Unknown query: produce the Rejected result directly (same rationale
        // as Apply's unknown branch).
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

    private static bool TryParseRequiredDouble(
        Dictionary<string, string> parameters,
        string key,
        TextWriter stderr,
        out double value)
    {
        if (!parameters.TryGetValue(key, out var raw))
        {
            InvalidUsage(stderr, $"CreateBox requires --param {key}=<number>.");
            value = 0;
            return false;
        }
        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            InvalidUsage(stderr, $"--param {key}='{raw}' is not a valid number.");
            return false;
        }
        return true;
    }

    private static (CommandBus Commands, QueryBus Queries) BuildEngine()
    {
        var document = new Document();
        var commandRegistry = new CommandRegistry();
        commandRegistry.Register(new NoOpCommandHandler());
        commandRegistry.Register(new CreateBoxCommandHandler());
        var queryRegistry = new QueryRegistry();
        queryRegistry.Register(new GetBoundingBoxQueryHandler());
        var sink = new InMemoryEventSink();
        // Native Manifold when its library is loadable, else the managed stub so the
        // CLI runs on any platform (ADR-0014 §4). The one-shot process reclaims the
        // native backend on exit.
        IGeometryBackend backend = ManifoldGeometryBackend.IsNativeAvailable()
            ? new ManifoldGeometryBackend()
            : new InProcessMeshBackend();
        var commandBus = new CommandBus(document, commandRegistry, sink, backend);
        var queryBus = new QueryBus(document, queryRegistry, backend);
        return (commandBus, queryBus);
    }
}
