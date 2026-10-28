using System.Text.Json;
using Engine.Api.Http.Json;
using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;

namespace Engine.Tests.Persistence;

// ADR-0015 items 5 to 7 (TASK-0091, KD3 and KD4): each log entry records the backend
// that applied its command, a replay of entries refuses another backend, and the
// Document has one length unit, which the snapshot gives.
public class BackendRecordTests
{
    [Fact]
    public async Task Each_Log_Entry_Records_The_Backend_Of_The_Bus()
    {
        var backend = new InProcessMeshBackend();
        var kit = EngineHosting.CreateDefault(backend);
        var bus = kit.CreateCommandBus();

        await bus.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        await bus.Apply(new NoOpCommand { Echo = "x" });

        Assert.Equal(2, kit.Document.Log.Count);
        Assert.All(kit.Document.Log, entry => Assert.Equal(backend.Identity, entry.Backend));
        Assert.Equal(new BackendIdentity("managed", "1"), backend.Identity);
    }

    [Fact]
    public async Task A_Replay_Of_Entries_Refuses_An_Entry_Of_Another_Backend()
    {
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        var log = new[]
        {
            new LogEntry(new NoOpCommand { Echo = "a" }, new BackendIdentity("null", "1")),
            new LogEntry(new NoOpCommand { Echo = "b" }, new BackendIdentity("manifold", "3.5.2")),
        };

        var error = await Assert.ThrowsAsync<BackendMismatchException>(() => Replay.ReplayLog(log, registry));

        Assert.Equal(1, error.Index);
        Assert.StartsWith(DiagnosticCodes.IoBackendMismatch, error.Message, StringComparison.Ordinal);
    }

    // ADR-0015 item 6: the replay checks the name; the open reports another version.
    [Fact]
    public async Task A_Replay_Of_Entries_Accepts_Another_Version_Of_The_Same_Backend()
    {
        var registry = new CommandRegistry();
        registry.Register(new NoOpCommandHandler());
        var log = new[] { new LogEntry(new NoOpCommand { Echo = "a" }, new BackendIdentity("null", "0")) };

        var replay = await Replay.ReplayLog(log, registry);

        Assert.Equal(1, replay.Document.Version);
        Assert.Equal(NullGeometryBackend.Instance.Identity, replay.Document.Log[0].Backend);
    }

    [Fact]
    public void A_Document_Has_The_Millimetre_And_Refuses_Another_Unit()
    {
        Assert.Equal("mm", new Document().LengthUnit);
        Assert.Throws<ArgumentException>(() => new Document(lengthUnit: "m"));
    }

    [Fact]
    public void The_Snapshot_Gives_The_Length_Unit()
    {
        var json = JsonSerializer.Serialize(SnapshotProjector.Project(new Document(), 0), ApiJson.Options);

        using var parsed = JsonDocument.Parse(json);
        Assert.Equal("mm", parsed.RootElement.GetProperty("lengthUnit").GetString());
    }
}
