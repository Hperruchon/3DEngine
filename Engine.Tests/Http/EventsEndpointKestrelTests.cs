using System.Net.Http.Json;
using System.Net.WebSockets;
using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Tests.Http;

// Finding E17 of the codebase review of 2026-09-30, parts 5 and 6 (register entry R-0028, TASK-0481): two closes of the
// event stream on a real Kestrel at 127.0.0.1. The test server gives a socket that does not abort when a receive is
// cancelled, so the tests of the test server did not see either fault; a socket of Kestrel aborts, and an aborted socket
// sends no close frame.
[Collection("real host")]
public class EventsEndpointKestrelTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    // Part 5: the pump of a lagged subscriber sends the close frame before it cancels the receive loop, so the client reads
    // the status 1008 and "subscriber.lagged" (ADR-0005 section 6). Until TASK-0481 the cancel came first, the receive of the
    // server aborted the socket, and the client read a broken connection.
    [Fact]
    public async Task A_Lagged_Subscriber_Of_The_Host_Reads_The_Close_With_Subscriber_Lagged()
    {
        var open = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sends = 0;
        var options = new SubscriberOptions(
            ChannelCapacity: 2,
            HeartbeatInterval: TimeSpan.FromMinutes(5),
            PumpGate: ct => Interlocked.Increment(ref sends) == 1 ? Task.CompletedTask : open.Task.WaitAsync(ct));
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host, options);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        using var limit = new CancellationTokenSource(Limit);
        using var socket = await Connect(app, limit.Token);

        await WebSocketTestClient.SendJsonAsync(socket, new { documentId = host.Document.DocumentId, lastSeenSeq = (long?)null }, limit.Token);
        var reset = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
        Assert.Equal("subscription.reset", reset.GetProperty("kind").GetString());

        // Four events while the gate holds the pump: one in the pump, two in the channel, and the fourth overflows.
        for (var i = 0; i < 4; i++)
        {
            var response = await http.PostAsJsonAsync("/commands", new { name = "NoOp", schemaVersion = 1, parameters = new { echo = $"flood-{i}" } }, limit.Token);
            response.EnsureSuccessStatusCode();
        }

        open.SetResult();
        WebSocketTestClient.ReceiveResult? close = null;
        for (var i = 0; i < 30 && close is null; i++)
        {
            var message = await WebSocketTestClient.ReceiveAsync(socket, limit.Token);
            if (message.IsClose)
                close = message;
        }

        Assert.NotNull(close);
        Assert.Equal(WebSocketCloseStatus.PolicyViolation, close.CloseStatus);
        Assert.Equal("subscriber.lagged", close.CloseStatusDescription);
    }

    // Part 6: a client that sends no subscribe frame in the time limit reads the close of a missing frame, 1007 with
    // E-API-WS-INVALID-SUBSCRIBE (ADR-0005 section 5). Until TASK-0481 the read had no time limit, and the client waited until
    // it left.
    [Fact]
    public async Task A_Client_That_Sends_No_Subscribe_Frame_Reads_The_Close_Of_A_Missing_Frame()
    {
        var options = SubscriberOptions.Default with { SubscribeTimeout = TimeSpan.FromMilliseconds(200) };
        var host = new EngineHost(new InProcessMeshBackend());
        await using var app = await StartSurface(host, options);
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = await Connect(app, limit.Token);

        var message = await WebSocketTestClient.ReceiveAsync(socket, limit.Token);

        Assert.True(message.IsClose);
        Assert.Equal(WebSocketCloseStatus.InvalidPayloadData, message.CloseStatus);
        Assert.Equal("E-API-WS-INVALID-SUBSCRIBE", message.CloseStatusDescription);

        // The default is the limit of ADR-0005 section 5.
        Assert.Equal(TimeSpan.FromSeconds(30), SubscriberOptions.Default.SubscribeTimeout);
    }

    private static async Task<ClientWebSocket> Connect(WebApplication app, CancellationToken ct)
    {
        var socket = new ClientWebSocket();
        var address = new UriBuilder(app.Urls.First()) { Scheme = "ws", Path = "/events" }.Uri;
        await socket.ConnectAsync(address, ct);
        return socket;
    }

    // The steps of a host that mounts the surface (DesktopSurfaceTests), with the options of the subscribers of the test.
    private static async Task<WebApplication> StartSurface(EngineHost host, SubscriberOptions options)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddEngineSurface(_ => host);
        builder.Services.Replace(ServiceDescriptor.Singleton(options));
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        Assert.Null(app.BoundAddressRefusal());
        return app;
    }
}
