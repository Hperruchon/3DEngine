using System.Net.Http.Json;
using System.Net.WebSockets;
using Engine.Api.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Engine.Tests.Http;

public class EventsEndpointResumeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EventsEndpointResumeTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task Subscribe_With_Cursor_Inside_Ring_Receives_Resume_Then_Buffered_Events()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var http = factory.CreateClient();

        // Apply three NoOp commands to build up history.
        foreach (var echo in new[] { "alpha", "beta", "gamma" })
        {
            var response = await http.PostAsJsonAsync("/commands", new
            {
                name = "NoOp",
                schemaVersion = 1,
                parameters = new { echo },
            });
            response.EnsureSuccessStatusCode();
        }

        var host = factory.Services.GetRequiredService<EngineHost>();
        var docId = host.Document.DocumentId;

        // Subscribe with lastSeenSeq=1 — client claims it saw the first event.
        // Engine should resume from Seq 2 and replay events 2 and 3 from the ring.
        using var socket = await WebSocketTestClient.ConnectAsync(factory);
        await WebSocketTestClient.SendJsonAsync(socket, new
        {
            documentId = docId,
            lastSeenSeq = 1L,
        });

        var resume = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("subscription.resume", resume.GetProperty("kind").GetString());
        Assert.Equal(2L, resume.GetProperty("fromSeq").GetInt64());

        var second = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("command.applied", second.GetProperty("kind").GetString());
        Assert.Equal(2L, second.GetProperty("seq").GetInt64());

        var third = await WebSocketTestClient.ReceiveJsonAsync(socket);
        Assert.Equal("command.applied", third.GetProperty("kind").GetString());
        Assert.Equal(3L, third.GetProperty("seq").GetInt64());

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
    }
}
