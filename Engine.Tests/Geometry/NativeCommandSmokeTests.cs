using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;

namespace Engine.Tests.Geometry;

// TASK-0036, scope item 5. Translate and Subtract on the native backend through
// the command bus of a session, which is the path of each host. The managed
// backend refuses both, so before TASK-0036 a host on a computer with no native
// library started and then refused this demonstration.
public class NativeCommandSmokeTests
{
    [NativeManifoldFact]
    public async Task Translate_And_Subtract_Apply_On_The_Native_Backend()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));

        var big = new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 };
        var small = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        Assert.Equal(CommandStatus.Applied, (await session.Apply(big)).Status);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(small)).Status);

        var moved = new TranslateCommand { BodyId = small.CommandId, Dx = 0.5, Dy = 0, Dz = 0 };
        var translate = await session.Apply(moved);
        Assert.Equal(CommandStatus.Applied, translate.Status);

        // The moved box spans 0 to 1 on x: the cube of size 1 is centred on the
        // origin, and Translate moves it by 0.5. The query comes before the
        // subtract, because the subtract consumes the moved box (ADR-0021).
        var box = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = moved.CommandId });
        Assert.Null(box.Error);
        Assert.Equal(0.0, box.Result.MinX, 9);
        Assert.Equal(1.0, box.Result.MaxX, 9);

        var cut = new SubtractCommand { MinuendBodyId = big.CommandId, SubtrahendBodyId = moved.CommandId };
        var subtract = await session.Apply(cut);
        Assert.Equal(CommandStatus.Applied, subtract.Status);

        var consumed = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = moved.CommandId });
        Assert.Equal("E-GEOM-BODY-NOT-FOUND", consumed.Error?.Code);

        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = cut.CommandId });
        Assert.Null(result.Error);
        Assert.Equal(-1.0, result.Result.MinX, 9);
        Assert.Equal(1.0, result.Result.MaxX, 9);
    }
}
