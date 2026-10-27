using Engine.Api.Http;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;

namespace Engine.Tests.Http;

// The subscription of a host to the events of its own session (ADR-0024 item 1,
// TASK-0071). ADR-0024 names this file in enforced-by. The desktop uses it to draw
// each live body; it must give the same events as /events, in the order of the
// commits, and a failing action must not stop a commit.
public class EngineHostSubscriptionTests
{
    [Fact]
    public async Task A_Subscriber_Receives_Each_Event_Of_A_Commit_In_Order()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var received = new List<string>();
        using var subscription = host.Subscribe(record => received.Add(record.Kind));

        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        Assert.Equal(CommandStatus.Applied, (await host.Session.Apply(box)).Status);

        Assert.Equal(["command.applied", "body.created"], received);
    }

    [Fact]
    public async Task A_Disposed_Subscription_Receives_No_Later_Event()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var count = 0;
        var subscription = host.Subscribe(_ => count++);

        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        subscription.Dispose();
        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });

        Assert.Equal(2, count);
    }

    // An action that throws is removed, the commit stays applied, and each other
    // action still receives the events.
    [Fact]
    public async Task An_Action_That_Throws_Is_Removed_And_The_Commit_Continues()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var failures = 0;
        var other = new List<string>();
        using var failing = host.Subscribe(_ => { failures++; throw new InvalidOperationException("a defect of the host"); });
        using var good = host.Subscribe(record => other.Add(record.Kind));

        var first = await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var second = await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });

        Assert.Equal(CommandStatus.Applied, first.Status);
        Assert.Equal(CommandStatus.Applied, second.Status);
        Assert.Equal(1, failures);
        Assert.Equal(4, other.Count);
    }

    // Finding E62 of the codebase review of 2026-10-13 (TASK-0074): the removal of an
    // action is reported once, with its registered code.
    [Fact]
    public async Task The_Removal_Of_An_Action_Is_Reported_With_Its_Code()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var reports = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.Broadcaster.Report = message =>
        {
            reports.Enqueue(message);
            reported.TrySetResult();
        };
        using var failing = host.Subscribe(_ => throw new InvalidOperationException("a defect of the host"));

        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        await reported.Task.WaitAsync(Limit);
        await Task.Delay(100);

        var report = Assert.Single(reports);
        Assert.StartsWith(DiagnosticCodes.ApiSubscriptionRemoved + ":", report, StringComparison.Ordinal);
        Assert.Contains("a defect of the host", report, StringComparison.Ordinal);
    }

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

    // Finding E67 of the codebase review of 2026-10-17 (TASK-0079): the report ran
    // inside the commit. A report that threw made Apply throw after the Document held
    // the body, and the other actions missed the later events of that commit.
    [Fact]
    public async Task A_Report_That_Throws_Does_Not_Stop_The_Commit()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        host.Broadcaster.Report = _ => throw new InvalidOperationException("a defect of the report");
        using var failing = host.Subscribe(_ => throw new InvalidOperationException("a defect of the host"));
        var other = new List<string>();
        using var good = host.Subscribe(record => other.Add(record.Kind));

        var result = await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });

        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.Equal(["command.applied", "body.created"], other);
    }

    // E67: a report that blocked held the serial section for its whole wait.
    [Fact]
    public async Task A_Report_That_Blocks_Does_Not_Hold_The_Session()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        using var release = new ManualResetEventSlim();
        host.Broadcaster.Report = _ => release.Wait(Limit);
        using var failing = host.Subscribe(_ => throw new InvalidOperationException("a defect of the host"));

        try
        {
            // On the thread pool: a commit with no real wait runs to its end before
            // Apply returns its task, so a report that blocked the commit would also
            // block this line and pass the limit below.
            var apply = Task.Run(() => host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }));
            var read = await apply.WaitAsync(TimeSpan.FromSeconds(5))
                .ContinueWith(_ => host.Session.Read((document, _) => document.Version)).Unwrap()
                .WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, read);
        }
        finally
        {
            release.Set();
        }
    }

    // Finding E63 (TASK-0074): an action cannot change the payload that the ring of
    // /events keeps.
    [Fact]
    public async Task An_Action_Cannot_Change_A_Payload()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        EventRecord? created = null;
        using var subscription = host.Subscribe(record =>
        {
            if (record.Kind == "body.created")
                created = record;
        });

        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });

        var payload = Assert.IsAssignableFrom<IDictionary<string, object?>>(created!.Payload);
        Assert.True(payload.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => payload["bodyId"] = Guid.Empty);
    }

    // Finding E64 (TASK-0074): one method subscribed two times is two subscriptions,
    // and the disposal of the first one, even two times, leaves the second one.
    [Fact]
    public async Task Each_Subscription_Is_One_Entry_That_Its_Disposal_Removes_Once()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        var counter = new Counter();
        var first = host.Subscribe(counter.Count);
        using var second = host.Subscribe(counter.Count);

        first.Dispose();
        first.Dispose();
        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });

        Assert.Equal(2, counter.Events);
    }

    private sealed class Counter
    {
        public int Events { get; private set; }

        public void Count(EventRecord record) => Events++;
    }

    // The way of ADR-0024: read the live bodies and subscribe in one read of the
    // session, so that no commit falls between them.
    [Fact]
    public async Task A_Read_And_A_Subscription_In_One_Section_Miss_No_Body()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), "managed", "test");
        await host.Session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var created = new List<Guid>();

        var (live, subscription) = await host.Session.Read((document, _) =>
            (document.Bodies.Select(body => body.Handle.Id).ToList(),
             host.Subscribe(record =>
             {
                 if (record.Kind == "body.created")
                     created.Add((Guid)((IReadOnlyDictionary<string, object?>)record.Payload!)["bodyId"]!);
             })));
        using (subscription)
        {
            var second = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
            await host.Session.Apply(second);

            Assert.Single(live);
            Assert.Equal([second.CommandId], created);
        }
    }
}
