using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests.Commands;

// Three findings of the codebase review of 2026-10-08 that register entry R-0036 held (TASK-0135):
// E26, the log of the Document; E29, a cancellation of a replay; E32, a query of a body that the
// backend does not hold.
public sealed class EngineDebtTests
{
    // E26: Log returned the list itself, and a cast to List and a Clear moved the version from 2 to 0.
    [Fact]
    public void The_Log_Is_A_Read_Only_View()
    {
        var document = new Document();

        Assert.IsNotType<List<LogEntry>>(document.Log);
        Assert.True(((ICollection<LogEntry>)document.Log).IsReadOnly);
    }

    // E29: a handler that sees the cancellation gives Cancelled, and the replay reported a divergence of
    // the log. It now ends with the cancellation of the caller.
    [Fact]
    public async Task A_Cancellation_Of_A_Replay_Is_A_Cancellation()
    {
        using var cancel = new CancellationTokenSource();
        var registry = new CommandRegistry();
        registry.Register(new CancellingHandler(cancel));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Replay.ReplayLog(new Command[] { new CancellingCommand() }, registry, NullGeometryBackend.Instance, cancel.Token));
    }

    // E28 (TASK-0136): an exception of a handler left the replay raw, with no entry and no command. It is
    // a divergence at its entry.
    [Fact]
    public async Task An_Exception_Of_A_Handler_In_A_Replay_Is_A_Divergence()
    {
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        registry.Register(new ThrowingHandler());
        var throwing = new ThrowingCommand();

        var divergence = await Assert.ThrowsAsync<ReplayDivergenceException>(
            () => Replay.ReplayLog(new Command[] { new NoOpCommand { Echo = "a" }, throwing }, registry, NullGeometryBackend.Instance));

        Assert.Equal(1, divergence.Index);
        Assert.Same(throwing, divergence.Command);
        Assert.Null(divergence.Result);
        Assert.IsType<InvalidCastException>(divergence.InnerException);
    }

    // E30 (TASK-0136): the backend of a replay was optional, and the null backend made each log with
    // geometry diverge. The caller names it, and null is refused.
    [Fact]
    public async Task A_Replay_Needs_A_Backend()
    {
        var registry = new CommandRegistry();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Replay.ReplayLog(Array.Empty<Command>(), registry, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Replay.ReplayLog(Array.Empty<LogEntry>(), registry, null!));
    }

    // E32: a body that the backend does not hold threw KeyNotFoundException through the query bus, and a
    // host answered HTTP 500. The two queries of geometry give E-GEOM-NATIVE-OP.
    [Fact]
    public async Task A_Query_Of_A_Body_That_The_Backend_Does_Not_Hold_Gives_An_Error()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new ForgettingBackend()));
        var box = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var body = (Guid)box.Outputs.Values["bodyId"]!;

        var bounds = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        var mesh = await session.Query<Tessellation>(new GetTessellationQuery { BodyId = body });

        Assert.Equal(DiagnosticCodes.GeomNativeOp, bounds.Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomNativeOp, mesh.Error?.Code);
    }

    private sealed record CancellingCommand : Command
    {
        public override string Name => "TestCancelling";
        public override int SchemaVersion => 1;
    }

    // Cancels the source of the caller and then observes it, as a long command would.
    private sealed class CancellingHandler(CancellationTokenSource cancel) : ICommandHandler
    {
        public string CommandName => "TestCancelling";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => throw new NotSupportedException();

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
        {
            cancel.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(CommandHandlerResult.Success(new Outputs(new Dictionary<string, object?>())));
        }
    }

    private sealed record ThrowingCommand : Command
    {
        public override string Name => "TestThrowing";
        public override int SchemaVersion => 1;
    }

    // Throws an exception that is not a cancellation, as the review agent of E28 did.
    private sealed class ThrowingHandler : ICommandHandler
    {
        public string CommandName => "TestThrowing";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Command Create(CommandInput input) => throw new NotSupportedException();

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
            => throw new InvalidCastException("The handler fails.");
    }

    // A backend that makes a box and then holds nothing, as after a failure of the native library.
    private sealed class ForgettingBackend : IGeometryBackend, IMeshOps, IGeometryQuery, ITessellationOps
    {
        public BackendIdentity Identity { get; } = new("test", "1");

        public BackendCapabilities Capabilities => BackendCapabilities.Mesh | BackendCapabilities.Query | BackendCapabilities.Tessellation;

        public T? TryGet<T>() where T : class => this as T;

        public void CreateBox(BodyHandle handle, BoxParameters parameters)
        {
        }

        public Aabb GetBoundingBox(BodyHandle handle) => throw new KeyNotFoundException($"No body {handle.Id}.");

        public Tessellation Tessellate(BodyHandle handle) => throw new KeyNotFoundException($"No body {handle.Id}.");
    }
}
