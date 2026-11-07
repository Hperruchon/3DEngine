using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;

namespace ThreeDEngine.Desktop;

// ADR-0072 item 2 (phase R14e2, TASK-0226): the session of a host with no surface, as on the phone (ADR-0061 item 4).
// It composes a DocumentSession on the backend that the host gives, with a sink that gives each committed event to each
// subscription after the inner sink holds it, as EngineHost does through its broadcaster. The document has no file in
// this slice; the picker of Android comes in a later slice of R14e.
internal sealed class LocalSessionHost : ISessionHost, IDisposable
{
    private readonly IGeometryBackend _backend;
    private readonly FanOutSink _sink;

    // ADR-0083 item 2 (TASK-0260): assets is the store of the phone, which the import of an STL file reads.
    public LocalSessionHost(IGeometryBackend backend, Engine.Core.Assets.IAssetSource? assets = null)
    {
        ArgumentNullException.ThrowIfNull(backend);
        _backend = backend;
        var kit = EngineHosting.CreateDefault(backend, assets: assets);
        _sink = new FanOutSink(kit.Events);
        Session = new DocumentSession(kit, _sink);
    }

    public DocumentSession Session { get; }

    public string? FileName => null;

    public IDisposable Subscribe(Action<EventRecord> action) => _sink.Listen(action);

    // The session closes first and waits for a command that still runs, then the backend is freed, as EngineHost does.
    public void Dispose()
    {
        Session.Close();
        (_backend as IDisposable)?.Dispose();
    }
}

// The sink of LocalSessionHost. It stores the event in the inner sink first, then gives it to each subscription, in the
// order of the commits. A sink must not throw, by contract (DesktopSinkContractTests): an action that throws is removed,
// with W-API-SUBSCRIPTION-REMOVED on the error output, as for a subscription of EngineHost.
internal sealed class FanOutSink(IEventSink inner) : IEventSink
{
    private readonly object _lock = new();
    private readonly List<Listener> _listeners = [];

    public async Task Append(EventRecord record, CancellationToken ct = default)
    {
        await inner.Append(record, ct).ConfigureAwait(false);
        Listener[] listeners;
        lock (_lock)
            listeners = [.. _listeners];
        foreach (var listener in listeners)
        {
            try
            {
                listener.Action(record);
            }
            catch (Exception exception)
            {
                if (Remove(listener))
                    Announce($"{DiagnosticCodes.ApiSubscriptionRemoved}: the action of a subscription threw "
                        + $"{exception.GetType().Name} at the event {record.Seq} ({record.Kind}), and it receives no later "
                        + $"event: {exception.Message}");
            }
        }
    }

    // The channel of the report. The test gives its own, so that it does not change the error output of the process.
    internal Action<string> Report { get; set; } = message => Console.Error.WriteLine(message);

    // Finding V100 of the codebase review of 2026-11-06 (c) (TASK-0232): the report ran inside the commit, so a report that
    // threw stopped the commit after the Document held its change, the defect that E67 removed from EventBroadcaster. The
    // report runs on the thread pool, out of the commit, as EventBroadcaster.Announce does, and a report that throws is lost.
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

    public IReadOnlyList<EventRecord> Snapshot() => inner.Snapshot();

    public int Count => inner.Count;

    public IDisposable Listen(Action<EventRecord> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var listener = new Listener(this, action);
        lock (_lock)
            _listeners.Add(listener);
        return listener;
    }

    private bool Remove(Listener listener)
    {
        lock (_lock)
            return _listeners.Remove(listener);
    }

    // One subscription. The list compares entries by reference.
    private sealed class Listener(FanOutSink sink, Action<EventRecord> action) : IDisposable
    {
        public Action<EventRecord> Action => action;

        public void Dispose() => sink.Remove(this);
    }
}
