using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

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
        Assert.NotNull(result.AppliedAtSeq);
        Assert.Equal(result.AppliedAtSeq, doc.Version);

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

        // Document log + materialized state are unchanged on rejection.
        // Document.Version is a runtime observation version (mirrors last emitted Seq),
        // not a successful-mutation counter — so it advances even on rejection.
        Assert.Empty(doc.Log);
        Assert.True(doc.Version > versionBefore);

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

        Assert.Equal(events[^1].Seq, doc.Version);
    }
}
