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
// The test measures the gap between consecutive heartbeat frames, and not the
// count of frames in a fixed second. A loaded runner makes each gap longer and
// never shorter, so the test cannot fail for slowness; the defect makes each
// gap close to zero, so the test cannot pass with the defect. The first form
// of this test counted frames in one second and failed once on Ubuntu.
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

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new { });

        var initial = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", initial.GetProperty("kind").GetString());

        // Three heartbeat frames, with the moment of each one.
        var moments = new List<TimeSpan>();
        var clock = Stopwatch.StartNew();
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (moments.Count < 3)
        {
            var frame = await WebSocketTestClient.ReceiveJsonAsync(socket, limit.Token);
            if (frame.GetProperty("kind").GetString() == "heartbeat")
                moments.Add(clock.Elapsed);
        }

        // Each gap is at least the interval, less the one millisecond of
        // tolerance that the loop itself gives. With the defect the frames
        // arrive together and the gaps are close to zero.
        var least = interval - TimeSpan.FromMilliseconds(5);
        for (var i = 1; i < moments.Count; i++)
        {
            var gap = moments[i] - moments[i - 1];
            Assert.True(
                gap >= least,
                $"Heartbeat {i + 1} came {gap.TotalMilliseconds:F0} ms after heartbeat {i}; the interval is "
                + $"{interval.TotalMilliseconds:F0} ms. Moments: {string.Join(", ", moments.Select(m => m.TotalMilliseconds.ToString("F0")))} ms.");
        }
    }
}
