using System.Collections.Concurrent;
using Engine.Contracts;
using Engine.Contracts.Drawing;

namespace ThreeDEngine.Desktop;

// ADR-0167 item 2 (phase R30b, TASK-0407): the live sheets of the session in the window, as SessionSketches does for the
// sketches. One read gives the live sheets and the subscription, so no sheet is missed. The events sheet.created and
// sheet.changed carry the whole record, so the window needs no query: the action of the subscription runs inside
// the commit and only puts the record in a queue, and the render loop calls Apply.
internal sealed class SessionSheets : IDisposable
{
    private readonly ConcurrentQueue<(Guid Id, SheetRecord? Sheet)> _changes = new();
    private readonly IDisposable _subscription;
    private readonly List<SheetRecord> _live = [];

    public SessionSheets(ISessionHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        _subscription = host.Session.Read((document, _) =>
        {
            foreach (var sheet in document.Sheets)
                _changes.Enqueue((sheet.Id, sheet));
            return host.Subscribe(OnEvent);
        }).GetAwaiter().GetResult();
        Apply();
    }

    // Inside the commit: no call of the session, and no wait.
    private void OnEvent(EventRecord record)
    {
        if (record.Payload is not IReadOnlyDictionary<string, object?> payload || payload.GetValueOrDefault("sheetId") is not Guid id)
            return;
        switch (record.Kind)
        {
            case "sheet.created":
            case "sheet.changed":
                _changes.Enqueue((id, Record(id, payload)));
                break;
            case "sheet.removed":
                _changes.Enqueue((id, null));
                break;
        }
    }

    // The record of the fields of the event (CommandBus.SheetFields).
    private static SheetRecord Record(Guid id, IReadOnlyDictionary<string, object?> payload) => new(
        id,
        (string)payload["name"]!,
        (string)payload["paper"]!,
        (bool)payload["landscape"]!,
        (bool)payload["thirdAngle"]!,
        (IReadOnlyList<SheetView>)payload["views"]!,
        (IReadOnlyList<SheetDimension>)payload["dimensions"]!);

    // The live sheets in the order of the Document, as the last call of Apply left them.
    public IReadOnlyList<SheetRecord> Live => _live;

    // On the render loop: a created sheet goes at the end, a changed sheet keeps its place, and a removed sheet goes.
    public void Apply()
    {
        while (_changes.TryDequeue(out var change))
        {
            var index = _live.FindIndex(sheet => sheet.Id == change.Id);
            if (change.Sheet is null)
            {
                if (index >= 0)
                    _live.RemoveAt(index);
            }
            else if (index >= 0)
                _live[index] = change.Sheet;
            else
                _live.Add(change.Sheet);
        }
    }

    public void Dispose() => _subscription.Dispose();
}
