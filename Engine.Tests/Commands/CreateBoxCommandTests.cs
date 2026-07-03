using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;

namespace Engine.Tests.Commands;

public class CreateBoxCommandTests
{
    private static (Document doc, CommandBus bus, InMemoryEventSink sink, InProcessMeshBackend backend) NewBus()
    {
        var doc = new Document();
        var registry = new CommandRegistry();
        registry.Register(new CreateBoxCommandHandler());
        var sink = new InMemoryEventSink();
        var backend = new InProcessMeshBackend();
        var bus = new CommandBus(doc, registry, sink, backend);
        return (doc, bus, sink, backend);
    }

    [Fact]
    public async Task CreateBox_Succeeds_With_Valid_Params_Emits_Body_Created_Event()
    {
        var (_, bus, sink, _) = NewBus();
        var command = new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 };

        var result = await bus.Apply(command);

        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.Null(result.Error);
        Assert.True(result.Outputs.TryGet<Guid>("bodyId", out var bodyId));
        Assert.Equal(command.CommandId, bodyId);

        var events = sink.Snapshot();
        Assert.Equal(2, events.Count);
        Assert.Equal("command.applied", events[0].Kind);
        Assert.Equal("body.created", events[1].Kind);
        Assert.Equal(command.CommandId, events[1].CauseCommandId);
    }

    [Fact]
    public async Task CreateBox_Adds_Body_To_Document_Bodies()
    {
        var (doc, bus, _, _) = NewBus();
        var command = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await bus.Apply(command);

        Assert.Single(doc.Bodies);
        var body = doc.Bodies.Single();
        Assert.Equal(command.CommandId, body.Handle.Id);
        Assert.Equal("Box", body.Kind);
    }

    [Fact]
    public async Task CreateBox_Backend_Cache_Reflects_Body()
    {
        var (_, bus, _, backend) = NewBus();
        var command = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        await bus.Apply(command);

        Assert.Equal(1, backend.BodyCount);
        Assert.True(backend.Contains(new BodyHandle(command.CommandId)));
    }

    [Theory]
    [InlineData(0.0, 1.0, 1.0)]
    [InlineData(1.0, 0.0, 1.0)]
    [InlineData(1.0, 1.0, 0.0)]
    public async Task CreateBox_With_Zero_Size_Returns_E_GEOM_INVALID_PARAM(double sx, double sy, double sz)
    {
        var (_, bus, _, _) = NewBus();
        var result = await bus.Apply(new CreateBoxCommand { SizeX = sx, SizeY = sy, SizeZ = sz });
        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, result.Error!.Code);
    }

    [Theory]
    [InlineData(-1.0, 1.0, 1.0)]
    [InlineData(1.0, -1.0, 1.0)]
    [InlineData(1.0, 1.0, -1.0)]
    public async Task CreateBox_With_Negative_Size_Returns_E_GEOM_INVALID_PARAM(double sx, double sy, double sz)
    {
        var (_, bus, _, _) = NewBus();
        var result = await bus.Apply(new CreateBoxCommand { SizeX = sx, SizeY = sy, SizeZ = sz });
        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, result.Error!.Code);
    }

    [Fact]
    public async Task CreateBox_When_Backend_Lacks_Mesh_Ops_Returns_E_GEOM_CAP_MISSING()
    {
        var doc = new Document();
        var registry = new CommandRegistry();
        registry.Register(new CreateBoxCommandHandler());
        var sink = new InMemoryEventSink();
        // NullGeometryBackend.Capabilities == None; TryGet<IMeshOps> returns null.
        var bus = new CommandBus(doc, registry, sink, NullGeometryBackend.Instance);

        var result = await bus.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, result.Error!.Code);
        Assert.Empty(doc.Bodies);
    }

    [Fact]
    public async Task Body_Handle_Equals_CommandId()
    {
        var (doc, bus, _, _) = NewBus();
        var id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        await bus.Apply(new CreateBoxCommand { CommandId = id, SizeX = 1, SizeY = 1, SizeZ = 1 });

        var body = doc.Bodies.Single();
        Assert.Equal(id, body.Handle.Id);
    }
}
