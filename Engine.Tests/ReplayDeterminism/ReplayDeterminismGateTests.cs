using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;

namespace Engine.Tests.ReplayDeterminism;

// Gate per CLAUDE.md: "The gate runs … replay determinism fixture."
// Locks observable Document + event state against a hand-authored baseline
// (ReplayDeterminismFixture). Per ADR-0005, equality is asserted modulo
// EventRecord.Timestamp / EventRecord.DocumentId (and Document.DocumentId /
// CreatedAt / UpdatedAt, which derive from the same nondeterministic sources).
//
// Extended for TASK-0011 (P7a): body handles and Document.Bodies are also
// deterministic — per ADR-0012 §4, body handle is a pure function of
// CommandId. Each replay gets a fresh InProcessMeshBackend; the backend
// is a cache that gets reconstructed by replay (ADR-0001 §Consequences).
public class ReplayDeterminismGateTests
{
    private static CommandRegistry NewRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        registry.Register(new CreateBoxCommandHandler());
        return registry;
    }

    [Fact]
    public async Task Replay_Of_Fixture_Matches_Expected_Document_And_Event_Sequence()
    {
        var result = await Replay.ReplayLog(
            ReplayDeterminismFixture.Commands,
            NewRegistry(),
            new InProcessMeshBackend());

        Assert.Equal(ReplayDeterminismFixture.ExpectedDocumentVersion, result.Document.Version);
        Assert.Equal(ReplayDeterminismFixture.Commands.Count, result.Document.Log.Count);
        for (var i = 0; i < ReplayDeterminismFixture.Commands.Count; i++)
        {
            var expected = ReplayDeterminismFixture.Commands[i];
            Assert.Equal(expected.CommandId, result.Document.Log[i].CommandId);
            Assert.Equal(expected.Name, result.Document.Log[i].Name);
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

        var bodies = result.Document.Bodies.OrderBy(b => b.Handle.Id).ToList();
        Assert.Equal(ReplayDeterminismFixture.ExpectedBodies.Count, bodies.Count);
        for (var i = 0; i < ReplayDeterminismFixture.ExpectedBodies.Count; i++)
        {
            var expected = ReplayDeterminismFixture.ExpectedBodies[i];
            Assert.Equal(expected.HandleId, bodies[i].Handle.Id);
            Assert.Equal(expected.Kind, bodies[i].Kind);
        }
    }

    [Fact]
    public async Task Two_Replays_Of_Fixture_Produce_Identical_Observable_State()
    {
        var first = await Replay.ReplayLog(
            ReplayDeterminismFixture.Commands, NewRegistry(), new InProcessMeshBackend());
        var second = await Replay.ReplayLog(
            ReplayDeterminismFixture.Commands, NewRegistry(), new InProcessMeshBackend());

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

        // Body projection determinism: handles are pure functions of CommandId
        // per ADR-0012 §4. Two replays produce identical Document.Bodies.
        var firstBodies = first.Document.Bodies.OrderBy(b => b.Handle.Id).ToList();
        var secondBodies = second.Document.Bodies.OrderBy(b => b.Handle.Id).ToList();
        Assert.Equal(firstBodies.Count, secondBodies.Count);
        for (var i = 0; i < firstBodies.Count; i++)
        {
            Assert.Equal(firstBodies[i].Handle.Id, secondBodies[i].Handle.Id);
            Assert.Equal(firstBodies[i].Kind, secondBodies[i].Kind);
        }
    }
}
