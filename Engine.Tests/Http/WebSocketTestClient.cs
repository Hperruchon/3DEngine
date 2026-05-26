using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Engine.Tests.Http;

// Helper for opening WebSocket connections against WebApplicationFactory<Program>.
// All access is in-process via TestServer.CreateWebSocketClient(); no ports.
internal static class WebSocketTestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public static async Task<WebSocket> ConnectAsync(
        WebApplicationFactory<Program> factory,
        CancellationToken ct = default)
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        var uri = new UriBuilder(factory.Server.BaseAddress)
        {
            Scheme = "ws",
            Path = "/events",
        }.Uri;
        return await wsClient.ConnectAsync(uri, ct);
    }

    public static async Task SendJsonAsync(
        WebSocket socket,
        object payload,
        CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    public static async Task<JsonElement> ReceiveJsonAsync(
        WebSocket socket,
        CancellationToken ct = default)
    {
        var buffer = new byte[16 * 1024];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new InvalidOperationException(
                    $"WebSocket closed before expected message. Status={result.CloseStatus}, " +
                    $"Reason='{result.CloseStatusDescription}'.");
            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        ms.Position = 0;
        return JsonDocument.Parse(ms).RootElement.Clone();
    }

    /// Receive raw — returns a tuple of (closeFrame?, message?) so the caller
    /// can distinguish a Close from a Text.
    public static async Task<ReceiveResult> ReceiveAsync(
        WebSocket socket,
        CancellationToken ct = default)
    {
        var buffer = new byte[16 * 1024];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return new ReceiveResult(
                    IsClose: true,
                    CloseStatus: result.CloseStatus,
                    CloseStatusDescription: result.CloseStatusDescription,
                    Json: default);
            }
            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        ms.Position = 0;
        var json = JsonDocument.Parse(ms).RootElement.Clone();
        return new ReceiveResult(false, null, null, json);
    }

    internal sealed record ReceiveResult(
        bool IsClose,
        WebSocketCloseStatus? CloseStatus,
        string? CloseStatusDescription,
        JsonElement Json);
}
