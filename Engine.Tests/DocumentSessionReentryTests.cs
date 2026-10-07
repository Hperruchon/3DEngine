using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests;

// Finding E20 of the codebase review of 2026-10-04, register entry R-0033: the
// session has one serial section, and a call from inside the section into the
// same session waited for the section that its own flow holds. With no time
// limit it waited forever, and each other client waited behind it. TASK-0051.
//
// Before the correction each test failed. The call from the sink ran into its
// time limit of two seconds, and the call that a read function started ran after
// the read and returned a result.
public class DocumentSessionReentryTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task A_Sink_That_Calls_The_Session_Gets_An_Exception_And_Does_Not_Wait()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        var sink = new CallingSink(kit.Events);
        var session = new DocumentSession(kit, sink);
        sink.Session = session;

        var first = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });

        Assert.Equal(CommandStatus.Applied, first.Status);
        var error = Assert.IsType<InvalidOperationException>(sink.Failure);
        Assert.Contains("serial section", error.Message, StringComparison.Ordinal);

        // The refusal left the section free: the next command and a read from
        // outside run at once.
        sink.Session = null;
        using var limit = new CancellationTokenSource(Limit);
        var second = await session.Apply(new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 }, limit.Token);
        Assert.Equal(CommandStatus.Applied, second.Status);
        Assert.Equal(2, await session.Read((document, _) => document.Version, limit.Token));
    }

    [Fact]
    public async Task A_Read_Function_That_Starts_A_Query_Gets_An_Exception()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await session.Apply(box);

        var started = await session.Read((_, _) =>
            session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = box.CommandId }));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => started);
        Assert.Contains("serial section", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_Read_Function_That_Starts_A_Command_Gets_An_Exception_And_Nothing_Is_Applied()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));

        var started = await session.Read((_, _) =>
            session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => started);
        Assert.Equal(0, await session.Read((document, _) => document.Version));
    }

    // A sink that calls the session for each event, as a projection that asks a
    // query when a body arrives would do (TASK-0038). It keeps the first failure,
    // because a sink must not throw (finding E18).
    private sealed class CallingSink : IEventSink
    {
        private readonly IEventSink _inner;

        public CallingSink(IEventSink inner) => _inner = inner;

        public DocumentSession? Session { get; set; }

        public Exception? Failure { get; private set; }

        public int Count => _inner.Count;

        public IReadOnlyList<EventRecord> Snapshot() => _inner.Snapshot();

        public async Task Append(EventRecord record, CancellationToken ct = default)
        {
            await _inner.Append(record, ct).ConfigureAwait(false);
            if (Session is null)
                return;

            try
            {
                using var limit = new CancellationTokenSource(Limit);
                await Session.Read((document, _) => document.Version, limit.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Failure ??= ex;
            }
        }
    }
}
