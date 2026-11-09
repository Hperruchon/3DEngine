using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;

namespace Engine.Tests;

// The open part of finding T2 of the codebase review of 2026-09-23 (R-0027, TASK-0321): no test made the
// backend throw during a command through the command bus and then checked that the log, the bodies, the
// events and the backend did not change.
public sealed class CommandBusBackendFailureTests
{
    [Fact]
    public async Task A_Backend_That_Throws_Changes_No_Log_No_Body_No_Event_And_No_Backend_State()
    {
        var backend = new FailingBackend();
        var document = new Document();
        var registry = new CommandRegistry();
        registry.Register(new CreateBoxCommandHandler());
        var sink = new InMemoryEventSink();
        var bus = new CommandBus(document, registry, sink, backend);

        var first = await bus.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        Assert.Equal(CommandStatus.Applied, first.Status);
        var log = document.Log.ToList();
        var bodies = document.Bodies.ToList();
        var events = sink.Snapshot().Count;
        var version = document.Version;

        backend.Fail = true;
        var failing = new CreateBoxCommand { SizeX = 4, SizeY = 5, SizeZ = 6 };
        var result = await bus.Apply(failing);

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.GeomNativeOp, result.Error!.Code);
        Assert.Equal(log, document.Log);
        Assert.Equal(bodies, document.Bodies);
        Assert.Equal(version, document.Version);
        var added = Assert.Single(sink.Snapshot().Skip(events));
        Assert.Equal("command.rejected", added.Kind);
        Assert.Equal(1, backend.Inner.BodyCount);
        Assert.False(backend.Inner.Contains(new BodyHandle(failing.CommandId)));
        // The second call reached the backend, so the test proves the path of a throw.
        Assert.Equal(2, backend.Calls);
    }

    // The managed backend, with a switch that makes CreateBox throw before it changes the backend, as a
    // failure of the native library would.
    private sealed class FailingBackend : IGeometryBackend, IMeshOps
    {
        public InProcessMeshBackend Inner { get; } = new();
        public bool Fail { get; set; }
        public int Calls { get; private set; }

        public BackendIdentity Identity => Inner.Identity;
        public BackendCapabilities Capabilities => Inner.Capabilities;

        public T? TryGet<T>() where T : class => this as T ?? Inner.TryGet<T>();

        public void CreateBox(BodyHandle handle, BoxParameters parameters)
        {
            Calls++;
            if (Fail)
                throw new InvalidOperationException("The native library failed.");
            Inner.CreateBox(handle, parameters);
        }
    }
}
