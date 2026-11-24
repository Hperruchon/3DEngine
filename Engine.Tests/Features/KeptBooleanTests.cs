using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Preview;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Features;

// TASK-0454 (phase P0.184, ADR-0059 item 2): the projection keeps the result of each boolean of the history of a body, so the
// command and each reader make one exact boolean for each change of the Document, and not one for each reader. The count of
// the booleans of the kernel on this thread (PlanarBoolean.BooleansOnThisThread) holds the work, and no time decides a test
// (ADR-0173 item 4). The command and the queries complete on the thread of the test (TASK-0452).
// TASK-0457 (phase P0.186): two tests fill the kept results past their limit and each test counts the booleans or the kept
// results, so the class runs in the collection "kept booleans", alone (KeptBooleanCollection.cs).
[Collection("kept booleans")]
public sealed class KeptBooleanTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("kept-boolean-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static async Task<Guid> Body(DocumentSession session, Command command) => Output(await Applied(session, command), "bodyId");

    private static async Task<FacesInfo> Faces(DocumentSession session, Guid body)
    {
        var result = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static Task<KernelSolid?> Solid(DocumentSession session, Guid body)
        => session.Read((document, _) => KernelProjection.Of(document, body).Solid);

    private static Task<KernelSolid?> KeptSolid(DocumentSession session, Guid body)
        => session.Read((document, _) => KernelProjection.OfRecord(document, document.Bodies.Single(record => record.Handle.Id == body)).Solid);

    // A plate of 10 by 6 on the plane XY, extruded by the distance, with a datum on its side x = 10, and a box of 4 by 20 by 20
    // at x = 10 that cuts the part of the plate from x = 8 to x = 10. The part that stays is 8 by 6 by the distance, and the
    // datum gets the fault of a gone face (ADR-0059 item 3).
    private static async Task<(Guid Plate, Guid Cut, Guid Step, Guid Datum)> CutPlate(DocumentSession session, double distance)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        var lines = new Guid[4];
        for (var i = 0; i < 4; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }), "lineId");
        var plate = await Body(session, new ExtrudeCommand { SketchId = sketch, Distance = distance });
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = plate, Element = "side", ItemId = lines[1] }), "datumId");
        var tool = await Body(session, new TranslateCommand { BodyId = await Body(session, new CreateBoxCommand { SizeX = 4, SizeY = 20, SizeZ = 20 }), Dx = 10, Dy = 0, Dz = 0 });
        var step = Guid.NewGuid();
        var cut = await Body(session, new SubtractCommand { CommandId = step, MinuendBodyId = plate, SubtrahendBodyId = tool });
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await session.Read((document, _) => document.Datums.Single(record => record.Id == datum))).Fault?.Code);
        return (plate, cut, step, datum);
    }

    // The command Subtract makes the boolean for the faults of its datums, and each later reader of the result takes the kept
    // result: GetFaces, the solid of the projection, the kept solid of the key, and the faces of the B-Rep again. Union and
    // Intersect make one boolean each in the same way. Until TASK-0454 the query of the faces made the boolean again.
    [NativeManifoldFact]
    public async Task One_Boolean_Command_And_Its_Readers_Make_One_Boolean()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Body(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 });
        var tool = await Body(session, new CreateCylinderCommand { Radius = 3, Height = 10 });

        var start = PlanarBoolean.BooleansOnThisThread;
        var cut = await Body(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool });
        Assert.Equal(1, PlanarBoolean.BooleansOnThisThread - start);
        var faces = await Faces(session, cut);
        var solid = await Solid(session, cut);
        var kept = await KeptSolid(session, cut);
        Assert.Equal(1, PlanarBoolean.BooleansOnThisThread - start);
        Assert.Contains(faces.Faces, face => face.Reference == new DatumReference(tool, "side", null));
        Assert.Same(solid, kept);

        foreach (Func<Guid, Guid, Command> make in new Func<Guid, Guid, Command>[]
        {
            (first, second) => new UnionCommand { FirstBodyId = first, SecondBodyId = second },
            (first, second) => new IntersectCommand { FirstBodyId = first, SecondBodyId = second },
        })
        {
            var a = await Body(session, new CreateBoxCommand { SizeX = 6, SizeY = 6, SizeZ = 6 });
            var b = await Body(session, new TranslateCommand { BodyId = await Body(session, new CreateBoxCommand { SizeX = 6, SizeY = 6, SizeZ = 6 }), Dx = 3, Dy = 2, Dz = 1 });
            start = PlanarBoolean.BooleansOnThisThread;
            var joined = await Body(session, make(a, b));
            await Faces(session, joined);
            await Solid(session, joined);
            Assert.Equal(1, PlanarBoolean.BooleansOnThisThread - start);
        }
    }

    // A change of an operand changes the solid of the step, so the kept result of the old operands does not answer: the edit
    // of the distance of the plate makes the boolean again, and the solid of the result has the volume of the new plate. An
    // undo gives the volume before.
    [NativeManifoldFact]
    public async Task A_Change_Of_An_Operand_Makes_The_Boolean_Again()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (plate, cut, _, _) = await CutPlate(session, 2);
        await Faces(session, cut);
        Assert.Equal((Rational)96, (await Solid(session, cut))!.Volume());

        var start = PlanarBoolean.BooleansOnThisThread;
        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = plate, Distance = 3 });
        await Faces(session, cut);
        Assert.Equal((Rational)144, (await Solid(session, cut))!.Volume());
        Assert.Equal(1, PlanarBoolean.BooleansOnThisThread - start);

        await Applied(session, new UndoCommand());
        Assert.Equal((Rational)96, (await Solid(session, cut))!.Volume());
    }

    // A step that is no longer in effect leaves the kept results at the next walk of the Document: an undo of the cut and a
    // read of the plate remove the result of the step.
    [NativeManifoldFact]
    public async Task A_Step_That_Is_No_Longer_In_Effect_Leaves_The_Kept_Results()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (plate, _, step, _) = await CutPlate(session, 2);
        var document = session.DocumentId;
        Assert.True(KernelProjection.IsBooleanKept(document, step));

        await Applied(session, new UndoCommand());
        await Solid(session, plate);
        Assert.False(KernelProjection.IsBooleanKept(document, step));
    }

    private static readonly KernelSolid First = KernelSolid.Box(Guid.NewGuid(), 4, 4, 4);
    private static readonly KernelSolid Second = KernelSolid.Box(Guid.NewGuid(), 2, 2, 8);

    private static KernelSolid Copy(KernelSolid solid) => KernelSolid.Box(solid.Polygons[0].Tag.Feature, 4, 4, 4);

    // A kept result answers a step only for the same kind and the same operands, value for value: a copy of an operand takes
    // the kept result, another operand or another kind of step makes the boolean again.
    [Fact]
    public void A_Kept_Result_Needs_The_Same_Kind_And_The_Same_Operands()
    {
        var (document, step) = (Guid.NewGuid(), Guid.NewGuid());
        var subtract = new SubtractCommand { CommandId = step, MinuendBodyId = Guid.NewGuid(), SubtrahendBodyId = Guid.NewGuid() };

        var start = PlanarBoolean.BooleansOnThisThread;
        var result = KernelProjection.Boolean(document, subtract, First, Second);
        Assert.Equal((Rational)(64 - 16), result!.Volume());
        Assert.Same(result, KernelProjection.Boolean(document, subtract, Copy(First), Second));
        Assert.Equal(1, PlanarBoolean.BooleansOnThisThread - start);

        var larger = KernelSolid.Box(First.Polygons[0].Tag.Feature, 4, 4, 6);
        Assert.Equal((Rational)(96 - 24), KernelProjection.Boolean(document, subtract, larger, Second)!.Volume());
        Assert.Equal(2, PlanarBoolean.BooleansOnThisThread - start);

        var union = new UnionCommand { CommandId = step, FirstBodyId = Guid.NewGuid(), SecondBodyId = Guid.NewGuid() };
        Assert.Equal((Rational)(96 + 8), KernelProjection.Boolean(document, union, larger, Second)!.Volume());
        Assert.Equal(3, PlanarBoolean.BooleansOnThisThread - start);

        // The same step of another Document makes its own boolean.
        Assert.Equal((Rational)(96 + 8), KernelProjection.Boolean(Guid.NewGuid(), union, larger, Second)!.Volume());
        Assert.Equal(4, PlanarBoolean.BooleansOnThisThread - start);
    }

    // The kept results stay at BooleanLimit: the step of the oldest use leaves first.
    [Fact]
    public void The_Kept_Results_Stay_At_Their_Limit()
    {
        var document = Guid.NewGuid();
        var steps = Enumerable.Range(0, KernelProjection.BooleanLimit + 8).Select(_ => Guid.NewGuid()).ToList();
        foreach (var step in steps)
            KernelProjection.Boolean(document, new SubtractCommand { CommandId = step, MinuendBodyId = Guid.NewGuid(), SubtrahendBodyId = Guid.NewGuid() }, First, Second);

        Assert.True(KernelProjection.BooleansKept <= KernelProjection.BooleanLimit, $"{KernelProjection.BooleansKept} results are kept.");
        Assert.False(KernelProjection.IsBooleanKept(document, steps[0]));
        Assert.True(KernelProjection.IsBooleanKept(document, steps[^1]));
    }

    // Many threads read and fill the kept results at one time, also past the limit; each result is the exact result.
    [Fact]
    public void Many_Threads_Share_The_Kept_Results()
    {
        var documents = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToList();
        var steps = Enumerable.Range(0, KernelProjection.BooleanLimit / 2).Select(_ => Guid.NewGuid()).ToList();
        var volumes = new System.Collections.Concurrent.ConcurrentBag<Rational>();
        const int Calls = 16_384;
        Parallel.For(0, Calls, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            var step = new SubtractCommand { CommandId = steps[i % steps.Count], MinuendBodyId = Guid.NewGuid(), SubtrahendBodyId = Guid.NewGuid() };
            volumes.Add(KernelProjection.Boolean(documents[i / steps.Count % documents.Count], step, First, Second)!.Volume());
        });

        Assert.Equal(Calls, volumes.Count);
        Assert.All(volumes, volume => Assert.Equal((Rational)(64 - 16), volume));
        Assert.True(KernelProjection.BooleansKept <= KernelProjection.BooleanLimit, $"{KernelProjection.BooleansKept} results are kept.");
    }

    // TASK-0457 (phase P0.186): the failure of run 45021528072, made on purpose. Another thread adds BooleanLimit new results
    // between the command Subtract and the query of the faces, as a test of this class that runs at the same time can do. The
    // kept result of the cut leaves, so the query makes the boolean again: two booleans on the thread of the test, and not
    // one. The new result has the same exact content as the result that left. So the product is correct, and only a test that
    // counts the booleans or the kept results depends on the tests that run beside it (KeptBooleanCollection.cs).
    [NativeManifoldFact]
    public async Task A_Result_That_Other_Steps_Push_Out_Is_Made_Again_With_The_Same_Content()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Body(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 });
        var tool = await Body(session, new CreateCylinderCommand { Radius = 3, Height = 10 });
        var step = Guid.NewGuid();

        var start = PlanarBoolean.BooleansOnThisThread;
        var cut = await Body(session, new SubtractCommand { CommandId = step, MinuendBodyId = plate, SubtrahendBodyId = tool });
        var before = await Solid(session, cut);
        Assert.Equal(1, PlanarBoolean.BooleansOnThisThread - start);

        var other = new Thread(() =>
        {
            var document = Guid.NewGuid();
            for (var i = 0; i < KernelProjection.BooleanLimit; i++)
                KernelProjection.Boolean(document, new SubtractCommand { CommandId = Guid.NewGuid(), MinuendBodyId = Guid.NewGuid(), SubtrahendBodyId = Guid.NewGuid() }, First, Second);
        });
        other.Start();
        other.Join();
        Assert.False(KernelProjection.IsBooleanKept(session.DocumentId, step));

        var faces = await Faces(session, cut);
        Assert.Equal(2, PlanarBoolean.BooleansOnThisThread - start);
        Assert.Contains(faces.Faces, face => face.Reference == new DatumReference(tool, "side", null));
        var after = await Solid(session, cut);
        Assert.Equal(2, PlanarBoolean.BooleansOnThisThread - start);
        Assert.NotSame(before, after);
        Assert.Equal(Text(before!), Text(after!));
    }

    private static string Text(KernelSolid solid)
        => string.Join("\n", solid.Polygons.Select(polygon => $"{polygon.Tag} {polygon.Plane} {string.Join(" ", polygon.Vertices)}")) + $"\n{solid.Volume()}";

    // TASK-0461 (finding E239 of the codebase review of 2026-11-23, ADR-0185 item 11): a fork and a base of a preview replay the
    // log in a new Document, and take the kept results of the first Document. So a preview makes no boolean of the log again,
    // and the store of the first Document does not change. A fork keeps its own results in its own store, which leaves with the
    // fork. Until TASK-0461 the rename made one boolean (the replay), the move made two (the replay and the base), and each fork
    // added its results to the store of the first Document. The fork runs on the thread of the test, so the count of the
    // booleans of this thread is the count of the preview.
    [NativeManifoldFact]
    public async Task A_Fork_Takes_The_Kept_Results_Of_The_First_Document_And_Adds_None_To_Them()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Body(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 });
        var tool = await Body(session, new TranslateCommand { BodyId = await Body(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 10 }), Dx = 4, Dy = 4, Dz = -2 });
        var step = Guid.NewGuid();
        var cut = await Body(session, new SubtractCommand { CommandId = step, MinuendBodyId = plate, SubtrahendBodyId = tool });
        await Faces(session, cut);
        var post = await Body(session, new TranslateCommand { BodyId = await Body(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 4 }), Dx = 100, Dy = 0, Dz = 0 });
        var source = await session.Read((document, _) => ForkSource.Of(document));
        var kept = KernelProjection.BooleansKept;
        Func<IGeometryBackend> backends = () => new ManifoldGeometryBackend();

        var start = PlanarBoolean.BooleansOnThisThread;
        var renamed = await DocumentFork.Run(source, [new RenameBodyCommand { BodyId = cut, NewName = "Plate" }], backends);
        var rename = PlanarBoolean.BooleansOnThisThread - start;
        Assert.NotNull(Assert.Single(renamed.Measures).After.Mass);

        start = PlanarBoolean.BooleansOnThisThread;
        var moved = await DocumentFork.Run(source, [new TranslateCommand { BodyId = post, Dx = 50, Dy = 0, Dz = 0 }], backends);
        var move = PlanarBoolean.BooleansOnThisThread - start;
        Assert.NotNull(Assert.Single(moved.Measures).After.Mass);

        Assert.True(rename == 0 && move == 0, $"The rename made {rename} booleans and the move {move}; each boolean of the log is kept.");
        Assert.Equal(kept, KernelProjection.BooleansKept);
        Assert.True(KernelProjection.IsBooleanKept(session.DocumentId, step));

        // A new boolean of the proposal is the own result of the fork: it leaves with the fork and pushes out nothing.
        var box = new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 20 };
        var hole = await DocumentFork.Run(source, [box, new SubtractCommand { MinuendBodyId = cut, SubtrahendBodyId = box.CommandId }], backends);
        Assert.Null(hole.FailedIndex);
        Assert.Equal(kept, KernelProjection.BooleansKept);
        Assert.Equal(0, KernelProjection.ForksOpen);
    }

    // TASK-0461 (finding E239): the probe of the review on a host. Eight proposals with a move and sixteen commits run forks and
    // bases on the worker of the queue; the kept result of the cut of the first Document stays, and the next read of its faces
    // makes no boolean.
    [NativeManifoldFact]
    public async Task The_Forks_Of_Eight_Proposals_And_Sixteen_Commits_Push_Out_No_Kept_Result_Of_The_First_Document()
    {
        using var host = new Engine.Api.Http.EngineHost(new ManifoldGeometryBackend(), forks: () => new ManifoldGeometryBackend());
        var session = host.Session;
        var plate = await Body(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 });
        var tool = await Body(session, new TranslateCommand { BodyId = await Body(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 10 }), Dx = 4, Dy = 4, Dz = -2 });
        var step = Guid.NewGuid();
        var cut = await Body(session, new SubtractCommand { CommandId = step, MinuendBodyId = plate, SubtrahendBodyId = tool });
        await Faces(session, cut);
        var kept = KernelProjection.BooleansKept;

        var proposals = new List<Guid>();
        for (var i = 0; i < Engine.Api.Http.Proposals.MostOpen; i++)
        {
            var command = new TranslateCommand { BodyId = cut, Dx = i + 1, Dy = 0, Dz = 0 };
            var text = new Engine.Api.Http.ProposedCommand("Translate", 1, new Dictionary<string, System.Text.Json.JsonElement>(), command.CommandId, null);
            var summary = $"Move {i}";
            var added = await session.Read((document, _) => host.Proposals.Add(summary, [(text, command)], document));
            proposals.Add(added!.Id);
        }

        for (var i = 0; i < 16; i++)
            await Applied(session, new RenameBodyCommand { BodyId = cut, NewName = $"Plate {i}" });
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        foreach (var id in proposals)
            Assert.Equal(Engine.Api.Http.PreviewStates.Ready, (await host.Proposals.Preview(id, limit.Token))?.State);

        Assert.Equal(kept, KernelProjection.BooleansKept);
        Assert.True(KernelProjection.IsBooleanKept(session.DocumentId, step), "A fork pushed out the kept result of the cut.");
        var start = PlanarBoolean.BooleansOnThisThread;
        await Faces(session, cut);
        Assert.Equal(0, PlanarBoolean.BooleansOnThisThread - start);
    }

    // The kept results hold no state that the log does not give: the session makes the cut with a datum on a face that it
    // removes, edits the plate two times, reads the faces, undoes and redoes, and a replay of its log in a new session gives
    // the same records, faults and exact solids.
    [NativeManifoldFact]
    public async Task A_Replay_Gives_The_Same_Records_And_Solids_As_The_Session_That_Kept_The_Results()
    {
        var path = Path.Combine(_folder, "cut" + DocumentFile.Extension);
        List<BodyRecord> bodies;
        List<DatumRecord> datums;
        List<string> solids;
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = NewSession(backend);
            DocumentFile.Create(session, path);
            var (plate, cut, _, datum) = await CutPlate(session, 2);
            var second = await Body(session, new SubtractCommand { MinuendBodyId = cut, SubtrahendBodyId = await Body(session, new CreateCylinderCommand { Radius = 1, Height = 20 }) });
            await Faces(session, second);
            await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = plate, Distance = 3 });
            await Faces(session, second);
            await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = plate, Distance = 4 });
            await Applied(session, new UndoCommand());
            await Applied(session, new RedoCommand());
            await Faces(session, second);
            (bodies, datums, solids) = await State(session);
            session.Close();
            Assert.Equal(DiagnosticCodes.DatumReferenceFailed, datums.Single(record => record.Id == datum).Fault?.Code);
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var replay = NewSession(backend);
            Assert.Empty(await DocumentFile.Open(replay, path));
            var (replayedBodies, replayedDatums, replayedSolids) = await State(replay);
            replay.Close();
            Assert.Equal(bodies, replayedBodies);
            Assert.Equal(datums, replayedDatums);
            Assert.Equal(solids, replayedSolids);
        }
    }

    private static async Task<(List<BodyRecord> Bodies, List<DatumRecord> Datums, List<string> Solids)> State(DocumentSession session)
    {
        var bodies = await session.Read((document, _) => document.Bodies.OrderBy(body => body.Handle.Id).ToList());
        var datums = await session.Read((document, _) => document.Datums.OrderBy(datum => datum.Id).ToList());
        var solids = new List<string>();
        foreach (var body in bodies)
        {
            var solid = await Solid(session, body.Handle.Id);
            solids.Add(solid is null ? "none" : string.Join("\n", solid.Polygons.Select(polygon => $"{polygon.Tag} {polygon.Plane} {string.Join(" ", polygon.Vertices)}")) + $"\n{solid.Volume()}");
        }

        return (bodies, datums, solids);
    }
}
