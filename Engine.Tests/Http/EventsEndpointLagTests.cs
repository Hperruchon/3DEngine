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
        // Tiny channel + a small PumpDelay makes the slow subscriber overflow
        // deterministically when events arrive in bursts.
        using var factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(new SubscriberOptions(
                    ChannelCapacity: 2,
                    HeartbeatInterval: TimeSpan.FromMinutes(5),
                    PumpDelay: TimeSpan.FromMilliseconds(200))));
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

        // Now flood the engine with commands faster than the slow subscriber
        // can drain (PumpDelay 200 ms per message, channel cap 2).
        for (var i = 0; i < 12; i++)
        {
            var r = await http.PostAsJsonAsync("/commands", new
            {
                name = "NoOp",
                schemaVersion = 1,
                parameters = new { echo = $"flood-{i}" },
            });
            r.EnsureSuccessStatusCode();
        }

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
