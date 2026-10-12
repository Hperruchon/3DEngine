using Engine.Api.Http;
using Engine.Contracts;
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
