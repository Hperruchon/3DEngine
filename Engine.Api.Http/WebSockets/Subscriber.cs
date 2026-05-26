using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Engine.Api.Http.Json;

namespace Engine.Api.Http.WebSockets;

// One Subscriber per WebSocket connection. Owns:
//   - the WebSocket itself,
//   - a bounded outbound Channel<object> (the "per-subscriber outbound queue"
//     from ADR-0005 §6),
//   - a pump task draining the channel to WebSocket.SendAsync,
//   - a heartbeat task firing after idle,
//   - a receive task watching for client-side close.
//
// Thread safety: TryEnqueue is callable from any thread (the broadcaster pushes
// from CommandBus.Apply). Channel.Writer.TryWrite is thread-safe. The pump and
// receive tasks are the only readers of socket/channel respectively.
internal sealed class Subscriber : IAsyncDisposable
{
    public const int DefaultChannelCapacity = 1024;
    public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(30);

    private readonly WebSocket _socket;
    private readonly Channel<object> _outbound;
    private readonly TimeSpan _heartbeatInterval;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pumpTask;
    private readonly Task _heartbeatTask;
    private readonly Task _receiveTask;
    private long _lastSendTicks;
    private volatile bool _lagged;

    public int ChannelCapacity { get; }

    public bool IsLagged => _lagged;

    /// Completes when the pump exits — i.e. the connection should be torn down.
    public Task RunCompleted => _pumpTask;

    private readonly TimeSpan _pumpDelay;

    public Subscriber(
        WebSocket socket,
        int channelCapacity = DefaultChannelCapacity,
        TimeSpan? heartbeatInterval = null,
        TimeSpan? pumpDelay = null)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        ChannelCapacity = channelCapacity;

        _outbound = Channel.CreateBounded<object>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });
        _heartbeatInterval = heartbeatInterval ?? DefaultHeartbeatInterval;
        _pumpDelay = pumpDelay ?? TimeSpan.Zero;
        _lastSendTicks = DateTime.UtcNow.Ticks;

        _pumpTask = Task.Run(PumpAsync);
        _heartbeatTask = Task.Run(HeartbeatAsync);
        _receiveTask = Task.Run(ReceiveLoopAsync);
    }

    /// Enqueue a message. Returns false if the channel is full (subscriber is
    /// then marked lagged and will be torn down). Used by the broadcaster.
    public bool TryEnqueue(object message)
    {
        if (_lagged || _cts.IsCancellationRequested)
            return false;

        if (_outbound.Writer.TryWrite(message))
            return true;

        // Full — per ADR-0005 §6: disconnect this subscriber. Do not block,
        // do not drop, do not coalesce.
        _lagged = true;
        _outbound.Writer.TryComplete();
        return false;
    }

    /// Synchronous enqueue used during handshake, before the broadcaster sees
    /// us. Caller guarantees the message count fits in the channel.
    /// Returns false only on impossible conditions (queue already completed).
    public bool EnqueueDuringHandshake(object message)
        => _outbound.Writer.TryWrite(message);

    private async Task PumpAsync()
    {
        try
        {
            await foreach (var message in _outbound.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                if (_pumpDelay > TimeSpan.Zero)
                    await Task.Delay(_pumpDelay, _cts.Token).ConfigureAwait(false);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(message, message.GetType(), ApiJson.Options);
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, _cts.Token).ConfigureAwait(false);
                Interlocked.Exchange(ref _lastSendTicks, DateTime.UtcNow.Ticks);
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { /* client gone */ }
        finally
        {
            _cts.Cancel();
            await CloseSocketAsync().ConfigureAwait(false);
        }
    }

    private async Task HeartbeatAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var elapsedTicks = DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastSendTicks);
                var remainingTicks = _heartbeatInterval.Ticks - elapsedTicks;
                if (remainingTicks > 0)
                    await Task.Delay(TimeSpan.FromTicks(remainingTicks), _cts.Token).ConfigureAwait(false);

                var idleTicks = DateTime.UtcNow.Ticks - Interlocked.Read(ref _lastSendTicks);
                if (idleTicks >= _heartbeatInterval.Ticks - TimeSpan.TicksPerMillisecond)
                {
                    // Don't lag-disconnect on heartbeat queue overflow; if the
                    // channel is already full a real event will trip the lag
                    // path on its own.
                    _outbound.Writer.TryWrite(HeartbeatMessage.Instance);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ReceiveLoopAsync()
    {
        // We don't expect application messages from the client after subscribe.
        // Drain whatever arrives. The meaningful signal is Close.
        var buffer = new byte[1024];
        try
        {
            while (!_cts.IsCancellationRequested && _socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer, _cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _cts.Cancel();
                    _outbound.Writer.TryComplete();
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException)
        {
            _cts.Cancel();
            _outbound.Writer.TryComplete();
        }
    }

    private async Task CloseSocketAsync()
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            var status = _lagged ? WebSocketCloseStatus.PolicyViolation : WebSocketCloseStatus.NormalClosure;
            var reason = _lagged ? "subscriber.lagged" : "normal closure";
            try
            {
                await _socket.CloseOutputAsync(status, reason, CancellationToken.None).ConfigureAwait(false);
            }
            catch { /* best-effort */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _outbound.Writer.TryComplete();
        try
        {
            await Task.WhenAll(_pumpTask, _heartbeatTask, _receiveTask).ConfigureAwait(false);
        }
        catch { /* swallow background-task failures during disposal */ }
        _cts.Dispose();
    }
}
