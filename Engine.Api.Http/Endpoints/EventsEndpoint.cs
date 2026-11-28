using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Engine.Api.Http.Errors;
using Engine.Api.Http.Json;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Engine.Api.Http.Endpoints;

// GET /events — WebSocket upgrade.
//
// Per TASK-0010 §4 and ADR-0005 §5:
//   1. Accept the WebSocket upgrade. Non-WS request → 400 with E-API-BAD-REQUEST.
//   2. Read one client text frame as a SubscribeRequest. Malformed → close
//      with status 1007 + reason "E-API-WS-INVALID-SUBSCRIBE" (docs/diagnostics.md;
//      finding E24 of the codebase review of 2026-10-04, R-0033, TASK-0321: until
//      TASK-0321 this comment said 1003, and the code sent 1007 from the start).
//      A frame that does not come in SubscriberOptions.SubscribeTimeout is missing,
//      with the same close (ADR-0005 section 5, TASK-0481).
//   3. In a read of the document session (TASK-0034), AttachAndPrime on the
//      broadcaster: under the broadcaster's lock, decide
//      resume vs reset, enqueue initial response + any replay, then add to the
//      subscriber list. This guarantees live events emitted between snapshot
//      and attach can't slip past the replay.
//   4. Await the subscriber's pump completion (client close, lag detected, or
//      the stop of the host; TASK-0062).
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

        // The stop of the host ends the connection. Without it the stop waited for
        // each open socket until the time limit of the host (finding E49 of the
        // review of 2026-10-11, TASK-0062).
        var stopping = context.RequestServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        using var ending = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, stopping);

        SubscribeRequest? subscribeRequest;
        try
        {
            subscribeRequest = await ReadSubscribeAsync(webSocket, options.SubscribeTimeout, ending.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        // Finding E17 of the codebase review of 2026-09-30, part 6 (register entry R-0028, TASK-0481): a client that sends no
        // subscribe frame in the time limit gets the close of a missing frame. The receive stays open, because a cancelled
        // receive aborts the socket, and then no close frame can go. Until TASK-0481 the read had no time limit, so a client
        // that sent nothing held the socket and its task until it left or the host stopped.
        catch (TimeoutException)
        {
            return;
        }
        // Finding E24 of the codebase review of 2026-10-04 (R-0033, TASK-0321): a client that leaves before its
        // subscribe frame ends the read with a WebSocketException. The connection is then gone, and no close
        // frame can go. Until TASK-0321 the exception left the endpoint, and the log held an error with a stack.
        catch (WebSocketException)
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
            pumpGate: options.PumpGate);

        try
        {
            // The session first, the broadcaster second: the same order as a
            // commit, which holds the session when its sink takes the lock of the
            // broadcaster (TASK-0034). In the session no commit runs, so the
            // snapshot, the ring and the version agree, and the first live event
            // follows the last event that the handshake gives (finding E9).
            await host.Session.Read(
                (document, events) =>
                {
                    broadcaster.AttachAndPrime(subscriber, document, events, subscribeRequest);
                    return true;
                },
                context.RequestAborted).ConfigureAwait(false);

            // Finding E24 of the codebase review of 2026-10-04 (R-0033, TASK-0321): the heartbeat starts after the
            // handshake, so its first frame comes after the subscription message.
            subscriber.StartHeartbeat();

            using (stopping.Register(subscriber.Stop))
                await subscriber.RunCompleted.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client left while the handshake waited for the session.
        }
        finally
        {
            broadcaster.Detach(subscriber);
            await subscriber.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<SubscribeRequest?> ReadSubscribeAsync(
        WebSocket socket,
        TimeSpan limit,
        CancellationToken ct)
    {
        const int maxSubscribeBytes = 4096;
        var buffer = new byte[maxSubscribeBytes];

        var receive = socket.ReceiveAsync(buffer, ct);
        WebSocketReceiveResult result;
        try
        {
            result = await receive.WaitAsync(limit, ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // The close frame goes while the receive waits, and the receive ends when the client answers the close. The
            // endpoint waits for that answer, for at most Subscriber.CloseWait: the dispose of the socket aborts it, and on
            // Windows the abort can discard a close frame that the client did not read yet.
            await CloseMissingSubscribeAsync(socket).ConfigureAwait(false);
            try
            {
                await receive.WaitAsync(Subscriber.CloseWait, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TimeoutException or WebSocketException or OperationCanceledException)
            {
            }

            // A receive that ends after the wait fails when the socket is disposed; its failure is expected.
            _ = receive.ContinueWith(static done => _ = done.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }

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

    // The close of a frame that did not come in the time limit: the close frame goes while the receive of the frame waits, so
    // the close does not receive the answer of the client; the wait of ReadSubscribeAsync does.
    private static async Task CloseMissingSubscribeAsync(WebSocket socket)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseOutputAsync(
                    WebSocketCloseStatus.InvalidPayloadData,
                    "E-API-WS-INVALID-SUBSCRIBE",
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* best-effort */ }
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
