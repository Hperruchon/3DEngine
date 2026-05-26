using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

public class EventsEndpointInvalidSubscribeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public EventsEndpointInvalidSubscribeTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    [Fact]
    public async Task Malformed_Subscribe_Frame_Closes_With_E_API_WS_INVALID_SUBSCRIBE()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        using var socket = await WebSocketTestClient.ConnectAsync(factory);

        var bytes = Encoding.UTF8.GetBytes("not-json-at-all");
        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);

        var msg = await WebSocketTestClient.ReceiveAsync(socket);
        Assert.True(msg.IsClose);
        Assert.Equal(WebSocketCloseStatus.InvalidPayloadData, msg.CloseStatus);
        Assert.Equal("E-API-WS-INVALID-SUBSCRIBE", msg.CloseStatusDescription);
    }

    [Fact]
    public async Task Plain_HTTP_GET_On_Events_Returns_400_With_E_API_BAD_REQUEST()
    {
        using var factory = _baseFactory.WithWebHostBuilder(_ => { });
        var client = factory.CreateClient();
        var response = await client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;
        Assert.Equal(
            "E-API-BAD-REQUEST",
            json.GetProperty("error").GetProperty("code").GetString());
    }
}
