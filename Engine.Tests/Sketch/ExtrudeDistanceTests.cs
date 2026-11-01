using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// The edit of the distance of an extrude (ADR-0049, TASK-0163). ADR-0049 names this file in
// enforced-by. A live extrude, a negative distance, an edit that changes nothing, the refusals, an
// undo and a redo, a later edit of the sketch, a chain, a broken profile and its repair, the query,
// and an open.
public sealed class ExtrudeDistanceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-distance-" + Guid.NewGuid().ToString("N"));

    public ExtrudeDistanceTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    private sealed record Sketch(Guid Id, Guid[] Points, Guid[] Lines);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(ManifoldGeometryBackend backend)
    {
        var kit = EngineHosting.CreateDefault(backend);
        return (new DocumentSession(kit), kit.Events);
    }

    // A closed sketch of the corners on XY.
    private static async Task<Sketch> NewSketch(DocumentSession session, (double X, double Y)[] corners)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        var lines = new Guid[corners.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
        return new Sketch(sketch, points, lines);
    }

    private static async Task<Guid> Extrude(DocumentSession session, Sketch sketch, double distance)
        => Output(await Applied(session, new ExtrudeCommand { SketchId = sketch.Id, Distance = distance }), "bodyId");

    private static Task<CommandResult> SetDistance(DocumentSession session, Guid extrude, double distance)
        => Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = extrude, Distance = distance });

    private static async Task<(double, double, double, double, double, double)> Bounds(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        var box = result.Result;
        return (Math.Round(box.MinX, 9), Math.Round(box.MinY, 9), Math.Round(box.MinZ, 9), Math.Round(box.MaxX, 9), Math.Round(box.MaxY, 9), Math.Round(box.MaxZ, 9));
    }

    private static Task<BodyRecord> Record(DocumentSession session, Guid body)
        => session.Read((document, _) => document.Bodies.Single(record => record.Handle.Id == body));

    // Items 1 and 4: the body keeps its handle, gets the solid of the new distance under a key of
    // the edit, and one body.changed; a negative distance extrudes below the datum.
    [NativeManifoldFact]
    public async Task A_Live_Extrude_Takes_The_New_Distance_Under_Its_Handle()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var body = await Extrude(session, await NewSketch(session, Rectangle), 5);

        var edit = await SetDistance(session, body, 12);

        Assert.Empty(edit.Diagnostics);
        Assert.Equal(body, Output(edit, "extrudeId"));
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 12.0), await Bounds(session, body));
        var record = await Record(session, body);
        Assert.Equal((GeometryKeys.Derive(edit.CommandId, body), (BodyFault?)null, "Extrude"), (record.Geometry, record.Fault, record.Kind));
        Assert.Single(events.Snapshot(), e => e.Kind == "body.changed" && e.CauseCommandId == edit.CommandId);

        await SetDistance(session, body, -7);
        Assert.Equal((1.0, 2.0, -7.0, 11.0, 8.0, 0.0), await Bounds(session, body));
    }

    // Item 4: the same distance gives the same parameters, so nothing changes and no event comes.
    [NativeManifoldFact]
    public async Task The_Same_Distance_Changes_Nothing()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var body = await Extrude(session, await NewSketch(session, Rectangle), 5);
        var before = await Record(session, body);

        var edit = await SetDistance(session, body, 5);

        Assert.Equal(before, await Record(session, body));
        Assert.DoesNotContain(events.Snapshot(), e => e.CauseCommandId == edit.CommandId && e.Kind.StartsWith("body.", StringComparison.Ordinal));
    }

    // Item 2: a distance that is not finite or is zero, an identifier of no Extrude in effect (none,
    // a box, an undone extrude), and an identifier that the log holds are refused.
    [NativeManifoldFact]
    public async Task Each_Refusal_Gives_Its_Code()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var body = await Extrude(session, sketch, 5);
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }), "bodyId");

        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 0 }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = double.NaN }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = double.PositiveInfinity }));
        Assert.Equal(DiagnosticCodes.FeatureNotFound, await Refused(session, new SetExtrudeDistanceCommand { ExtrudeId = Guid.NewGuid(), Distance = 3 }));
        Assert.Equal(DiagnosticCodes.FeatureNotFound, await Refused(session, new SetExtrudeDistanceCommand { ExtrudeId = box, Distance = 3 }));

        var undone = await Extrude(session, sketch, 2);
        await Applied(session, new UndoCommand());
        Assert.Equal(DiagnosticCodes.FeatureNotFound, await Refused(session, new SetExtrudeDistanceCommand { ExtrudeId = undone, Distance = 3 }));
    }

    // Item 2: a command sent again after the idempotency cache forgot it is refused, as E95 gives
    // the commands of a sketch, and the Document does not change.
    [NativeManifoldFact]
    public async Task A_Command_Sent_Again_After_The_Cache_Forgot_It_Is_Refused()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var body = await Extrude(session, await NewSketch(session, Rectangle), 5);
        var set = new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 8 };
        await Applied(session, set);
        var before = await session.Read((document, _) => document.Version);
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand());

        Assert.Equal(DiagnosticCodes.FeatureIdInUse, await Refused(session, set with { Distance = 9 }));
        Assert.Equal(before, await session.Read((document, _) => document.Version));
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 8.0), await Bounds(session, body));
    }

    // Items 3 and 6: an undo gives back the old key and solid, a redo the new ones, and an edit of
    // the sketch reads the distance in effect: the new one, and after an undo of it the old one.
    [NativeManifoldFact]
    public async Task An_Undo_And_An_Edit_Of_The_Sketch_Read_The_Distance_In_Effect()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var body = await Extrude(session, sketch, 5);
        var first = await Record(session, body);
        var edit = await SetDistance(session, body, 12);
        var second = await Record(session, body);

        await Applied(session, new UndoCommand());
        Assert.Equal(first, await Record(session, body));
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 5.0), await Bounds(session, body));
        await Applied(session, new RedoCommand());
        Assert.Equal(second, await Record(session, body));

        var move = await Applied(session, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = 15, Y = 12 });
        Assert.Empty(move.Diagnostics);
        Assert.Equal((1.0, 2.0, 0.0, 15.0, 12.0, 12.0), await Bounds(session, body));

        await Applied(session, new UndoCommand());
        await Applied(session, new UndoCommand());
        await Applied(session, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = 13, Y = 10 });
        Assert.Equal((1.0, 2.0, 0.0, 13.0, 10.0, 5.0), await Bounds(session, body));
        Assert.NotEqual(GeometryKeys.Derive(edit.CommandId, body), (await Record(session, body)).Geometry);
    }

    // Item 4: after a Translate and a Subtract consume the extrude, the edit rebuilds the live end
    // of the chain under a key of the edit.
    [NativeManifoldFact]
    public async Task The_End_Of_A_Chain_Follows_The_New_Distance()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var body = await Extrude(session, await NewSketch(session, Rectangle), 5);
        var up = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = 10 }), "bodyId");
        var cutter = Output(await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 50 }), "bodyId");
        var end = Output(await Applied(session, new SubtractCommand { MinuendBodyId = up, SubtrahendBodyId = cutter }), "bodyId");

        var edit = await SetDistance(session, body, 20);

        Assert.Empty(edit.Diagnostics);
        Assert.Equal((1.0, 2.0, 10.0, 11.0, 8.0, 30.0), await Bounds(session, end));
        Assert.Equal((GeometryKeys.Derive(edit.CommandId, end), (BodyFault?)null), ((await Record(session, end)).Geometry, (await Record(session, end)).Fault));
    }

    // Item 5: with an open profile the command applies, the body keeps its last solid and gets
    // W-FEATURE-REBUILD-FAILED; the edit that closes the profile rebuilds it with the new distance.
    [NativeManifoldFact]
    public async Task A_Broken_Profile_Gives_A_Fault_And_The_Repair_Takes_The_New_Distance()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var body = await Extrude(session, sketch, 5);
        await Applied(session, new DeleteSketchItemCommand { SketchId = sketch.Id, ItemId = sketch.Lines[3] });
        var broken = await Record(session, body);

        var edit = await SetDistance(session, body, 9);

        var diagnostic = Assert.Single(edit.Diagnostics);
        Assert.Equal((DiagnosticCodes.FeatureRebuildFailed, (object?)body), (diagnostic.Code, diagnostic.Data!["bodyId"]));
        var faulted = await Record(session, body);
        Assert.Equal((broken.Geometry, DiagnosticCodes.FeatureRebuildFailed), (faulted.Geometry, faulted.Fault?.Code));

        await Applied(session, new AddSketchLineCommand { SketchId = sketch.Id, StartPointId = sketch.Points[3], EndPointId = sketch.Points[0] });
        Assert.Null((await Record(session, body)).Fault);
        Assert.Equal((1.0, 2.0, 0.0, 11.0, 8.0, 9.0), await Bounds(session, body));
    }

    // Item 7: GetBodyInfo gives the distance in effect of an extrude, and null for a box.
    [NativeManifoldFact]
    public async Task The_Query_Gives_The_Distance_In_Effect()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var body = await Extrude(session, await NewSketch(session, Rectangle), 5);
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }), "bodyId");

        async Task<double?> Distance(Guid id) => (await session.Query<BodyInfo>(new GetBodyInfoQuery { BodyId = id })).Result!.Distance;

        Assert.Equal(5.0, await Distance(body));
        Assert.Null(await Distance(box));
        await SetDistance(session, body, -3.5);
        Assert.Equal(-3.5, await Distance(body));
        await Applied(session, new UndoCommand());
        Assert.Equal(5.0, await Distance(body));
    }

    // Items 9 and 10: an open of the file applies each entry again, and gives the same records,
    // keys and boxes as the live session.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_Keys_And_Boxes()
    {
        var path = Path.Combine(_folder, "distance" + DocumentFile.Extension);
        IReadOnlyList<BodyRecord> expected;
        (double, double, double, double, double, double) box;
        Guid end;
        using (var backend = new ManifoldGeometryBackend())
        {
            var (live, _) = NewSession(backend);
            DocumentFile.Create(live, path);
            var sketch = await NewSketch(live, Rectangle);
            var body = await Extrude(live, sketch, 5);
            end = Output(await Applied(live, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = 3 }), "bodyId");
            await SetDistance(live, body, 11);
            await Applied(live, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = 15, Y = 12 });
            expected = await live.Read((document, _) => document.Bodies.ToList());
            box = await Bounds(live, end);
            live.Close();
        }

        Assert.Equal((1.0, 2.0, 3.0, 15.0, 12.0, 14.0), box);
        using (var backend = new ManifoldGeometryBackend())
        {
            var (opened, _) = NewSession(backend);
            await DocumentFile.Open(opened, path);

            Assert.Equal(expected, await opened.Read((document, _) => document.Bodies.ToList()));
            Assert.Equal(box, await Bounds(opened, end));
            opened.Close();
        }
    }
}
