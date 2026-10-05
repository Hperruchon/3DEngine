using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;

namespace Engine.Tests;

public class CommandBusTests
{
    private static (Document doc, CommandRegistry reg, InMemoryEventSink sink, CommandBus bus) NewBusWithNoOp()
    {
        var doc = new Document();
        var reg = new CommandRegistry();
        reg.Register(new NoOpCommandHandler());
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(doc, reg, sink);
        return (doc, reg, sink, bus);
    }

    private sealed record UnknownCommand : Command
    {
        public override string Name => "Unknown";
        public override int SchemaVersion => 1;
    }

    [Fact]
    public async Task Apply_NoOpCommand_Returns_Applied_With_Echo_Output_And_Emits_Event()
    {
        var (doc, _, sink, bus) = NewBusWithNoOp();
        var command = new NoOpCommand { Echo = "hi" };

        var result = await bus.Apply(command);

        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.Null(result.Error);
        Assert.Equal(1, result.AppliedAtSeq);
        Assert.Equal(1, doc.Version);

        Assert.True(result.Outputs.TryGet<string>("echo", out var echo));
        Assert.Equal("hi", echo);

        var events = sink.Snapshot();
        Assert.Single(events);
        Assert.Equal("command.applied", events[0].Kind);
        Assert.Equal(result.AppliedAtSeq, events[0].Seq);
        Assert.Equal(command.CommandId, events[0].CauseCommandId);

        Assert.Single(doc.Log);
        Assert.Same(command, doc.Log[0]);
    }

    [Fact]
    public async Task Apply_Unknown_Command_Returns_Rejected_With_E_CMD_UNKNOWN()
    {
        var doc = new Document();
        var reg = new CommandRegistry();
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(doc, reg, sink);

        var versionBefore = doc.Version;
        var result = await bus.Apply(new UnknownCommand());

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.CommandUnknown, result.Error!.Code);
        Assert.Null(result.AppliedAtSeq);

        // A rejection changes neither the log nor the version (ADR-0020). Its event
        // still takes the next Seq.
        Assert.Empty(doc.Log);
        Assert.Equal(versionBefore, doc.Version);

        var events = sink.Snapshot();
        Assert.Single(events);
        Assert.Equal("command.rejected", events[0].Kind);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("world-with-dashes")]
    public async Task Applied_Implies_No_Error_And_Rejected_Implies_Error(string echo)
    {
        // Applied
        var (_, _, _, bus) = NewBusWithNoOp();
        var applied = await bus.Apply(new NoOpCommand { Echo = echo });
        Assert.Equal(CommandStatus.Applied, applied.Status);
        Assert.Null(applied.Error);

        // Rejected
        var doc2 = new Document();
        var bus2 = new CommandBus(doc2, new CommandRegistry(), new InMemoryEventSink());
        var rejected = await bus2.Apply(new NoOpCommand { Echo = echo });
        Assert.Equal(CommandStatus.Rejected, rejected.Status);
        Assert.NotNull(rejected.Error);
    }

    [Fact]
    public async Task Stale_ExpectedDocumentVersion_Is_Rejected_With_E_CMD_VERSION_STALE()
    {
        var (doc, _, _, bus) = NewBusWithNoOp();

        // First apply succeeds and bumps Version to 1.
        await bus.Apply(new NoOpCommand { Echo = "first" });
        Assert.Equal(1, doc.Version);

        // Second submission with stale expectation.
        var stale = new NoOpCommand { Echo = "stale", ExpectedDocumentVersion = 999 };
        var result = await bus.Apply(stale);

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.CommandVersionStale, result.Error!.Code);

        // Log is unchanged from the prior single applied command.
        Assert.Single(doc.Log);
    }

    [Fact]
    public async Task Apply_N_Commands_Yields_Monotonic_Sequence_And_Document_Version()
    {
        const int N = 5;
        var (doc, _, sink, bus) = NewBusWithNoOp();

        for (var i = 0; i < N; i++)
        {
            var r = await bus.Apply(new NoOpCommand { Echo = $"e{i}" });
            Assert.Equal(CommandStatus.Applied, r.Status);
        }

        Assert.Equal(N, doc.Log.Count);

        var events = sink.Snapshot();
        Assert.Equal(N, events.Count);

        for (var i = 0; i < N; i++)
            Assert.Equal(i + 1, events[i].Seq);

        Assert.Equal(N, doc.Version);
    }

    // TASK-0034, scope item 4 (finding E4). The sink stands for a host sink that
    // obeys the token. It cancels the token when it receives the first event of
    // the commit, which comes after the log append. Before the change the bus gave
    // the token of the caller to each append: the second append threw, and the log
    // held the command while the version and the events did not.
    [Fact]
    public async Task A_Cancellation_After_The_Log_Append_Leaves_Log_Bodies_Events_And_Version_In_Agreement()
    {
        using var cts = new CancellationTokenSource();
        var sink = new ObservingSink(record =>
        {
            if (record.Kind == "command.applied")
                cts.Cancel();
        });
        var (doc, bus) = NewBusWithCreateBox(sink);

        var result = await bus.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 }, cts.Token);

        Assert.True(cts.IsCancellationRequested);
        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.Single(doc.Log);
        Assert.Single(doc.Bodies);
        Assert.Equal(["command.applied", "body.created"], sink.Records.Select(r => r.Kind));
        Assert.Equal(2, sink.Records[^1].Seq);
        Assert.Equal(1, doc.Version);
        Assert.Equal(doc.Version, result.DocumentVersion);
    }

    // TASK-0034, scope item 7: change, then publish. A sink that throws loses an
    // event and never a part of the Document. The cache holds the result, so a
    // retry with the same CommandId does not run the handler a second time.
    // Before the change the version stayed at 0 and the cache was empty, and a
    // retry made the backend throw for a duplicate body.
    [Fact]
    public async Task A_Sink_That_Throws_Loses_An_Event_And_Never_A_Part_Of_The_Document()
    {
        var sink = new ObservingSink(record =>
        {
            if (record.Kind == "body.created")
                throw new InvalidOperationException("The sink failed.");
        });
        var (doc, bus) = NewBusWithCreateBox(sink);
        var command = new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.Apply(command));

        Assert.Single(doc.Log);
        Assert.Single(doc.Bodies);
        Assert.Equal(1, doc.Version);
        Assert.Equal(["command.applied", "body.created"], sink.Records.Select(r => r.Kind));

        sink.ActionEnabled = false;
        var retry = await bus.Apply(command);
        Assert.Equal(CommandStatus.Applied, retry.Status);
        Assert.Equal(1, retry.AppliedAtSeq);
        Assert.Single(doc.Log);

        var next = await bus.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        Assert.Equal(3, next.AppliedAtSeq);
        Assert.Equal(2, doc.Version);
    }

    // TASK-0034, scope item 8 (finding E10). The sequence counter belongs to the
    // bus, so a second bus on one Document started the sequence at 1 again.
    [Fact]
    public async Task A_Second_Bus_On_The_Document_Of_A_Session_Is_Refused()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var session = new DocumentSession(kit);
        var first = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });

        // A reference that leaves the session on purpose. Only a test does this.
        var document = await session.Read((d, _) => d);

        Assert.Throws<InvalidOperationException>(
            () => new CommandBus(document, kit.CommandRegistry, new InMemoryEventSink(), kit.Backend));

        var second = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        Assert.Equal(first.AppliedAtSeq + 2, second.AppliedAtSeq);
        Assert.Equal(["command.applied", "body.created", "command.applied", "body.created"],
            kit.Events.Snapshot().Select(r => r.Kind));
    }

    // TASK-0034, scope item 8 (finding E10), and TASK-0035. In TASK-0034 the
    // bus refused to move the version back, because Document.AdvanceVersion
    // accepted a lower value. TASK-0035 removed AdvanceVersion: the version is
    // the count of the log (ADR-0020), so no code can move it back. The test
    // keeps its purpose: after each result the version equals the count of the
    // log, and it never goes down.
    [Fact]
    public async Task The_Version_Is_The_Count_Of_The_Log_And_Never_Goes_Back()
    {
        var (doc, _, sink, bus) = NewBusWithNoOp();
        var versions = new List<long>();

        await bus.Apply(new NoOpCommand { Echo = "first" });
        versions.Add(doc.Version);
        await bus.Apply(new NoOpCommand { Echo = "stale", ExpectedDocumentVersion = 99 });
        versions.Add(doc.Version);
        await bus.Apply(new NoOpCommand { Echo = "second" });
        versions.Add(doc.Version);

        Assert.Equal([1L, 1L, 2L], versions);
        Assert.Equal(doc.Log.Count, doc.Version);
        Assert.Equal([1L, 2L, 3L], sink.Snapshot().Select(e => e.Seq));
    }

    private static (Document doc, CommandBus bus) NewBusWithCreateBox(IEventSink sink)
    {
        var doc = new Document();
        var reg = new CommandRegistry();
        reg.Register(new CreateBoxCommandHandler());
        return (doc, new CommandBus(doc, reg, sink, new InProcessMeshBackend()));
    }

    // Keeps each event, and then calls the action. A token that is already
    // cancelled makes it throw first, as a host sink that obeys the token does.
    private sealed class ObservingSink(Action<EventRecord> afterAppend) : IEventSink
    {
        public List<EventRecord> Records { get; } = [];

        public bool ActionEnabled { get; set; } = true;

        public Task Append(EventRecord record, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Records.Add(record);
            if (ActionEnabled)
                afterAppend(record);
            return Task.CompletedTask;
        }

        public IReadOnlyList<EventRecord> Snapshot() => Records;

        public int Count => Records.Count;
    }
}
