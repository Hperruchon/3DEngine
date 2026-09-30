using System.Net.WebSockets;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Tests.Http;

// ADR-0005 §5.4: the engine sends a heartbeat at most every interval when the
// stream is idle. Finding E8 of the codebase review of 2026-09-30: an idle
// subscriber received 1,025 heartbeat frames at second 30, because the loop
// wrote a frame and started again with no wait. Register entry R-0029.
public class HeartbeatTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public HeartbeatTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task An_Idle_Subscriber_Receives_One_Heartbeat_In_Each_Interval()
    {
        using var factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(new SubscriberOptions(
                    ChannelCapacity: Subscriber.DefaultChannelCapacity,
                    HeartbeatInterval: TimeSpan.FromMilliseconds(100),
                    PumpDelay: TimeSpan.Zero)));
            });
        });

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new { });

        var initial = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", initial.GetProperty("kind").GetString());

        // One second of silence holds ten intervals. The loop of today writes a
        // burst of frames at the first interval, up to the channel capacity.
        var heartbeats = 0;
        using var window = new CancellationTokenSource(TimeSpan.FromMilliseconds(1050));
        try
        {
            while (true)
            {
                var frame = await WebSocketTestClient.ReceiveJsonAsync(socket, window.Token);
                if (frame.GetProperty("kind").GetString() == "heartbeat")
                    heartbeats++;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }

        Assert.InRange(heartbeats, 3, 11);
    }
}
