using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

public class EventsEndpointWireShapeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EventsEndpointWireShapeTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task Document_Event_Frame_Matches_EventRecord_Shape()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();
        var host = factory.Services.GetRequiredService<EngineHost>();

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = host.Document.DocumentId,
            lastSeenSeq = host.Document.Version,
        });

        // Initial subscription.resume (no replay since cursor is current).
        var resume = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.resume", resume.GetProperty("kind").GetString());

        // Now apply a command and observe the live frame.
        var r = await http.PostAsJsonAsync("/commands", new
        {
            name = "NoOp",
            schemaVersion = 1,
            parameters = new { echo = "wire-shape" },
        });
        r.EnsureSuccessStatusCode();

        var live = await WebSocketTestClient.ReceiveJsonAsync(socket);
        // Required EventRecord fields per ADR-0005 §1.
        Assert.Equal("command.applied", live.GetProperty("kind").GetString());
        Assert.True(live.TryGetProperty("seq", out var seq) && seq.ValueKind == JsonValueKind.Number);
        Assert.True(live.TryGetProperty("timestamp", out var ts) && ts.ValueKind == JsonValueKind.String);
        Assert.True(live.TryGetProperty("documentId", out var did) && did.ValueKind == JsonValueKind.String);
        Assert.True(live.TryGetProperty("causeCommandId", out var ccid));
        Assert.True(live.TryGetProperty("payload", out _));

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Fact]
    public async Task Subscription_Reset_Snapshot_Matches_ADR_0010_Shape()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var host = factory.Services.GetRequiredService<EngineHost>();

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = (Guid?)null,
            lastSeenSeq = (long?)null,
        });

        var reset = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());
        Assert.Equal(host.Document.DocumentId, reset.GetProperty("documentId").GetGuid());

        // Snapshot per ADR-0010 §2: documentId, projectId, schemaVersion,
        // version, createdAt, updatedAt — and notably NOT a `log` field.
        // Extended per ADR-0012 §6: bodies array (possibly empty).
        var snapshot = reset.GetProperty("snapshot");
        Assert.Equal(host.Document.DocumentId, snapshot.GetProperty("documentId").GetGuid());
        Assert.True(snapshot.TryGetProperty("projectId", out _));
        Assert.True(snapshot.TryGetProperty("schemaVersion", out var sv) && sv.ValueKind == JsonValueKind.Number);
        Assert.True(snapshot.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.Number);
        Assert.True(snapshot.TryGetProperty("createdAt", out _));
        Assert.True(snapshot.TryGetProperty("updatedAt", out _));
        Assert.False(snapshot.TryGetProperty("log", out _), "snapshot must not include the command log (ADR-0010 §2).");
        Assert.True(snapshot.TryGetProperty("bodies", out var bodies)
            && bodies.ValueKind == JsonValueKind.Array,
            "snapshot must include a bodies array (ADR-0012 §6).");
        Assert.Empty(bodies.EnumerateArray());

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Fact]
    public async Task Subscription_Reset_After_CreateBox_Has_Bodies_Array_With_Created_Body()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();
        var host = factory.Services.GetRequiredService<EngineHost>();

        // Apply a CreateBox first so the Document has a body in its projection.
        var apply = await http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox",
            schemaVersion = 1,
            parameters = new { sizeX = 2.0, sizeY = 4.0, sizeZ = 8.0 },
        });
        apply.EnsureSuccessStatusCode();
        var applyBody = JsonDocument.Parse(await apply.Content.ReadAsStringAsync()).RootElement;
        var bodyId = applyBody.GetProperty("outputs").GetProperty("bodyId").GetGuid();

        // Now subscribe with a null cursor → reset with snapshot.
        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = (Guid?)null,
            lastSeenSeq = (long?)null,
        });

        var reset = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());

        var bodies = reset.GetProperty("snapshot").GetProperty("bodies");
        Assert.Equal(JsonValueKind.Array, bodies.ValueKind);
        var entries = bodies.EnumerateArray().ToArray();
        Assert.Single(entries);
        Assert.Equal(bodyId, entries[0].GetProperty("handle").GetGuid());
        Assert.Equal("Box", entries[0].GetProperty("kind").GetString());

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }
}
