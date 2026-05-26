using Engine.Contracts;
using Engine.Core;

namespace Engine.Api.Http.WebSockets;

// Decorator for IEventSink. Per TASK-0010 §2: stores the event in the inner
// sink first (the existing in-memory ring is unchanged), then notifies the
// broadcaster.
//
// Lock ordering: inner.Append takes the InMemoryEventSink lock. We call it
// before broadcaster.OnEvent (which takes the broadcaster lock). Therefore
// a single Apply never holds both locks at once — no deadlock with the
// handshake (which acquires broadcaster lock first, then InMemoryEventSink
// lock via events.Snapshot()).
internal sealed class BroadcastingEventSink : IEventSink
{
    private readonly IEventSink _inner;
    private readonly EventBroadcaster _broadcaster;

    public BroadcastingEventSink(IEventSink inner, EventBroadcaster broadcaster)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
    }

    public async Task Append(EventRecord record, CancellationToken ct = default)
    {
        await _inner.Append(record, ct).ConfigureAwait(false);
        _broadcaster.OnEvent(record);
    }

    public IReadOnlyList<EventRecord> Snapshot() => _inner.Snapshot();

    public int Count => _inner.Count;
}
