using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Engine.Api.Http.Endpoints;

// GET /events — WebSocket upgrade.
//
// Per TASK-0010 §4 and ADR-0005 §5:
//   1. Accept the WebSocket upgrade. Non-WS request → 400 with E-API-BAD-REQUEST.
//   2. Read one client text frame as a SubscribeRequest. Malformed → close
//      with status 1003 + reason "E-API-WS-INVALID-SUBSCRIBE".
//   3. AttachAndPrime on the broadcaster: under the broadcaster's lock, decide
//      resume vs reset, enqueue initial response + any replay, then add to the
//      subscriber list. This guarantees live events emitted between snapshot
//      and attach can't slip past the replay.
//   4. Await the subscriber's pump completion (client close, or lag detected).
//   5. Detach from the broadcaster, dispose the subscriber.
internal static class EventsEndpoint
{
    public static async Task Handle(
        HttpContext context,
        EngineHost host,
        EventBroadcaster broadcaster,
        SubscriberOptions options)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            var result = ApiErrorEnvelope.BadRequest("This endpoint requires a WebSocket upgrade.");
            await result.ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

        SubscribeRequest? subscribeRequest;
        try
        {
            subscribeRequest = await ReadSubscribeAsync(webSocket, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (subscribeRequest is null)
        {
            await CloseInvalidSubscribeAsync(webSocket).ConfigureAwait(false);
            return;
        }

        var subscriber = new Subscriber(
            webSocket,
            channelCapacity: options.ChannelCapacity,
            heartbeatInterval: options.HeartbeatInterval,
            pumpDelay: options.PumpDelay);
        broadcaster.AttachAndPrime(subscriber, host.Document, host.Events, subscribeRequest);

        try
        {
            await subscriber.RunCompleted.ConfigureAwait(false);
        }
        finally
        {
            broadcaster.Detach(subscriber);
            await subscriber.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<SubscribeRequest?> ReadSubscribeAsync(
        WebSocket socket,
        CancellationToken ct)
    {
        const int maxSubscribeBytes = 4096;
        var buffer = new byte[maxSubscribeBytes];

        var result = await socket.ReceiveAsync(buffer, ct).ConfigureAwait(false);
        if (result.MessageType != WebSocketMessageType.Text || !result.EndOfMessage)
            return null;

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        try
        {
            return JsonSerializer.Deserialize<SubscribeRequest>(json, ApiJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task CloseInvalidSubscribeAsync(WebSocket socket)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.InvalidPayloadData,
                    "E-API-WS-INVALID-SUBSCRIBE",
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* best-effort */ }
        }
    }
}
