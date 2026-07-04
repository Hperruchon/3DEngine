using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.ReplayDeterminism;

// Proves the NATIVE geometry path is replay-reconstructible (TASK-0012 §7): replaying a
// CreateBox log against two fresh ManifoldGeometryBackends yields identical observable
// state. This deliberately does NOT touch the canonical replay-determinism gate, which
// stays on the deterministic managed stub (ReplayDeterminismGateTests) — so the core
// gate never depends on native floating-point reproducibility (ADR-0014 §Open challenges).
// Skipped when native manifoldc is unavailable on the runner.
public class ManifoldReplayRoundTripTests
{
    private static CommandRegistry NewRegistry()
    {
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        registry.Register(new CreateBoxCommandHandler());
        return registry;
    }

    [NativeManifoldFact]
    public async Task Two_Native_Replays_Of_CreateBox_Produce_Identical_State()
    {
        var create = new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 };
        var log = new Command[] { create };

        using var backend1 = new ManifoldGeometryBackend();
        using var backend2 = new ManifoldGeometryBackend();
        var first = await Replay.ReplayLog(log, NewRegistry(), backend1);
        var second = await Replay.ReplayLog(log, NewRegistry(), backend2);

        // Document version + log length match.
        Assert.Equal(first.Document.Version, second.Document.Version);
        Assert.Equal(first.Document.Log.Count, second.Document.Log.Count);

        // Event sequence matches (Seq / Kind / CauseCommandId), per ADR-0005.
        var e1 = first.Events.Snapshot();
        var e2 = second.Events.Snapshot();
        Assert.Equal(e1.Count, e2.Count);
        for (var i = 0; i < e1.Count; i++)
        {
            Assert.Equal(e1[i].Seq, e2[i].Seq);
            Assert.Equal(e1[i].Kind, e2[i].Kind);
            Assert.Equal(e1[i].CauseCommandId, e2[i].CauseCommandId);
        }

        // Body projection: exactly one Box, handle == CommandId (ADR-0012 §4), stable
        // across both native replays.
        var b1 = Assert.Single(first.Document.Bodies);
        var b2 = Assert.Single(second.Document.Bodies);
        Assert.Equal(create.CommandId, b1.Handle.Id);
        Assert.Equal(b1.Handle.Id, b2.Handle.Id);
        Assert.Equal("Box", b1.Kind);
        Assert.Equal(b1.Kind, b2.Kind);
    }
}
