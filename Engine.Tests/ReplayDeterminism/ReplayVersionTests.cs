using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;

namespace Engine.Tests.ReplayDeterminism;

// TASK-0035. ADR-0020: the version counts applied commands, so a replay of the
// log rebuilds it. The first two tests are the probe of 2026-09-23 that
// ADR-0020 gives. The last two are findings E7 and E17 of the codebase review
// of 2026-10-04, which the owner added to the task (question Q1).
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

    // Finding E7. Each command in a log was applied one time, so a rejection
    // in a replay is a divergence. Before the change the replay returned an
    // empty Document with no signal. The replay here has no geometry backend,
    // so the box is rejected.
    [Fact]
    public async Task A_Replay_Stops_At_A_Rejection_And_Names_The_Command()
    {
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 };
        Command[] log = [new NoOpCommand { Echo = "first" }, box];

        var error = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            () => Replay.ReplayLog(log, NewRegistry()));

        Assert.Contains(box.CommandId.ToString(), error.Message);
        Assert.Contains(DiagnosticCodes.GeomCapMissing, error.Message);
    }

    // Finding E17, the replay part. A live bus keeps 1,024 results in its cache,
    // so a log can hold one CommandId two times. Before the change the replay
    // returned the cached result for the second entry and gave a shorter log.
    [Fact]
    public async Task A_Replay_Applies_Each_Entry_Of_The_Log_Once()
    {
        var id = Guid.NewGuid();
        Command[] log =
        [
            new NoOpCommand { CommandId = id, Echo = "first" },
            new NoOpCommand { CommandId = id, Echo = "again" },
        ];

        var replay = await Replay.ReplayLog(log, NewRegistry(), new InProcessMeshBackend());

        Assert.Equal(2, replay.Document.Log.Count);
        Assert.Equal(2, replay.Document.Version);
    }
}
