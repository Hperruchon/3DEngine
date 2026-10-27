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
//
// The lock alone is not sufficient. A commit changes the Document and appends
// its events in more than one step. A handshake between two steps saw a version
// and a ring that did not agree, and lost or repeated an event (finding E9 of
// the codebase review of 2026-09-30). The caller must therefore call AttachAndPrime
// inside DocumentSession.Read. In the session no commit runs, and the order of
// the locks is the order of a commit: the session first, this lock second
// (TASK-0034).
internal sealed class EventBroadcaster
{
    private readonly object _lock = new();
    private readonly List<Subscriber> _subscribers = new();
    private readonly List<Listener> _listeners = new();

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
            var snapshot = SnapshotProjector.Project(document, latestSeq);
            subscriber.EnqueueDuringHandshake(
                SubscriptionResetMessage.Create(currentDocId, snapshot));
            _subscribers.Add(subscriber);
        }
    }

    public void Detach(Subscriber subscriber)
    {
        lock (_lock) _subscribers.Remove(subscriber);
    }

    // The report of an action that a throw removed (finding E62 of the codebase review
    // of 2026-10-13, TASK-0074). The default writes one line with the code to the error
    // output; a test replaces it. Until TASK-0074 the removal wrote nothing, and a host
    // stopped following its session with no message. The report runs after the commit,
    // on the thread pool (Announce).
    internal Action<string> Report { get; set; } = message => Console.Error.WriteLine(message);

    // An action of the host that owns the session (ADR-0024 item 1, TASK-0071). It
    // receives each later event, inside the commit, until the result is disposed. An
    // event of a commit that is in progress when Dispose returns can still arrive. A
    // caller that also reads the session calls this inside DocumentSession.Read, so
    // that no commit falls between the read and the first event.
    //
    // Each call gives one entry, and its disposal removes that entry once. Until
    // TASK-0074 the disposal removed the first entry with an equal delegate, so one
    // method subscribed two times lost the events of its second subscription (E64).
    public IDisposable Listen(Action<EventRecord> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var listener = new Listener(this, action);
        lock (_lock) _listeners.Add(listener);
        return listener;
    }

    public void OnEvent(EventRecord record)
    {
        Subscriber[] snapshot;
        Listener[] listeners;
        lock (_lock)
        {
            snapshot = _subscribers.ToArray();
            listeners = _listeners.ToArray();
        }

        foreach (var subscriber in snapshot)
        {
            // TryEnqueue is non-blocking; if a subscriber lags, it self-marks
            // and will drain + close on its own. Other subscribers continue.
            subscriber.TryEnqueue(record);
        }

        // An action that throws is a defect of its host. It is removed and reported,
        // and the commit and each other action continue (ADR-0024 item 1).
        foreach (var listener in listeners)
        {
            try
            {
                listener.Action(record);
            }
            catch (Exception exception)
            {
                if (Remove(listener))
                {
                    Announce($"{DiagnosticCodes.ApiSubscriptionRemoved}: the action of a subscription threw "
                        + $"{exception.GetType().Name} at the event {record.Seq} ({record.Kind}), and it receives no later "
                        + $"event: {exception.Message}");
                }
            }
        }
    }

    // The report runs on the thread pool, out of the commit, and a report that throws
    // is lost (finding E67 of the codebase review of 2026-10-17, TASK-0079). Until
    // TASK-0079 the report ran inside the commit: a report that threw stopped the
    // commit after the Document held its change, so each later action and the ring
    // missed the later events of that commit, and a report that blocked held the
    // serial section. The flow of the commit does not reach the report, so a report
    // that calls the session waits like each other caller.
    private void Announce(string message)
    {
        var report = Report;
        using (ExecutionContext.SuppressFlow())
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    report(message);
                }
                catch (Exception)
                {
                    // The report is the last channel for this message; no other exists.
                }
            });
        }
    }

    private bool Remove(Listener listener)
    {
        lock (_lock) return _listeners.Remove(listener);
    }

    // One subscription. The list compares entries by reference.
    private sealed class Listener(EventBroadcaster broadcaster, Action<EventRecord> action) : IDisposable
    {
        public Action<EventRecord> Action => action;

        public void Dispose() => broadcaster.Remove(this);
    }
}
