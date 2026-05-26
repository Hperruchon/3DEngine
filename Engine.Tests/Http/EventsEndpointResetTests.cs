using System.Net.Http.Json;
using System.Net.WebSockets;
using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Engine.Tests.Http;

public class EventsEndpointResetTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EventsEndpointResetTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task Subscribe_With_Stale_Cursor_Receives_Reset_With_Snapshot()
    {
        // Configure a tiny channel so we can mark a cursor as "too far behind to
        // fit in the replay channel," forcing the fallback to reset.
        using var factory = _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(SubscriberOptions.Default with
                {
                    ChannelCapacity = 2,
                }));
            });
        });

        var http = factory.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            var r = await http.PostAsJsonAsync("/commands", new
            {
                name = "NoOp",
                schemaVersion = 1,
                parameters = new { echo = $"e{i}" },
            });
            r.EnsureSuccessStatusCode();
        }

        var host = factory.Services.GetRequiredService<EngineHost>();
        var docId = host.Document.DocumentId;

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        // Cursor at Seq 0 — replay would be 5 events, channel capacity is 2;
        // engine should reset instead.
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = docId,
            lastSeenSeq = 0L,
        });

        var msg = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", msg.GetProperty("kind").GetString());
        Assert.Equal(docId, msg.GetProperty("documentId").GetGuid());

        var snapshot = msg.GetProperty("snapshot");
        Assert.Equal(docId, snapshot.GetProperty("documentId").GetGuid());
        Assert.Equal(host.Document.Version, snapshot.GetProperty("version").GetInt64());

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Fact]
    public async Task Subscribe_With_Mismatched_DocumentId_Receives_Reset_With_Snapshot()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var host = factory.Services.GetRequiredService<EngineHost>();
        var foreignDocId = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = foreignDocId,
            lastSeenSeq = 5L,
        });

        var msg = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", msg.GetProperty("kind").GetString());
        Assert.Equal(host.Document.DocumentId, msg.GetProperty("documentId").GetGuid());

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }

    [Fact]
    public async Task Subscribe_With_Null_DocumentId_Receives_Reset_With_Snapshot()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var host = factory.Services.GetRequiredService<EngineHost>();

        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = (Guid?)null,
            lastSeenSeq = (long?)null,
        });

        var msg = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.reset", msg.GetProperty("kind").GetString());
        Assert.Equal(host.Document.DocumentId, msg.GetProperty("documentId").GetGuid());

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }
}
