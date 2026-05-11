using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;

namespace Engine.Tests.ReplayDeterminism;

// Gate per CLAUDE.md: "The gate runs … replay determinism fixture."
// Locks observable Document + event state against a hand-authored baseline
// (ReplayDeterminismFixture). Per ADR-0005, equality is asserted modulo
// EventRecord.Timestamp / EventRecord.DocumentId (and Document.DocumentId /
// CreatedAt / UpdatedAt, which derive from the same nondeterministic sources).
public class ReplayDeterminismGateTests
{
    private static CommandRegistry NewRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        return registry;
    }

    [Fact]
    public async Task Replay_Of_Fixture_Matches_Expected_Document_And_Event_Sequence()
    {
        var result = await Replay.ReplayLog(ReplayDeterminismFixture.Commands, NewRegistry());

        Assert.Equal(ReplayDeterminismFixture.ExpectedDocumentVersion, result.Document.Version);
        Assert.Equal(ReplayDeterminismFixture.Commands.Count, result.Document.Log.Count);
        for (var i = 0; i < ReplayDeterminismFixture.Commands.Count; i++)
        {
            var expected = (NoOpCommand)ReplayDeterminismFixture.Commands[i];
            var actual = Assert.IsType<NoOpCommand>(result.Document.Log[i]);
            Assert.Equal(expected.CommandId, actual.CommandId);
            Assert.Equal(expected.Echo, actual.Echo);
        }

        var events = result.Events.Snapshot();
        Assert.Equal(ReplayDeterminismFixture.ExpectedEvents.Count, events.Count);
        for (var i = 0; i < ReplayDeterminismFixture.ExpectedEvents.Count; i++)
        {
            var expected = ReplayDeterminismFixture.ExpectedEvents[i];
            Assert.Equal(expected.Seq, events[i].Seq);
            Assert.Equal(expected.Kind, events[i].Kind);
            Assert.Equal(expected.CauseCommandId, events[i].CauseCommandId);
        }
    }

    [Fact]
    public async Task Two_Replays_Of_Fixture_Produce_Identical_Observable_State()
    {
        var first = await Replay.ReplayLog(ReplayDeterminismFixture.Commands, NewRegistry());
        var second = await Replay.ReplayLog(ReplayDeterminismFixture.Commands, NewRegistry());

        Assert.Equal(first.Document.Version, second.Document.Version);
        Assert.Equal(first.Document.Log.Count, second.Document.Log.Count);
        for (var i = 0; i < first.Document.Log.Count; i++)
            Assert.Equal(first.Document.Log[i].CommandId, second.Document.Log[i].CommandId);

        var firstEvents = first.Events.Snapshot();
        var secondEvents = second.Events.Snapshot();
        Assert.Equal(firstEvents.Count, secondEvents.Count);
        for (var i = 0; i < firstEvents.Count; i++)
        {
            Assert.Equal(firstEvents[i].Seq, secondEvents[i].Seq);
            Assert.Equal(firstEvents[i].Kind, secondEvents[i].Kind);
            Assert.Equal(firstEvents[i].CauseCommandId, secondEvents[i].CauseCommandId);
        }
    }
}
