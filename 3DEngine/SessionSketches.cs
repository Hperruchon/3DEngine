using System.Collections.Concurrent;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core.Queries;

namespace ThreeDEngine.Desktop;

// The projection of the live sketches of the session into the desktop (ADR-0033 item 13).
// As SessionBodies does for the bodies, it reads the live sketches and subscribes to the
// events of the session in one read, so it misses no sketch. The action of the
// subscription runs inside each commit and only puts the identifier in a queue; the render
// loop calls Apply, which reads each changed sketch with the query GetSketch outside the
// commit. The desktop draws a sketch and does not change it: K2b gives the tools.
internal sealed class SessionSketches : IDisposable
{
    private readonly EngineHost _host;
    private readonly ConcurrentQueue<(Guid SketchId, bool Removed)> _changes = new();
    private readonly IDisposable _subscription;
    private readonly Dictionary<Guid, SketchInfo> _live = new();

    public SessionSketches(EngineHost host)
    {
        _host = host;
        _subscription = host.Session.Read((document, _) =>
        {
            foreach (var sketch in document.Sketches)
                _changes.Enqueue((sketch.Id, false));
            return host.Subscribe(OnEvent);
        }).GetAwaiter().GetResult();
    }

    // Inside the commit: no call of the session, and no wait.
    private void OnEvent(EventRecord record)
    {
        if (record.Payload is not IReadOnlyDictionary<string, object?> payload
            || payload.GetValueOrDefault("sketchId") is not Guid sketchId)
            return;

        switch (record.Kind)
        {
            case "sketch.created":
            case "sketch.changed":
                _changes.Enqueue((sketchId, false));
                break;
            case "sketch.removed":
                _changes.Enqueue((sketchId, true));
                break;
        }
    }

    // Each live sketch, as the last call of Apply left them.
    public IReadOnlyCollection<SketchInfo> Live => _live.Values;

    // On the render loop. A failure of one sketch is written as a warning, and the next
    // sketch continues, as in SessionBodies.
    public void Apply()
    {
        while (_changes.TryDequeue(out var change))
        {
            if (change.Removed)
            {
                _live.Remove(change.SketchId);
                continue;
            }

            var result = _host.Session.Query<SketchInfo>(new GetSketchQuery { SketchId = change.SketchId }).GetAwaiter().GetResult();
            if (result.Result is { } sketch)
                _live[change.SketchId] = sketch;
            else if (result.Error?.Code != Engine.Core.DiagnosticCodes.SketchNotFound)
                Console.WriteLine($"[WARN] The sketch {change.SketchId} was not read: {result.Error?.Code} {result.Error?.Message}");
        }
    }

    public void Dispose() => _subscription.Dispose();
}
