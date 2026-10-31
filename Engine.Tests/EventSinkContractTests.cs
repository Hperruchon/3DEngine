using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Core;

namespace Engine.Tests;

// Question Q4 of the codebase review of 2026-10-04 (finding E18, TASK-0140): a sink must not throw,
// by contract, and a test holds each sink to it. The bus appends the events of a commit after the
// Document changed, so a throw left a gap in the sequence, and nothing sent a reset.
public sealed class EventSinkContractTests
{
    // A new sink of the engine or of the surface must enter this list with a test below.
    private static readonly Type[] TestedSinks = [typeof(InMemoryEventSink), typeof(BroadcastingEventSink)];

    [Fact]
    public void Each_Sink_Of_The_Engine_Has_A_Test_That_It_Does_Not_Throw()
    {
        var sinks = new[] { typeof(IEventSink).Assembly, typeof(EngineHost).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEventSink).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(TestedSinks.OrderBy(type => type.FullName, StringComparer.Ordinal), sinks);
    }

    // The ring evicts the oldest event when it is full, and the range that stays is contiguous.
    [Fact]
    public async Task The_Ring_Does_Not_Throw_When_It_Is_Full()
    {
        var sink = new InMemoryEventSink(capacity: 2);

        for (var seq = 1; seq <= 5; seq++)
            Assert.Null(await Record.ExceptionAsync(() => sink.Append(Event(seq), CancellationToken.None)));

        Assert.Equal([4L, 5L], sink.Snapshot().Select(record => record.Seq));
    }

    // An action of a subscription that throws is removed and reported (ADR-0024 item 1), and the append
    // continues into the ring.
    [Fact]
    public async Task The_Broadcasting_Sink_Does_Not_Throw_When_An_Action_Throws()
    {
        var broadcaster = new EventBroadcaster();
        var ring = new InMemoryEventSink();
        var sink = new BroadcastingEventSink(ring, broadcaster);
        using var failing = broadcaster.Listen(_ => throw new InvalidOperationException("a defect of the host"));

        Assert.Null(await Record.ExceptionAsync(() => sink.Append(Event(1), CancellationToken.None)));
        Assert.Null(await Record.ExceptionAsync(() => sink.Append(Event(2), CancellationToken.None)));

        Assert.Equal([1L, 2L], ring.Snapshot().Select(record => record.Seq));
    }

    private static EventRecord Event(long seq)
        => new(seq, DateTime.UnixEpoch, Guid.Empty, null, "command.applied", null);
}
