using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Hosting;

namespace Engine.Tests.Hosting;

// Per TASK-0013: shared engine-wiring factory eliminates the duplicate
// bring-up sequences in Engine.Cli and Engine.Api.Http. These tests cover
// the factory's contract: independent kits per call, every V1.x default
// handler registered, backend with the expected capability set.
public class EngineHostingTests
{
    [Fact]
    public void CreateDefault_Returns_Fresh_Document_Each_Call()
    {
        var a = EngineHosting.CreateDefault();
        var b = EngineHosting.CreateDefault();
        Assert.NotEqual(a.Document.DocumentId, b.Document.DocumentId);
    }

    [Fact]
    public void CreateDefault_Returns_Independent_Registries_Each_Call()
    {
        var a = EngineHosting.CreateDefault();
        var b = EngineHosting.CreateDefault();
        Assert.NotSame(a.CommandRegistry, b.CommandRegistry);
        Assert.NotSame(a.QueryRegistry, b.QueryRegistry);
    }

    [Fact]
    public void CreateDefault_Registers_NoOp_Command_Handler()
    {
        var kit = EngineHosting.CreateDefault();
        Assert.True(kit.CommandRegistry.TryFind("NoOp", 1, out var handler));
        Assert.NotNull(handler);
    }

    [Fact]
    public void CreateDefault_Registers_CreateBox_Command_Handler()
    {
        var kit = EngineHosting.CreateDefault();
        Assert.True(kit.CommandRegistry.TryFind("CreateBox", 1, out var handler));
        Assert.NotNull(handler);
    }

    [Fact]
    public void CreateDefault_Registers_GetBoundingBox_Query_Handler()
    {
        var kit = EngineHosting.CreateDefault();
        Assert.True(kit.QueryRegistry.TryFind("GetBoundingBox", 1, out var handler));
        Assert.NotNull(handler);
    }

    [Fact]
    public void CreateDefault_Backend_Has_Mesh_And_Query_Capabilities()
    {
        var kit = EngineHosting.CreateDefault();
        Assert.Equal(
            BackendCapabilities.Mesh | BackendCapabilities.Query,
            kit.Backend.Capabilities);
    }

    [Fact]
    public void CreateDefault_Backend_Exposes_Mesh_And_Query_Via_TryGet()
    {
        var kit = EngineHosting.CreateDefault();
        Assert.NotNull(kit.Backend.TryGet<IMeshOps>());
        Assert.NotNull(kit.Backend.TryGet<IGeometryQuery>());
    }

    [Fact]
    public void CreateDefault_Events_Sink_Is_Fresh_InMemory_Sink()
    {
        var kit = EngineHosting.CreateDefault();
        Assert.NotNull(kit.Events);
        // A brand-new sink has no events and the next read returns from Seq=1.
        Assert.Empty(kit.Events.Snapshot());
    }

    [Fact]
    public void RegisterDefaultCommands_Adds_NoOp_And_CreateBox_To_Empty_Registry()
    {
        var registry = new CommandRegistry();
        EngineHosting.RegisterDefaultCommands(registry);
        Assert.True(registry.TryFind("NoOp", 1, out _));
        Assert.True(registry.TryFind("CreateBox", 1, out _));
    }

    [Fact]
    public void RegisterDefaultQueries_Adds_GetBoundingBox_To_Empty_Registry()
    {
        var registry = new QueryRegistry();
        EngineHosting.RegisterDefaultQueries(registry);
        Assert.True(registry.TryFind("GetBoundingBox", 1, out _));
    }

    [Fact]
    public void RegisterDefaultCommands_Throws_On_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => EngineHosting.RegisterDefaultCommands(null!));
    }

    [Fact]
    public void RegisterDefaultQueries_Throws_On_Null()
    {
        Assert.Throws<ArgumentNullException>(
            () => EngineHosting.RegisterDefaultQueries(null!));
    }
}
