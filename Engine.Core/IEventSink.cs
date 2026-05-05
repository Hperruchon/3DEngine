using Engine.Contracts;

namespace Engine.Core;

public interface IEventSink
{
    Task Append(EventRecord record, CancellationToken ct = default);
    IReadOnlyList<EventRecord> Snapshot();
    int Count { get; }
}
