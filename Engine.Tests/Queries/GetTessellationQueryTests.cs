using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests.Queries;

// The query GetTessellation of ADR-0018 (TASK-0028), on the engine with no host.
// ADR-0018 names this file in enforced-by. The native triangles are in
// Engine.Tests/Geometry/ManifoldTessellationTests.cs; this file holds the rules
// of the query: the errors, the schema, and no change to the Document.
public class GetTessellationQueryTests
{
    [Fact]
    public async Task The_Managed_Backend_Gives_E_GEOM_CAP_MISSING()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        Assert.Equal(CommandStatus.Applied, (await session.Apply(box)).Status);

        var result = await session.Query<Tessellation>(new GetTessellationQuery { BodyId = box.CommandId });

        Assert.Equal(DiagnosticCodes.GeomCapMissing, result.Error?.Code);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task An_Unknown_Body_Gives_E_GEOM_BODY_NOT_FOUND()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new OneTriangleBackend()));

        var result = await session.Query<Tessellation>(new GetTessellationQuery { BodyId = Guid.NewGuid() });

        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, result.Error?.Code);
    }

    [Fact]
    public async Task The_Query_Gives_The_Triangles_And_Adds_No_Event_And_No_Version()
    {
        var kit = EngineHosting.CreateDefault(new OneTriangleBackend());
        var session = new DocumentSession(kit);
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        Assert.Equal(CommandStatus.Applied, (await session.Apply(box)).Status);
        var before = (Version: await session.Read((document, _) => document.Version), Events: kit.Events.Count);

        var result = await session.Query<Tessellation>(new GetTessellationQuery { BodyId = box.CommandId });

        Assert.Null(result.Error);
        Assert.Equal(1, result.Result!.TriangleCount);
        Assert.Equal(3, result.Result.VertexCount);
        Assert.Equal(before.Version, await session.Read((document, _) => document.Version));
        Assert.Equal(before.Events, kit.Events.Count);
    }

    [Fact]
    public void The_Schema_Gives_The_Parameter_And_The_Four_Result_Fields()
    {
        var handler = new GetTessellationQueryHandler();

        Assert.Equal("guid", handler.Parameters["bodyId"].Type);
        Assert.True(handler.Parameters["bodyId"].Required);
        Assert.Equal(
            ["indices:array", "positions:array", "triangleCount:integer", "vertexCount:integer"],
            handler.Result.Select(field => $"{field.Key}:{field.Value.Type}").Order(StringComparer.Ordinal));
    }

    [Fact]
    public void The_Catalog_Registers_The_Query()
    {
        Assert.Contains(HandlerCatalog.QueryHandlers(), handler => handler is GetTessellationQueryHandler);
    }

    // A backend that creates a box and gives one fixed triangle for each body
    // that it holds, so that the rules of the query need no native library.
    private sealed class OneTriangleBackend : IGeometryBackend, IMeshOps, ITessellationOps
    {
        private readonly HashSet<Guid> _bodies = [];

        public BackendIdentity Identity { get; } = new("test", "1");

        public BackendCapabilities Capabilities => BackendCapabilities.Mesh | BackendCapabilities.Tessellation;

        public T? TryGet<T>() where T : class => this as T;

        public void CreateBox(BodyHandle handle, BoxParameters parameters) => _bodies.Add(handle.Id);

        public Tessellation Tessellate(BodyHandle handle)
            => _bodies.Contains(handle.Id)
                ? new Tessellation([0, 0, 0, 1, 0, 0, 0, 1, 0], [0, 1, 2])
                : throw new KeyNotFoundException($"The backend holds no body {handle.Id}.");
    }
}
