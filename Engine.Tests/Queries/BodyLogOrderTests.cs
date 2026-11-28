using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Queries;

// ADR-0192 (phase K25, TASK-0478): the snapshot of /events, CheckInterference with no bodyIds and GetBodies give the live
// bodies in the order of the log. ADR-0192 names this file in enforced-by. The scene of R-0080: a Translate consumes the
// first box, and the bus adds the moved box before it removes the first box, so the place of the first box in the Dictionary
// of Document.Bodies is free after the command. A third box takes that place, and the order of the Dictionary puts it before
// the two bodies that commands before it made.
public sealed class BodyLogOrderTests
{
    private static async Task<Guid> Body(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException($"No body from {command.Name}.");
    }

    // The two boxes, the move of the first and a third box, and the identifiers in the order of the log.
    private static async Task<(DocumentSession Session, Guid Second, Guid Moved, Guid Third)> Scene(ManifoldGeometryBackend backend)
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var first = await Body(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 });
        var second = await Body(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 });
        var moved = await Body(session, new TranslateCommand { BodyId = first, Dx = 5, Dy = 0, Dz = 0 });
        var third = await Body(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });
        return (session, second, moved, third);
    }

    // The Dictionary gives the third box first, so the scene shows the defect; the order of the log gives it last.
    [NativeManifoldFact]
    public async Task A_Body_In_The_Free_Place_Of_A_Consumed_One_Comes_In_The_Order_Of_The_Log()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, second, moved, third) = await Scene(backend);

        Assert.Equal([third, second, moved], await session.Read((document, _) => document.Bodies.Select(body => body.Handle.Id).ToList()));
        Guid[] log = [second, moved, third];
        Assert.Equal(log, await session.Read((document, _) => LogOrder.Bodies(document).Select(body => body.Handle.Id).ToList()));
        var snapshot = await session.Read((document, _) => SnapshotProjector.Project(document, seq: 0));
        Assert.Equal(log, snapshot.Bodies.Select(body => body.Handle).ToList());
        var pairs = (await session.Query<InterferenceInfo>(new CheckInterferenceQuery())).Result!.Pairs;
        Assert.Equal([(second, moved), (second, third), (moved, third)], pairs.Select(pair => (pair.BodyIdA, pair.BodyIdB)).ToList());
        var bodies = (await session.Query<BodiesInfo>(new GetBodiesQuery())).Result!.Bodies;
        Assert.Equal(log, bodies.Select(body => body.BodyId).ToList());
    }

    // ADR-0189 item 3: an Undo of the Translate gives the first box back at its place, before the second box.
    [NativeManifoldFact]
    public async Task An_Undo_Gives_A_Consumed_Body_Back_At_Its_Place_In_The_Log()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, second, _, _) = await Scene(backend);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(new UndoCommand())).Status);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(new UndoCommand())).Status);

        var order = await session.Read((document, _) => LogOrder.Bodies(document).Select(body => body.Handle.Id).ToList());
        Assert.Equal(2, order.Count);
        Assert.Equal(second, order[1]);
        var snapshot = await session.Read((document, _) => SnapshotProjector.Project(document, seq: 0));
        Assert.Equal(order, snapshot.Bodies.Select(body => body.Handle).ToList());
    }
}
