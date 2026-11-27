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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(60);

    // ADR-0173 item 4: a stop that does not end in 60 s waits for ever.
    private static readonly TimeSpan Hang = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task A_Command_Over_Http_Changes_The_Session_That_The_Host_Owns()
    {
        var host = new EngineHost(new InProcessMeshBackend());
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
        var host = new EngineHost(new InProcessMeshBackend());
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
    // TASK-0435 (phase P0.173): no time decides the test. The host of the test has no
    // stop limit, so only the end of the subscriber by the host lets the stop end. With the limit of 2 s of the
    // surface (TASK-0067), a host that does not end its subscriber also stopped in 2 s,
    // under the old limit of 5 s of the test. Hang (60 s) only stops a test that waits
    // for ever (ADR-0173 item 4).
    [Fact]
    public async Task A_Host_With_A_Subscriber_Ends_It_When_It_Stops()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host, stopLimit: Timeout.InfiniteTimeSpan);
        using var limit = new CancellationTokenSource(Limit);
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(app.Urls.First().Replace("http://", "ws://") + "/events"), limit.Token);
        await WebSocketTestClient.SendJsonAsync(socket, new { }, limit.Token);
        var reset = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
        Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());

        var stop = app.StopAsync();
        Assert.Same(stop, await Task.WhenAny(stop, Task.Delay(Hang)));
        await stop;
    }

    // Finding E58 of the codebase review of 2026-10-12: a client that sent part of a
    // request held the stop of a host for 30 s, the default limit of the framework. The
    // surface now sets a limit of 2 s (TASK-0067). TASK-0435 (phase P0.173): no time
    // decides the test. The host keeps the stop limit of the surface, and its stop with
    // the partial request ends; Hang (60 s) only stops a test that waits for ever.
    [Fact]
    public async Task A_Host_With_A_Partial_Request_Stops_With_The_Stop_Limit_Of_The_Surface()
    {
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host);
        Assert.Equal(EngineSurface.ShutdownTimeout, app.Services.GetRequiredService<IOptions<HostOptions>>().Value.ShutdownTimeout);
        var address = new Uri(app.Urls.First());
        using var client = new System.Net.Sockets.TcpClient();
        await client.ConnectAsync(address.Host, address.Port);
        var partial = System.Text.Encoding.ASCII.GetBytes("POST /commands HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 100\r\n\r\n{");
        await client.GetStream().WriteAsync(partial);
        await Task.Delay(200);

        var stop = app.StopAsync();
        Assert.Same(stop, await Task.WhenAny(stop, Task.Delay(Hang)));
        await stop;
    }

    // The steps of a host that mounts the surface, in the order of EngineSurface. A test
    // can give the host another stop limit after the surface.
    private static async Task<WebApplication> StartSurface(EngineHost host, TimeSpan? stopLimit = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        Assert.Null(EngineSurface.ConfigurationRefusal(builder.Configuration));

        builder.AddEngineSurface(_ => host);
        if (stopLimit is { } stopAfter)
            builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = stopAfter);
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();

        Assert.Null(app.BoundAddressRefusal());
        return app;
    }
}
