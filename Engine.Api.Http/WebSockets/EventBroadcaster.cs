using Engine.Contracts;
using Engine.Core;

namespace Engine.Api.Http.WebSockets;

// Singleton service that fans out Document events to connected Subscribers.
// Per TASK-0010 §2.
//
// Race-safety at attach (TASK-0010 §4): the broadcaster holds a single lock
// for the entire AttachAndPrime sequence. Inside the lock we:
//   1. snapshot the ring + decide resume/reset,
//   2. enqueue the initial response + any replay events into the subscriber,
//   3. add the subscriber to the broadcast list.
// While the lock is held, OnEvent (called from BroadcastingEventSink.Append)
// is blocked, so no live event slips into the subscriber's channel before
// the handshake's replays.
internal sealed class EventBroadcaster
{
    private readonly object _lock = new();
    private readonly List<Subscriber> _subscribers = new();

    public int SubscriberCount
    {
        get { lock (_lock) return _subscribers.Count; }
    }

    public void AttachAndPrime(
        Subscriber subscriber,
        Document document,
        IEventSink events,
        SubscribeRequest request)
    {
        lock (_lock)
        {
            var currentDocId = document.DocumentId;
            var ring = events.Snapshot();
            var latestSeq = ring.Count > 0 ? ring[^1].Seq : 0L;
            var earliestSeq = ring.Count > 0 ? ring[0].Seq : 0L;

            // Decide resume vs reset.
            // Resume is valid when the client matches the running Document,
            // claims a cursor inside the ring, and the replay fits in the
            // subscriber's outbound channel.
            var canResume = request.DocumentId == currentDocId
                && request.LastSeenSeq.HasValue
                && request.LastSeenSeq.Value <= latestSeq
                && (ring.Count == 0 || request.LastSeenSeq.Value >= earliestSeq - 1);

            if (canResume)
            {
                var fromSeq = request.LastSeenSeq!.Value + 1;
                var replaySize = latestSeq - request.LastSeenSeq.Value;

                // Leave one slot for the resume message itself.
                if (replaySize <= subscriber.ChannelCapacity - 1)
                {
                    subscriber.EnqueueDuringHandshake(SubscriptionResumeMessage.Create(fromSeq));
                    foreach (var record in ring)
                    {
                        if (record.Seq >= fromSeq)
                            subscriber.EnqueueDuringHandshake(record);
                    }
                    _subscribers.Add(subscriber);
                    return;
                }
                // Too far behind to fit in the channel — fall through to reset.
            }

            // Reset path: send a snapshot, drop any client-side derived state.
            var snapshot = SnapshotProjector.Project(document);
            subscriber.EnqueueDuringHandshake(
                SubscriptionResetMessage.Create(currentDocId, snapshot));
            _subscribers.Add(subscriber);
        }
    }

    public void Detach(Subscriber subscriber)
    {
        lock (_lock) _subscribers.Remove(subscriber);
    }

    public void OnEvent(EventRecord record)
    {
        Subscriber[] snapshot;
        lock (_lock) snapshot = _subscribers.ToArray();

        foreach (var subscriber in snapshot)
        {
            // TryEnqueue is non-blocking; if a subscriber lags, it self-marks
            // and will drain + close on its own. Other subscribers continue.
            subscriber.TryEnqueue(record);
        }
    }
}
