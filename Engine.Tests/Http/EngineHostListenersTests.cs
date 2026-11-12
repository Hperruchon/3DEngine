using System.Diagnostics;
using System.Net.WebSockets;
using Engine.Api.Http;
using Engine.Core.Geometry;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

// ADR-0127 item 3 (phase R18g, TASK-0354): the count of the clients that listen to the event stream of the surface, which
// the window of the desktop shows as "agent connected". A subscription of the host to its own session is not a client.
public class EngineHostListenersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EngineHostListenersTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public void A_Subscription_Of_The_Host_Is_Not_A_Listener()
    {
        using var host = new EngineHost(new InProcessMeshBackend());
        using var subscription = host.Subscribe(_ => { });

        Assert.Equal(0, host.Listeners);
    }

    [Fact]
    public async Task A_Client_Of_The_Event_Stream_Is_A_Listener_Until_It_Closes()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var host = factory.Services.GetRequiredService<EngineHost>();
        Assert.Equal(0, host.Listeners);

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        // A document identifier that is not the document of the host gives a reset, after the attach of the subscriber.
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
            lastSeenSeq = 0L,
        });
        var message = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", message.GetProperty("kind").GetString());
        Assert.Equal(1, host.Listeners);

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        var clock = Stopwatch.StartNew();
        while (host.Listeners > 0 && clock.Elapsed < TimeSpan.FromSeconds(10))
            await Task.Delay(20);
        Assert.Equal(0, host.Listeners);
    }
}
