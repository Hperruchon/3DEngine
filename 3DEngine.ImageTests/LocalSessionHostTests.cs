using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Geometry.Manifold;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// Phase R14e2 (ADR-0072 item 2, TASK-0226): the session of a host with no surface, as on the phone. These tests run it
// on the desktop, with no window and no Android device.
public sealed class LocalSessionHostTests
{
    private static EventRecord Event(long seq) => new(seq, DateTime.UnixEpoch, Guid.Empty, null, "probe", null);

    // A sink must not throw, by contract (DesktopSinkContractTests). An action that throws is removed, the inner sink
    // still holds each event, and each other subscription gets each event in order.
    [Fact]
    public async Task An_Action_That_Throws_Is_Removed_And_The_Sink_Does_Not_Throw()
    {
        var inner = new InMemoryEventSink();
        var sink = new FanOutSink(inner);
        var seen = new List<long>();
        var calls = 0;
        using var failing = sink.Listen(_ => { calls++; throw new InvalidOperationException("probe"); });
        using var kept = sink.Listen(record => seen.Add(record.Seq));

        await sink.Append(Event(1));
        await sink.Append(Event(2));

        Assert.Equal(1, calls);
        Assert.Equal([1L, 2L], seen);
        Assert.Equal(2, inner.Count);
        Assert.Equal(inner.Snapshot(), sink.Snapshot());
    }

    // Finding V100 of the codebase review of 2026-11-06 (c) (TASK-0232): the report of a removed action ran inside the
    // commit, so a report that threw came out of Append. The report runs out of the commit, and the next action gets the event.
    [Fact]
    public async Task A_Report_That_Throws_Does_Not_Stop_The_Commit()
    {
        var inner = new InMemoryEventSink();
        var sink = new FanOutSink(inner) { Report = _ => throw new IOException("probe") };
        var seen = new List<long>();
        using var failing = sink.Listen(_ => throw new InvalidOperationException("probe"));
        using var kept = sink.Listen(record => seen.Add(record.Seq));

        await sink.Append(Event(1));

        Assert.Equal([1L], seen);
        Assert.Equal(1, inner.Count);
    }

    // The report still comes, with its code.
    [Fact]
    public async Task The_Report_Of_A_Removed_Action_Comes_Out_Of_The_Commit()
    {
        var reported = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sink = new FanOutSink(new InMemoryEventSink()) { Report = message => reported.TrySetResult(message) };
        using var failing = sink.Listen(_ => throw new InvalidOperationException("probe"));

        await sink.Append(Event(1));

        Assert.StartsWith(DiagnosticCodes.ApiSubscriptionRemoved, await reported.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    // A subscription after Dispose gets no later event.
    [Fact]
    public async Task A_Disposed_Subscription_Gets_No_Later_Event()
    {
        var sink = new FanOutSink(new InMemoryEventSink());
        var seen = new List<long>();
        var subscription = sink.Listen(record => seen.Add(record.Seq));

        await sink.Append(Event(1));
        subscription.Dispose();
        await sink.Append(Event(2));

        Assert.Equal([1L], seen);
    }

    // The session of the phone gives each committed event to a subscription in the order of the session, and the
    // document has no file.
    [Fact]
    public async Task The_Local_Session_Gives_Each_Committed_Event_In_Order()
    {
        using var host = new LocalSessionHost(new ManifoldGeometryBackend());
        var seen = new List<EventRecord>();
        using var subscription = host.Subscribe(seen.Add);

        var result = await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });

        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.Null(host.FileName);
        Assert.NotEmpty(seen);
        Assert.Equal(seen.Select(record => record.Seq).Order(), seen.Select(record => record.Seq));
        Assert.Equal(1, await host.Session.Read((document, _) => document.Bodies.Count));
    }
}
