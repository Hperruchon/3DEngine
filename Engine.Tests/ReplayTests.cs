using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Tests;

public class ReplayTests
{
    private static (Document doc, InMemoryEventSink sink, CommandBus bus, CommandRegistry reg) NewBus()
    {
        var doc = new Document();
        var reg = new CommandRegistry();
        reg.Register(new NoOpCommandHandler());
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(doc, reg, sink);
        return (doc, sink, bus, reg);
    }

    [Fact]
    public async Task Replaying_Log_Reconstructs_Equivalent_Document()
    {
        var (originalDoc, _, originalBus, _) = NewBus();
        var inputs = new[] { "alpha", "beta", "gamma" };
        foreach (var echo in inputs)
            await originalBus.Apply(new NoOpCommand { Echo = echo });

        // Replay using a fresh registry of the same shape.
        var replayRegistry = new CommandRegistry();
        replayRegistry.Register(new NoOpCommandHandler());
        var replay = await Replay.ReplayLog(originalDoc.Log, replayRegistry);

        Assert.Equal(originalDoc.Log.Count, replay.Document.Log.Count);
        for (var i = 0; i < originalDoc.Log.Count; i++)
            Assert.Equal(originalDoc.Log[i].CommandId, replay.Document.Log[i].CommandId);

        Assert.Equal(originalDoc.Version, replay.Document.Version);
    }

    [Fact]
    public async Task Replay_Emits_Same_Event_Sequence_Modulo_Timestamp_And_DocumentId()
    {
        var (_, originalSink, originalBus, _) = NewBus();
        var inputs = new[] { "one", "two", "three", "four" };
        var commands = new List<Command>();
        foreach (var echo in inputs)
        {
            var cmd = new NoOpCommand { Echo = echo };
            commands.Add(cmd);
            await originalBus.Apply(cmd);
        }

        var replayRegistry = new CommandRegistry();
        replayRegistry.Register(new NoOpCommandHandler());
        var replay = await Replay.ReplayLog(commands, replayRegistry);

        var originalEvents = originalSink.Snapshot();
        var replayEvents = replay.Events.Snapshot();

        Assert.Equal(originalEvents.Count, replayEvents.Count);

        for (var i = 0; i < originalEvents.Count; i++)
        {
            Assert.Equal(originalEvents[i].Seq, replayEvents[i].Seq);
            Assert.Equal(originalEvents[i].Kind, replayEvents[i].Kind);
            Assert.Equal(originalEvents[i].CauseCommandId, replayEvents[i].CauseCommandId);
            // Timestamp and DocumentId may differ — explicitly excluded by the spec.
        }
    }
}
