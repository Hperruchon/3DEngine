using System.Text.Json.Nodes;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;

namespace Engine.Tests.Commands;

// Three findings of the codebase review of 2026-10-08 that register entry R-0036 held (TASK-0135):
// E26, the log of the Document; E29, a cancellation of a replay; E32, a query of a body that the
// backend does not hold. The later tasks add the next forms (TASK-0136, TASK-0137, TASK-0142).
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
        Assert.DoesNotContain("..", divergence.Message, StringComparison.Ordinal);
    }

    // E31 (TASK-0137): the comments of the backends said that the idempotency cache stops each duplicate.
    // A replay has no cache, and a second box with one identifier is a divergence with E-GEOM-NATIVE-OP.
    [Fact]
    public async Task A_Box_That_The_Log_Holds_Two_Times_Is_A_Divergence()
    {
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var registry = new CommandRegistry();
        registry.Register(new CreateBoxCommandHandler());

        var divergence = await Assert.ThrowsAsync<ReplayDivergenceException>(
            () => Replay.ReplayLog(new Command[] { box, box }, registry, new InProcessMeshBackend()));

        Assert.Equal(1, divergence.Index);
        Assert.Equal(DiagnosticCodes.GeomNativeOp, divergence.Result?.Error?.Code);
        Assert.DoesNotContain("..", divergence.Message, StringComparison.Ordinal);
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

    // E106 (TASK-0142): the open of a file reported a cancellation of the caller as a load failure, as
    // the replay did before E29.
    [Fact]
    public async Task A_Cancellation_Of_An_Open_Is_A_Cancellation()
    {
        var folder = NewFolder();
        try
        {
            var path = Path.Combine(folder, "cancel" + DocumentFile.Extension);
            var writer = NewSession();
            DocumentFile.Create(writer, path);
            await writer.Apply(new NoOpCommand { Echo = "one" });
            writer.Close();
            var lines = File.ReadAllLines(path);
            var entry = JsonNode.Parse(lines[1])!;
            entry["command"]!["name"] = "TestCancelling";
            entry["command"]!["parameters"] = new JsonObject();
            File.WriteAllLines(path, [lines[0], entry.ToJsonString()]);

            using var cancel = new CancellationTokenSource();
            var commands = new CommandRegistry();
            var queries = new QueryRegistry();
            HandlerCatalog.RegisterAll(commands, queries);
            commands.Register(new CancellingHandler(cancel));
            var session = new DocumentSession(new EngineKit(new Document(), commands, queries, new InMemoryEventSink(), new InProcessMeshBackend()));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DocumentFile.Open(session, path, cancel.Token));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // E107 (TASK-0142): the message of an open put ". Each entry" after a message that ends with a full
    // stop. The open and the replay share the clause now.
    [Fact]
    public async Task The_Message_Of_An_Open_Has_One_Full_Stop()
    {
        var folder = NewFolder();
        try
        {
            var path = Path.Combine(folder, "twice" + DocumentFile.Extension);
            var writer = NewSession();
            DocumentFile.Create(writer, path);
            await writer.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
            writer.Close();
            var lines = File.ReadAllLines(path);
            var second = JsonNode.Parse(lines[1])!;
            second["version"] = 2;
            File.WriteAllLines(path, [lines[0], lines[1], second.ToJsonString()]);

            var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(NewSession(), path));

            Assert.Contains(DiagnosticCodes.GeomNativeOp, error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("..", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // E108 (TASK-0142): the add of a circle, the add of a constraint 'radius' and the change of its value
    // compared a radius with 0, and the solver says that a radius of 1e-9 or less is no radius (E102).
    [Fact]
    public async Task A_Live_Radius_At_The_Noise_Level_Is_Refused()
    {
        var session = NewSession();
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var centre = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 3, Y = 3 }, "pointId");
        var circle = await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 2 }, "circleId");
        var radius = await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "radius", First = circle, Value = 2 }, "constraintId");

        var small = await session.Apply(new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 5e-10 });
        var constraint = await session.Apply(new AddSketchConstraintCommand { SketchId = sketch, Kind = "radius", First = circle, Value = 5e-10 });
        var change = await session.Apply(new SetSketchConstraintValueCommand { SketchId = sketch, ConstraintId = radius, Value = 5e-10 });

        Assert.Equal(DiagnosticCodes.SketchCurveInvalid, small.Error?.Code);
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, constraint.Error?.Code);
        Assert.Equal(DiagnosticCodes.SketchConstraintInvalid, change.Error?.Code);
        Assert.Contains("greater than", constraint.Error?.Message, StringComparison.Ordinal);
        Assert.Contains("greater than", change.Error?.Message, StringComparison.Ordinal);
    }

    // E108 (TASK-0142): an entry of a log keeps the rule of 0, because an earlier version applied such a
    // circle (E103).
    [Fact]
    public async Task An_Entry_With_A_Radius_At_The_Noise_Level_Still_Applies()
    {
        var sketch = new CreateSketchCommand { DatumId = Datums.XY.Id };
        var centre = new AddSketchPointCommand { SketchId = sketch.CommandId, X = 3, Y = 3 };
        var circle = new AddSketchCircleCommand { SketchId = sketch.CommandId, CenterPointId = centre.CommandId, Radius = 5e-10 };

        var replay = await Replay.ReplayLog(new Command[] { sketch, centre, circle }, Registry(), new InProcessMeshBackend());

        Assert.Equal(3, replay.Document.Log.Count);
    }

    // E109 (TASK-0142): the reader of a document file took the last value of a repeated member, which the
    // writer cannot write. It refuses the line.
    [Fact]
    public async Task A_Repeated_Member_Of_A_File_Line_Is_Refused()
    {
        var folder = NewFolder();
        try
        {
            var path = Path.Combine(folder, "repeated" + DocumentFile.Extension);
            var writer = NewSession();
            DocumentFile.Create(writer, path);
            await writer.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
            await writer.Apply(new NoOpCommand { Echo = "last" });
            writer.Close();
            var lines = File.ReadAllLines(path);
            var edited = lines[1].Replace("\"sizeX\":1", "\"sizeX\":1,\"sizeX\":7", StringComparison.Ordinal);
            Assert.NotEqual(lines[1], edited);
            File.WriteAllLines(path, [lines[0], edited, lines[2]]);

            var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(NewSession(), path));

            Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // E111 (TASK-0142): a host took the name and the version of its backend as two free strings, and a
    // test host gave "test" for a backend that records "1". The host reads the identity.
    [Fact]
    public void A_Host_Gives_The_Identity_Of_Its_Backend()
    {
        var backend = new InProcessMeshBackend();
        using var host = new EngineHost(backend);

        Assert.Equal(backend.Identity.Name, host.BackendName);
        Assert.Equal(backend.Identity.Version, host.BackendVersion);
    }

    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static CommandRegistry Registry()
    {
        var commands = new CommandRegistry();
        HandlerCatalog.RegisterAll(commands, new QueryRegistry());
        return commands;
    }

    private static string NewFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "engine-debt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Applied, result.Status);
        return (Guid)result.Outputs.Values[output]!;
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

        public Command Create(CommandInput input) => new CancellingCommand { CommandId = input.CommandId };

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
