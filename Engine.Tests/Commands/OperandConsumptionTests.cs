using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Contracts.Schema;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;

namespace Engine.Tests.Commands;

// ADR-0021, TASK-0037: an operation consumes its operands, and Document.Bodies
// holds the live bodies only. Before TASK-0037 each operation added a body and
// kept its operands, so the first demonstration left four bodies.
//
// The tests use a recording backend and not the native one, so that they run
// with no native library and can count each call into the backend.
public class OperandConsumptionTests
{
    [Fact]
    public async Task After_The_First_Demonstration_The_Document_Holds_The_Cut_Solid_Only()
    {
        var (session, _, _) = NewSession();
        var a = new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 };
        var b = new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 };
        var move = new TranslateCommand { BodyId = b.CommandId, Dx = 5, Dy = 0, Dz = 0 };
        var cut = new SubtractCommand { MinuendBodyId = a.CommandId, SubtrahendBodyId = move.CommandId };

        foreach (var command in new Command[] { a, b, move, cut })
            Assert.Equal(CommandStatus.Applied, (await session.Apply(command)).Status);

        var bodies = await session.Read((document, _) => document.Bodies.ToArray());
        var body = Assert.Single(bodies);
        Assert.Equal(cut.CommandId, body.Handle.Id);
    }

    [Fact]
    public async Task Translate_Consumes_Its_Source_Body()
    {
        var (session, _, _) = NewSession();
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var move = new TranslateCommand { BodyId = box.CommandId, Dx = 1, Dy = 0, Dz = 0 };
        await session.Apply(box);

        await session.Apply(move);

        var ids = await session.Read((document, _) => document.Bodies.Select(x => x.Handle.Id).ToArray());
        Assert.Equal([move.CommandId], ids);
    }

    [Fact]
    public async Task The_Events_Of_A_Subtract_Come_In_The_Order_Of_ADR_0021()
    {
        var (session, sink, _) = NewSession();
        var a = new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 };
        var b = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var cut = new SubtractCommand { MinuendBodyId = a.CommandId, SubtrahendBodyId = b.CommandId };
        await session.Apply(a);
        await session.Apply(b);

        await session.Apply(cut);

        var events = sink.Snapshot().Where(e => e.CauseCommandId == cut.CommandId).ToArray();
        Assert.Equal(["command.applied", "body.created", "body.consumed", "body.consumed"], events.Select(e => e.Kind));
        Assert.Equal(cut.CommandId, BodyId(events[1]));
        Assert.Equal(a.CommandId, BodyId(events[2]));
        Assert.Equal(b.CommandId, BodyId(events[3]));
        Assert.Equal(events[0].Seq + 3, events[3].Seq);
    }

    [Fact]
    public async Task A_Command_On_A_Consumed_Body_Is_Rejected_And_The_Backend_Gets_No_Call()
    {
        var (session, _, backend) = NewSession();
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var move = new TranslateCommand { BodyId = box.CommandId, Dx = 1, Dy = 0, Dz = 0 };
        await session.Apply(box);
        await session.Apply(move);
        var callsBefore = backend.Calls.Count;

        var again = await session.Apply(new TranslateCommand { BodyId = box.CommandId, Dx = 2, Dy = 0, Dz = 0 });
        var cut = await session.Apply(new SubtractCommand { MinuendBodyId = move.CommandId, SubtrahendBodyId = box.CommandId });

        Assert.Equal(CommandStatus.Rejected, again.Status);
        Assert.Equal("E-GEOM-BODY-NOT-FOUND", again.Error!.Code);
        Assert.Equal(CommandStatus.Rejected, cut.Status);
        Assert.Equal("E-GEOM-BODY-NOT-FOUND", cut.Error!.Code);
        Assert.Equal(callsBefore, backend.Calls.Count);
    }

    // The test records the choice of ADR-0021 item 2: a handler gives each handle
    // one time, so a subtract of a body from itself cannot consume it two times.
    // It is rejected before the backend, and the body stays live.
    [Fact]
    public async Task A_Subtract_Of_A_Body_From_Itself_Is_Rejected_And_The_Body_Stays()
    {
        var (session, _, backend) = NewSession();
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await session.Apply(box);
        var callsBefore = backend.Calls.Count;

        var result = await session.Apply(new SubtractCommand { MinuendBodyId = box.CommandId, SubtrahendBodyId = box.CommandId });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal("E-GEOM-INVALID-PARAM", result.Error!.Code);
        Assert.Equal(callsBefore, backend.Calls.Count);
        var ids = await session.Read((document, _) => document.Bodies.Select(x => x.Handle.Id).ToArray());
        Assert.Equal([box.CommandId], ids);
    }

    [Fact]
    public async Task A_Replay_Of_The_Log_Gives_The_Same_Live_Set()
    {
        var (session, _, _) = NewSession();
        var a = new CreateBoxCommand { SizeX = 3, SizeY = 3, SizeZ = 3 };
        var b = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var c = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var move = new TranslateCommand { BodyId = b.CommandId, Dx = 1, Dy = 0, Dz = 0 };
        var cut = new SubtractCommand { MinuendBodyId = a.CommandId, SubtrahendBodyId = move.CommandId };
        foreach (var command in new Command[] { a, b, c, move, cut })
            await session.Apply(command);
        var (log, live) = await session.Read((document, _) =>
            (document.Log.ToArray(), document.Bodies.Select(x => x.Handle.Id).OrderBy(id => id).ToArray()));

        var replay = await Replay.ReplayLog(log, NewRegistry(), new RecordingBackend());

        Assert.Equal(new[] { c.CommandId, cut.CommandId }.OrderBy(id => id), live);
        Assert.Equal(live, replay.Document.Bodies.Select(x => x.Handle.Id).OrderBy(id => id).ToArray());
    }

    [Fact]
    public async Task The_Reset_Snapshot_Lists_The_Live_Bodies_Only()
    {
        var (session, _, _) = NewSession();
        var a = new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 };
        var b = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var cut = new SubtractCommand { MinuendBodyId = a.CommandId, SubtrahendBodyId = b.CommandId };
        foreach (var command in new Command[] { a, b, cut })
            await session.Apply(command);

        var snapshot = await session.Read((document, _) => SnapshotProjector.Project(document, seq: 0));

        var entry = Assert.Single(snapshot.Bodies);
        Assert.Equal(cut.CommandId, entry.Handle);
    }

    // ADR-0021 item 2: a handler consumes only a live body, and it gives each
    // handle one time. The bus checks it before the commit, so a wrong handler
    // changes nothing in the Document.
    [Theory]
    [InlineData("not live")]
    [InlineData("two times")]
    public async Task The_Bus_Refuses_A_Wrong_Consumed_List_Before_It_Changes_The_Document(string fault)
    {
        var backend = new RecordingBackend();
        var kit = EngineHosting.CreateDefault(backend);
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var target = fault == "not live" ? Guid.NewGuid() : box.CommandId;
        kit.CommandRegistry.Register(new ConsumingHandler(target, times: fault == "two times" ? 2 : 1));
        var session = new DocumentSession(kit);
        await session.Apply(box);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => session.Apply(new ConsumeCommand()));

        Assert.Contains("ADR-0021", error.Message, StringComparison.Ordinal);
        var (version, ids) = await session.Read((document, _) =>
            (document.Version, document.Bodies.Select(x => x.Handle.Id).ToArray()));
        Assert.Equal(1, version);
        Assert.Equal([box.CommandId], ids);
    }

    private static Guid BodyId(EventRecord record)
        => (Guid)((IReadOnlyDictionary<string, object?>)record.Payload!)["bodyId"]!;

    private static (DocumentSession Session, InMemoryEventSink Sink, RecordingBackend Backend) NewSession()
    {
        var backend = new RecordingBackend();
        var kit = EngineHosting.CreateDefault(backend);
        return (new DocumentSession(kit), kit.Events, backend);
    }

    private static CommandRegistry NewRegistry()
    {
        var commands = new CommandRegistry();
        HandlerCatalog.RegisterAll(commands, new QueryRegistry());
        return commands;
    }

    // Each operation stores the result handle and records the call. It refuses
    // an unknown operand, as the native backend does.
    private sealed class RecordingBackend : IGeometryBackend, IMeshOps, ITransformOps, IBooleanOps
    {
        private readonly HashSet<Guid> _bodies = [];

        public List<string> Calls { get; } = [];

        public BackendCapabilities Capabilities
            => BackendCapabilities.Mesh | BackendCapabilities.Transform | BackendCapabilities.Booleans;

        public T? TryGet<T>() where T : class => this as T;

        public void CreateBox(BodyHandle handle, BoxParameters parameters) => Store("CreateBox", handle);

        public void Translate(BodyHandle result, BodyHandle source, double dx, double dy, double dz)
        {
            Require(source);
            Store("Translate", result);
        }

        public void Subtract(BodyHandle result, BodyHandle minuend, BodyHandle subtrahend)
        {
            Require(minuend);
            Require(subtrahend);
            Store("Subtract", result);
        }

        private void Require(BodyHandle handle)
        {
            if (!_bodies.Contains(handle.Id))
                throw new KeyNotFoundException($"The backend holds no body {handle.Id}.");
        }

        private void Store(string call, BodyHandle handle)
        {
            Calls.Add(call);
            if (!_bodies.Add(handle.Id))
                throw new InvalidOperationException($"The backend already holds body {handle.Id}.");
        }
    }

    private sealed record ConsumeCommand : Command
    {
        public override string Name => "TestConsume";
        public override int SchemaVersion => 1;
    }

    // A handler that breaks the rule of ADR-0021 item 2 on purpose.
    private sealed class ConsumingHandler(Guid target, int times) : ICommandHandler
    {
        public string CommandName => "TestConsume";
        public int SchemaVersion => 1;
        public IReadOnlyDictionary<string, FieldSchema> Parameters { get; } = new Dictionary<string, FieldSchema>();
        public IReadOnlyDictionary<string, FieldSchema> Outputs { get; } = new Dictionary<string, FieldSchema>();

        public Task<CommandHandlerResult> Handle(Command command, Document document, IGeometryBackend backend, CancellationToken ct)
            => Task.FromResult(CommandHandlerResult.Success(
                Engine.Contracts.Outputs.Empty,
                consumedBodies: Enumerable.Repeat(new BodyHandle(target), times).ToArray()));

        public Command Create(CommandInput input) => new ConsumeCommand();
    }
}
