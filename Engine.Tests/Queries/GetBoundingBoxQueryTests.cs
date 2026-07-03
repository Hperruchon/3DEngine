using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Queries;

namespace Engine.Tests.Queries;

public class GetBoundingBoxQueryTests
{
    private static async Task<(Document doc, QueryBus qb, Guid bodyId)> SetupWithOneBox()
    {
        var doc = new Document();
        var commandRegistry = new CommandRegistry();
        commandRegistry.Register(new CreateBoxCommandHandler());
        var queryRegistry = new QueryRegistry();
        queryRegistry.Register(new GetBoundingBoxQueryHandler());
        var sink = new InMemoryEventSink();
        var backend = new InProcessMeshBackend();
        var commandBus = new CommandBus(doc, commandRegistry, sink, backend);
        var queryBus = new QueryBus(doc, queryRegistry, backend);

        var createId = Guid.NewGuid();
        await commandBus.Apply(new CreateBoxCommand
        {
            CommandId = createId,
            SizeX = 10,
            SizeY = 20,
            SizeZ = 40,
        });

        return (doc, queryBus, createId);
    }

    [Fact]
    public async Task GetBoundingBox_Returns_Stored_Aabb_For_Existing_Body()
    {
        var (_, queryBus, bodyId) = await SetupWithOneBox();
        var result = await queryBus.Query<Aabb>(new GetBoundingBoxQuery { BodyId = bodyId });

        Assert.Null(result.Error);
        Assert.Equal(-5.0,  result.Result.MinX);
        Assert.Equal(-10.0, result.Result.MinY);
        Assert.Equal(-20.0, result.Result.MinZ);
        Assert.Equal(5.0,   result.Result.MaxX);
        Assert.Equal(10.0,  result.Result.MaxY);
        Assert.Equal(20.0,  result.Result.MaxZ);
    }

    [Fact]
    public async Task GetBoundingBox_For_Unknown_Body_Returns_E_GEOM_BODY_NOT_FOUND()
    {
        var (_, queryBus, _) = await SetupWithOneBox();
        var result = await queryBus.Query<Aabb>(new GetBoundingBoxQuery { BodyId = Guid.NewGuid() });

        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task GetBoundingBox_When_Backend_Lacks_Geometry_Query_Returns_E_GEOM_CAP_MISSING()
    {
        var doc = new Document();
        var queryRegistry = new QueryRegistry();
        queryRegistry.Register(new GetBoundingBoxQueryHandler());
        // Use NullGeometryBackend so TryGet<IGeometryQuery>() returns null.
        var queryBus = new QueryBus(doc, queryRegistry, NullGeometryBackend.Instance);

        var result = await queryBus.Query<Aabb>(new GetBoundingBoxQuery { BodyId = Guid.NewGuid() });
        Assert.NotNull(result.Error);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, result.Error!.Code);
    }
}
