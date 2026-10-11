using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Http;

// ADR-0019, the hybrid topology: a host owns a document session, mounts the HTTP
// and WebSocket surface on it, and an agent works live on that session. This test
// is the host, in the steps of the desktop host (TASK-0054): it composes the
// session, mounts the surface library on a real Kestrel at 127.0.0.1, and also
// sends typed calls to the same session. TASK-0038 made the surface a library;
// before it, only the program engine-api-http could serve the surface.
[Collection("real host")]
public class DesktopSurfaceTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task A_Command_Over_Http_Changes_The_Session_That_The_Host_Owns()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var limit = new CancellationTokenSource(Limit);
        var commandId = Guid.NewGuid();

        using var response = await http.PostAsJsonAsync("/commands", new
        {
            name = "CreateBox",
            schemaVersion = 1,
            commandId,
            parameters = new { sizeX = 1.0, sizeY = 2.0, sizeZ = 3.0 },
        }, limit.Token);
        response.EnsureSuccessStatusCode();

        // A typed query of the host on the same session reads the body.
        var box = await host.Session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = commandId }, limit.Token);
        Assert.Null(box.Error);
        Assert.Equal(2.0, box.Result.MaxY - box.Result.MinY, 9);
    }

    [Fact]
    public async Task A_Subscriber_Of_The_Surface_Receives_The_Event_Of_A_Typed_Call_Of_The_Host()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host);
        using var limit = new CancellationTokenSource(Limit);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        var reset = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
        Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());

        // A typed call of the host, and not a request over HTTP.
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(box, limit.Token)).Status);

        // The frames of the commit come in the order command.applied, body.created.
        JsonElement frame;
        string? kind;
        do
        {
            frame = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            kind = frame.GetProperty("kind").GetString();
        }
        while (kind is "command.applied" or "heartbeat");

        Assert.Equal("body.created", kind);
        Assert.Equal(box.CommandId, frame.GetProperty("payload").GetProperty("bodyId").GetGuid());
    }

    // Finding E49 of the codebase review of 2026-10-11: the stop of a host waited for
    // each open /events WebSocket until the time limit of the host, 31.0 s in a run
    // of a review agent. The host now ends each subscriber when it stops (TASK-0062).
    [Fact]
    public async Task A_Host_With_A_Subscriber_Stops_At_Once()
    {
        var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await using var app = await StartSurface(host);
        using var limit = new CancellationTokenSource(Limit);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        var reset = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
        Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());

        var clock = System.Diagnostics.Stopwatch.StartNew();
        await app.StopAsync();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"The host took {clock.Elapsed.TotalSeconds:F1} s to stop.");
    }

    // The steps of a host that mounts the surface, in the order of EngineSurface.
    private static async Task<WebApplication> StartSurface(EngineHost host)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        Assert.Null(EngineSurface.ConfigurationRefusal(builder.Configuration));

        builder.AddEngineSurface(_ => host);
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();

        Assert.Null(app.BoundAddressRefusal());
        return app;
    }
}
