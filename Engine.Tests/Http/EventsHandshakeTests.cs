using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Engine.Api.Http.Endpoints;
using Engine.Api.Http.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Engine.Tests.Http;

// Two parts of finding E24 of the codebase review of 2026-10-04 (R-0033, TASK-0321), with a socket of the
// test in place of the network, so that each test controls when a frame arrives and when the client goes.
public sealed class EventsHandshakeTests
{
    // A client that leaves before its subscribe frame ends the read with a WebSocketException. Until
    // TASK-0321 the exception left the endpoint, and the log held an error with a stack.
    [Fact]
    public async Task A_Client_That_Leaves_Before_Its_Subscribe_Frame_Ends_The_Request_With_No_Exception()
    {
        var socket = new TestSocket { Leave = true };
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpWebSocketFeature>(new UpgradeFeature(socket));
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IHostApplicationLifetime>(new Lifetime())
            .BuildServiceProvider();

        // The endpoint returns before it reads the host and the broadcaster.
        await EventsEndpoint.Handle(context, host: null!, broadcaster: null!, SubscriberOptions.Default);

        Assert.Empty(socket.Sent);
    }

    // The heartbeat started in the constructor of the subscriber, before the handshake, so a heartbeat
    // could come before the subscription message. It now starts after the handshake.
    [Fact]
    public async Task No_Heartbeat_Comes_Before_The_Subscription_Message()
    {
        var socket = new TestSocket();
        var subscriber = new Subscriber(socket, heartbeatInterval: TimeSpan.FromMilliseconds(20));
        try
        {
            // Ten intervals with no handshake, as a handshake that waits for a long command.
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            Assert.Empty(socket.Sent);

            subscriber.EnqueueDuringHandshake(SubscriptionResumeMessage.Create(1));
            subscriber.StartHeartbeat();

            using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (socket.Sent.Count < 2)
                await Task.Delay(10, limit.Token);

            Assert.Equal("subscription.resume", Kind(socket.Sent.First()));
            Assert.Equal("heartbeat", Kind(socket.Sent.Skip(1).First()));
        }
        finally
        {
            await subscriber.DisposeAsync();
        }
    }

    private static string? Kind(string frame) => JsonDocument.Parse(frame).RootElement.GetProperty("kind").GetString();

    // A socket that keeps each frame that the server sends. A receive waits for the end of the test, or
    // throws as a connection that the client dropped when Leave is set.
    private sealed class TestSocket : WebSocket
    {
        public bool Leave { get; init; }
        public System.Collections.Concurrent.ConcurrentQueue<string> Sent { get; } = new();

        public override WebSocketCloseStatus? CloseStatus => null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => WebSocketState.Open;
        public override string? SubProtocol => null;

        public override void Abort()
        {
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override void Dispose()
        {
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (Leave)
                throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new OperationCanceledException(cancellationToken);
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            Sent.Enqueue(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
            return Task.CompletedTask;
        }
    }

    private sealed class UpgradeFeature(WebSocket socket) : IHttpWebSocketFeature
    {
        public bool IsWebSocketRequest => true;

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) => Task.FromResult(socket);
    }

    private sealed class Lifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
