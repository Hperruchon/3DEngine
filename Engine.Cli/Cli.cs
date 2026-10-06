using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
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
    internal const int ExitBackendUnavailable = 3;

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
        => RunAsync(args, stdout, stderr, CancellationToken.None, BackendOptions.Native).GetAwaiter().GetResult();

    internal static async Task<int> RunAsync(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct,
        BackendOptions? backend = null)
    {
        backend ??= BackendOptions.Native;

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
            "apply" => await Apply(rest, stdout, stderr, ct, backend).ConfigureAwait(false),
            "query" => await Query(rest, stdout, stderr, ct, backend).ConfigureAwait(false),
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
        CancellationToken ct,
        BackendOptions backend)
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

        // Per ADR-0016: find the handler, bind the parameters, then let the
        // handler build its command. No command name appears in this file.
        var session = BuildSession(backend, stderr);
        if (session is null)
            return ExitBackendUnavailable;

        if (!session.CommandRegistry.TryFind(name, DefaultSchemaVersion, out var handler))
        {
            // No sentinel command: produce the Rejected result client-side.
            // CommandBus.Apply requires a concrete Command; constructing one
            // for an unknown name would itself be the sentinel we are forbidden.
            JsonRenderer.WriteCommandResult(
                UnknownCommand(name),
                stdout);
            return ExitRejected;
        }

        var bound = ParameterBinder.Bind(handler.Parameters, AsRaw(parameters));
        if (!bound.IsSuccess)
            return InvalidUsage(stderr, bound.FirstMessage);

        var command = handler.Create(
            new CommandInput(bound.Values!, Guid.NewGuid(), ExpectedDocumentVersion: null));

        var result = await session.Apply(command, ct).ConfigureAwait(false);

        JsonRenderer.WriteCommandResult(result, stdout);
        return result.Status == CommandStatus.Applied ? ExitApplied : ExitRejected;
    }

    private static async Task<int> Query(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken ct,
        BackendOptions backend)
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

        // Per ADR-0016: same three steps as Apply. No query name appears here.
        var session = BuildSession(backend, stderr);
        if (session is null)
            return ExitBackendUnavailable;

        if (!session.QueryRegistry.TryFind(name, DefaultSchemaVersion, out var handler))
        {
            var unknown = new QueryResult<object>(
                QueryName: name,
                AsOfDocumentVersion: 0,
                Result: null,
                Diagnostics: Array.Empty<Diagnostic>(),
                Error: new ErrorDetail(
                    DiagnosticCodes.QueryUnknown,
                    $"No handler registered for query '{name}'."),
                DurationMs: 0);
            JsonRenderer.WriteQueryResult(unknown, stdout);
            return ExitRejected;
        }

        var bound = ParameterBinder.Bind(handler.Parameters, AsRaw(parameters));
        if (!bound.IsSuccess)
            return InvalidUsage(stderr, bound.FirstMessage);

        var query = handler.Create(new QueryInput(bound.Values!, Guid.NewGuid()));

        // The CLI renders one concrete result type. GetBoundingBox is the only
        // registered query, and its result is an Aabb. A second query type
        // needs a typed render path; see ADR-0016 "Next".
        var typed = await session.Query<Aabb>(query, ct).ConfigureAwait(false);
        JsonRenderer.WriteQueryResult(typed, stdout);
        return typed.Error is null ? ExitApplied : ExitRejected;

    }

    private static IReadOnlyDictionary<string, object?> AsRaw(Dictionary<string, string> parameters)
    {
        var raw = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in parameters)
            raw[key] = value;
        return raw;
    }

    private static CommandResult UnknownCommand(string name) => new(
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

    // Every registered handler comes from HandlerCatalog per ADR-0016, so the
    // CLI, the HTTP host and the canonical replay gate use one set. The CLI has
    // one caller and one command, and it still uses the session, so that each
    // host reaches the engine in the same way (TASK-0034).
    //
    // The CLI requires the native backend (TASK-0036). When its library does not
    // load, the CLI writes E-GEOM-BACKEND-INIT and stops before the command, with
    // the exit code 3. Until TASK-0036 it took the managed backend with no message,
    // which holds boxes only, and anti-objective 9 refuses that silent fallback.
    // The one-shot process reclaims the native backend on exit. This choice stays
    // here, because only a composition root may name Engine.Geometry.Manifold.
    private static DocumentSession? BuildSession(BackendOptions options, TextWriter stderr)
    {
        IGeometryBackend backend;
        if (options.UseManagedBackend)
        {
            backend = new InProcessMeshBackend();
        }
        else if (options.IsNativeAvailable())
        {
            backend = new ManifoldGeometryBackend();
        }
        else
        {
            stderr.WriteLine($"{DiagnosticCodes.GeomBackendInit}: {ManifoldGeometryBackend.UnavailableReason()}");
            return null;
        }

        return new DocumentSession(EngineHosting.CreateDefault(backend));
    }

    // The CLI dispatches by name only. It has no argument for a schema
    // version, therefore it asks for version 1. A second version of a command
    // needs a CLI argument; see ADR-0016 "Next".
    private const int DefaultSchemaVersion = 1;
}

// The backend of the command line. A person has one choice: the native backend,
// or a stop with E-GEOM-BACKEND-INIT. A test can select the managed backend, and a
// test can replace the probe to show the stop (TASK-0036). Neither choice is in the
// usage text, and neither has an argument.
internal sealed record BackendOptions(bool UseManagedBackend, Func<bool> IsNativeAvailable)
{
    public static BackendOptions Native { get; } = new(false, ManifoldGeometryBackend.IsNativeAvailable);

    public static BackendOptions ManagedForTests { get; } = new(true, () => false);
}
