using System.Diagnostics;
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
//
// The test counts the heartbeat frames that arrive in at least one second, and
// compares the count with the time that passed since before the connection. The
// server sends at most one frame in each interval, so the count cannot pass that
// time divided by the interval, plus a margin. A slow runner or a late client
// only makes the count smaller. The defect sent thousands of frames in a second.
//
// The first form counted the frames in one fixed second and failed once on
// Ubuntu. The second form measured the gap between two frames on the client;
// a late client read two frames that waited in the queue with a gap of about
// zero, and it failed on Windows in runs 37963719832 and 37997004258 (finding
// T12 of the codebase review of 2026-10-04, TASK-0059).
[Collection("real host")]
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
        var interval = TimeSpan.FromMilliseconds(100);

        using var factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(new SubscriberOptions(
                    ChannelCapacity: Subscriber.DefaultChannelCapacity,
                    HeartbeatInterval: interval,
                    PumpDelay: TimeSpan.Zero)));
            });
        });

        // The clock starts before the connection, so the server cannot have sent
        // a frame before it.
        var clock = Stopwatch.StartNew();
        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new { });

        var initial = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", initial.GetProperty("kind").GetString());

        // At least one second and at least one heartbeat. A host that sends no
        // heartbeat fails at the limit.
        var heartbeats = 0;
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (clock.Elapsed < TimeSpan.FromSeconds(1) || heartbeats == 0)
        {
            var frame = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            if (frame.GetProperty("kind").GetString() == "heartbeat")
                heartbeats++;
        }

        var elapsed = clock.Elapsed;
        var most = (int)(elapsed.Ticks / interval.Ticks) + 2;
        Assert.True(
            heartbeats <= most,
            $"{heartbeats} heartbeat frames came in {elapsed.TotalMilliseconds:F0} ms; with one frame in each "
            + $"{interval.TotalMilliseconds:F0} ms the most is {most}.");
    }
}
