using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;

namespace Engine.Tests.ReplayDeterminism;

// TASK-0035. ADR-0020: the version counts applied commands, so a replay of the
// log rebuilds it. These tests are the probe of 2026-09-23 that ADR-0020 gives.
public class ReplayVersionTests
{
    private static CommandRegistry NewRegistry()
    {
        var registry = new CommandRegistry();
        HandlerCatalog.RegisterAll(registry, new QueryRegistry());
        return registry;
    }

    // A rejection, then a command that states the version that its client saw.
    // Before the change the live version was 4 and the replay gave 2, and the
    // replay rejected the box, so a body was lost.
    [Fact]
    public async Task A_Replay_Rebuilds_The_Version_And_The_Bodies_After_A_Rejection()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var session = new DocumentSession(kit);

        await session.Apply(new NoOpCommand { Echo = "first" });
        var rejected = await session.Apply(new CreateBoxCommand { SizeX = -1, SizeY = 1, SizeZ = 1 });
        Assert.Equal(CommandStatus.Rejected, rejected.Status);

        var seen = await session.Read((document, _) => document.Version);
        var box = await session.Apply(new CreateBoxCommand
        {
            SizeX = 1, SizeY = 2, SizeZ = 3, ExpectedDocumentVersion = seen,
        });
        Assert.Equal(CommandStatus.Applied, box.Status);

        var (liveVersion, log, liveBodies) = await session.Read((document, _) => (
            document.Version,
            document.Log.ToArray(),
            document.Bodies.Select(b => b.Handle.Id).OrderBy(id => id).ToArray()));

        var replay = await Replay.ReplayLog(log, NewRegistry(), new InProcessMeshBackend());

        Assert.Equal(liveVersion, replay.Document.Version);
        Assert.Equal(log.Length, replay.Document.Log.Count);
        Assert.Equal(liveBodies, replay.Document.Bodies.Select(b => b.Handle.Id).OrderBy(id => id).ToArray());
    }

    // ADR-0020 items 1 and 2: a rejection does not move the version, and its
    // event still takes the next Seq.
    [Fact]
    public async Task A_Rejection_Keeps_The_Version_And_Takes_A_New_Seq()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var session = new DocumentSession(kit);

        var first = await session.Apply(new NoOpCommand { Echo = "first" });
        var rejected = await session.Apply(new CreateBoxCommand { SizeX = -1, SizeY = 1, SizeZ = 1 });

        Assert.Equal(1, first.DocumentVersion);
        Assert.Equal(CommandStatus.Rejected, rejected.Status);
        Assert.Equal(1, rejected.DocumentVersion);

        var events = kit.Events.Snapshot();
        Assert.Equal("command.rejected", events[^1].Kind);
        Assert.Equal(2, events[^1].Seq);
    }
}
