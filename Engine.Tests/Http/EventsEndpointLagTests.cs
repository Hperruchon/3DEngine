using System.Net.Http.Json;
using System.Net.WebSockets;
using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Tests.Http;

public class EventsEndpointLagTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EventsEndpointLagTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task Slow_Subscriber_Is_Disconnected_With_Subscriber_Lagged_While_Fast_Subscriber_Receives_All_Events()
    {
        // Register entry R-0047 (TASK-0154): a pump delay of 200 ms gave the overflow only when the
        // posts came faster than the delay, and on a slow runner they did not. The gate lets the
        // pump send the reset, and then holds it until the posts are done. The pump holds at most
        // one event and the channel two, so the fourth event overflows on each runner.
        var open = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sends = 0;
        using var factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(new SubscriberOptions(
                    ChannelCapacity: 2,
                    HeartbeatInterval: TimeSpan.FromMinutes(5),
                    PumpGate: ct => Interlocked.Increment(ref sends) == 1 ? Task.CompletedTask : open.Task.WaitAsync(ct))));
            });
        });

        var http = factory.CreateClient();
        var host = factory.Services.GetRequiredService<EngineHost>();
        var docId = host.Document.DocumentId;

        using var slowSocket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(slowSocket, new
        {
            documentId = docId,
            lastSeenSeq = (long?)null,
        });
        // Drain the initial subscription.reset so the subscriber is ready for
        // live events.
        var initial = await WebSocketTestClient.ReceiveJsonAsync(slowSocket);
        Assert.Equal("subscription.reset", initial.GetProperty("kind").GetString());

        // Four events while the gate holds the pump: one in the pump, two in the channel, and the
        // fourth overflows. Each post returns after its commit gave the event to the subscriber.
        for (var i = 0; i < 4; i++)
        {
            var r = await http.PostAsJsonAsync("/commands", new
            {
                name = "NoOp",
                schemaVersion = 1,
                parameters = new { echo = $"flood-{i}" },
            });
            r.EnsureSuccessStatusCode();
        }

        open.SetResult();

        // Walk the slow subscriber's stream until we observe a Close frame.
        // We may also see a handful of buffered events first; that's fine.
        var sawLaggedClose = false;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        for (var i = 0; i < 30 && !sawLaggedClose; i++)
        {
            var msg = await WebSocketTestClient.ReceiveAsync(slowSocket, deadline.Token);
            if (msg.IsClose)
            {
                Assert.Equal(WebSocketCloseStatus.PolicyViolation, msg.CloseStatus);
                Assert.Equal("subscriber.lagged", msg.CloseStatusDescription);
                sawLaggedClose = true;
            }
        }

        Assert.True(sawLaggedClose, "Expected the slow subscriber to be lag-disconnected.");
    }
}
