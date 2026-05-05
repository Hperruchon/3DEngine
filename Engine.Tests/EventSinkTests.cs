using Engine.Contracts;
using Engine.Core;

namespace Engine.Tests;

public class EventSinkTests
{
    private static EventRecord MakeEvent(long seq) => new(
        Seq: seq,
        Timestamp: DateTime.UtcNow,
        DocumentId: Guid.Empty,
        CauseCommandId: null,
        Kind: "test.event",
        Payload: null);

    [Fact]
    public async Task Ring_Evicts_Oldest_When_Capacity_Exceeded()
    {
        const int capacity = 4;
        var sink = new InMemoryEventSink(capacity);

        for (var i = 1; i <= capacity + 1; i++)
            await sink.Append(MakeEvent(i));

        var events = sink.Snapshot();
        Assert.Equal(capacity, events.Count);

        // Oldest (Seq=1) must have been dropped.
        Assert.DoesNotContain(events, e => e.Seq == 1);

        // Newest (Seq=capacity+1) must still be present.
        Assert.Contains(events, e => e.Seq == capacity + 1);
    }

    [Fact]
    public async Task Buffered_Range_Is_Contiguous_In_Seq_After_Eviction()
    {
        const int capacity = 8;
        const int total = capacity + 5;
        var sink = new InMemoryEventSink(capacity);

        for (var i = 1; i <= total; i++)
            await sink.Append(MakeEvent(i));

        var events = sink.Snapshot();
        Assert.Equal(capacity, events.Count);

        // After eviction the surviving Seqs are (total - capacity + 1) .. total, contiguous.
        var expectedFirst = total - capacity + 1;
        for (var i = 0; i < events.Count; i++)
            Assert.Equal(expectedFirst + i, events[i].Seq);
    }
}
