using Engine.Contracts;

namespace Engine.Core;

// Question Q4 of the codebase review of 2026-10-04 (finding E18, TASK-0140): Append must not throw for
// a record of the bus. The bus appends the events of a commit after the Document changed, so a throw
// leaves a gap in the sequence, and no code sends a reset for it. Engine.Tests/EventSinkContractTests.cs
// holds each sink of the engine and of the surface to it.
public interface IEventSink
{
    Task Append(EventRecord record, CancellationToken ct = default);
    IReadOnlyList<EventRecord> Snapshot();
    int Count { get; }
}
