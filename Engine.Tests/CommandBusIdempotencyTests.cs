using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Tests;

public class CommandBusIdempotencyTests
{
    private static (Document doc, InMemoryEventSink sink, CommandBus bus) NewBusWithNoOp()
    {
        var doc = new Document();
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(doc, registry, sink);
        return (doc, sink, bus);
    }

    private sealed record UnknownCommand : Command
    {
        public override string Name => "Unknown";
        public override int SchemaVersion => 1;
    }

    [Fact]
    public async Task Applying_Same_CommandId_Twice_Returns_Cached_Result_With_No_New_Event_Or_Log_Entry()
    {
        var (doc, sink, bus) = NewBusWithNoOp();
        var command = new NoOpCommand
        {
            CommandId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
            Echo = "first",
        };

        var first = await bus.Apply(command);
        var versionAfterFirst = doc.Version;
        var eventsAfterFirst = sink.Snapshot().Count;
        var logCountAfterFirst = doc.Log.Count;

        var second = await bus.Apply(command);

        Assert.Equal(CommandStatus.Applied, second.Status);
        Assert.Equal(first.AppliedAtSeq, second.AppliedAtSeq);
        Assert.Equal(first.DocumentVersion, second.DocumentVersion);
        Assert.Equal(versionAfterFirst, doc.Version);
        Assert.Equal(eventsAfterFirst, sink.Snapshot().Count);
        Assert.Equal(logCountAfterFirst, doc.Log.Count);
    }

    [Fact]
    public async Task Applying_Same_CommandId_Twice_Returns_Identical_AppliedAtSeq_And_DurationMs()
    {
        var (_, _, bus) = NewBusWithNoOp();
        var command = new NoOpCommand
        {
            CommandId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
            Echo = "trace",
        };

        var first = await bus.Apply(command);
        var second = await bus.Apply(command);

        Assert.Equal(first.AppliedAtSeq, second.AppliedAtSeq);
        Assert.Equal(first.DurationMs, second.DurationMs);
        Assert.Equal(first.DocumentVersion, second.DocumentVersion);
        Assert.True(first.Outputs.TryGet<string>("echo", out var firstEcho));
        Assert.True(second.Outputs.TryGet<string>("echo", out var secondEcho));
        Assert.Equal(firstEcho, secondEcho);
    }

    [Fact]
    public async Task Rejected_Command_Is_Cached_And_Returns_Cached_Rejection_On_Duplicate()
    {
        var (doc, sink, bus) = NewBusWithNoOp();
        var command = new UnknownCommand
        {
            CommandId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
        };

        var first = await bus.Apply(command);
        Assert.Equal(CommandStatus.Rejected, first.Status);
        Assert.Equal(DiagnosticCodes.CommandUnknown, first.Error!.Code);
        var versionAfterFirst = doc.Version;
        var eventsAfterFirst = sink.Snapshot().Count;

        var second = await bus.Apply(command);

        Assert.Equal(CommandStatus.Rejected, second.Status);
        Assert.Equal(DiagnosticCodes.CommandUnknown, second.Error!.Code);
        Assert.Equal(first.DocumentVersion, second.DocumentVersion);
        Assert.Equal(versionAfterFirst, doc.Version);
        Assert.Equal(eventsAfterFirst, sink.Snapshot().Count);
    }
}
