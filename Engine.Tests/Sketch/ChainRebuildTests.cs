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

// The rebuild of the chain of a consumed extrude (ADR-0039, TASK-0116). ADR-0039 names this file
// in enforced-by. A translate, a subtract with the extrude as each operand, a chain of three
// steps, an undo and a redo, an empty result and its repair, an operand of another sketch, two
// extrudes in one subtract, an edit that changes nothing, the graph of a log, and an open.
public sealed class ChainRebuildTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-chain-" + Guid.NewGuid().ToString("N"));

    public ChainRebuildTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    private sealed record Sketch(Guid Id, Guid[] Points);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
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
        for (var i = 0; i < points.Length; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] });
        return new Sketch(sketch, points);
    }

    private static async Task<Guid> Extrude(DocumentSession session, Sketch sketch, double distance)
        => Output(await Applied(session, new ExtrudeCommand { SketchId = sketch.Id, Distance = distance }), "bodyId");

    private static async Task<Guid> Box(DocumentSession session, double x, double y, double z)
        => Output(await Applied(session, new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }), "bodyId");

    private static async Task<Guid> Translate(DocumentSession session, Guid body, double dx, double dy, double dz)
        => Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }), "bodyId");

    private static async Task<Guid> Subtract(DocumentSession session, Guid minuend, Guid subtrahend)
        => Output(await Applied(session, new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend }), "bodyId");

    private static Task<CommandResult> Move(DocumentSession session, Sketch sketch, int point, double x, double y)
        => Applied(session, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[point], X = x, Y = y });

    private static async Task<(double, double, double, double, double, double)> Bounds(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        var box = result.Result;
        return (Math.Round(box.MinX, 9), Math.Round(box.MinY, 9), Math.Round(box.MinZ, 9), Math.Round(box.MaxX, 9), Math.Round(box.MaxY, 9), Math.Round(box.MaxZ, 9));
    }

    private static Task<BodyRecord> Record(DocumentSession session, Guid body)
        => session.Read((document, _) => document.Bodies.Single(record => record.Handle.Id == body));

    // ADR-0039 items 2, 3 and 6: after a Translate consumes the extrude, an edit rebuilds the
    // translated body under a key of the edit, with one body.changed and no warning.
    [NativeManifoldFact]
    public async Task A_Translate_Of_The_Extrude_Follows_The_Edit()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var moved = await Translate(session, await Extrude(session, sketch, 5), 0, 0, 10);

        var edit = await Move(session, sketch, 2, 15, 12);

        Assert.Empty(edit.Diagnostics);
        Assert.Equal((1.0, 2.0, 10.0, 15.0, 12.0, 15.0), await Bounds(session, moved));
        var record = await Record(session, moved);
        Assert.Equal((GeometryKeys.Derive(edit.CommandId, moved), (BodyFault?)null), (record.Geometry, record.Fault));
        Assert.Single(events.Snapshot(), e => e.Kind == "body.changed" && e.CauseCommandId == edit.CommandId);
    }

    // ADR-0039 item 3: the extrude is the minuend of a cut by a box, and then the subtrahend of a
    // larger box; each end follows, and neither gets a fault.
    [NativeManifoldFact]
    public async Task A_Subtract_Follows_With_The_Extrude_As_Each_Operand()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var cutter = await Translate(session, await Box(session, 4, 4, 20), 1, 2, 0);
        var cut = await Subtract(session, await Extrude(session, sketch, 5), cutter);

        var other = await NewSketch(session, Rectangle);
        var plate = await Subtract(session, await Box(session, 40, 40, 2), await Extrude(session, other, 5));

        var first = await Move(session, sketch, 2, 15, 12);
        var second = await Move(session, other, 2, 15, 12);

        Assert.Empty(first.Diagnostics.Concat(second.Diagnostics));
        Assert.Equal((1.0, 2.0, 0.0, 15.0, 12.0, 5.0), await Bounds(session, cut));
        Assert.Equal(GeometryKeys.Derive(first.CommandId, cut), (await Record(session, cut)).Geometry);
        Assert.Equal((GeometryKeys.Derive(second.CommandId, plate), (BodyFault?)null), ((await Record(session, plate)).Geometry, (await Record(session, plate)).Fault));
    }

    // ADR-0039 items 3 and 8: a chain of a translate, a cut and a translate follows in log order.
    [NativeManifoldFact]
    public async Task A_Chain_Of_Three_Steps_Follows_In_Log_Order()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var up = await Translate(session, await Extrude(session, sketch, 5), 0, 0, 10);
        var cutter = await Translate(session, await Box(session, 4, 4, 40), 1, 2, 0);
        var cut = await Subtract(session, up, cutter);
        var end = await Translate(session, cut, 100, 0, 0);

        await Move(session, sketch, 2, 15, 12);

        Assert.Equal((101.0, 2.0, 10.0, 115.0, 12.0, 15.0), await Bounds(session, end));
        Assert.Null((await Record(session, end)).Fault);
    }

    // ADR-0039 item 6: an undo of the edit gives back the key and the solid before it, and a redo
    // the rebuilt ones.
    [NativeManifoldFact]
    public async Task An_Undo_And_A_Redo_Of_The_Edit_Swap_The_Records()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var moved = await Translate(session, await Extrude(session, sketch, 5), 0, 0, 10);
        var before = await Record(session, moved);

        var edit = await Move(session, sketch, 2, 15, 12);
        await Applied(session, new UndoCommand());

        Assert.Equal(before, await Record(session, moved));
        Assert.Equal((1.0, 2.0, 10.0, 11.0, 8.0, 15.0), await Bounds(session, moved));

        await Applied(session, new RedoCommand());
        Assert.Equal(GeometryKeys.Derive(edit.CommandId, moved), (await Record(session, moved)).Geometry);
        Assert.Equal((1.0, 2.0, 10.0, 15.0, 12.0, 15.0), await Bounds(session, moved));
    }

    // ADR-0039 item 5: a cut that the edit makes empty gives the end the fault and keeps its solid,
    // and the edit applies; an edit that mends the cut clears the fault.
    [NativeManifoldFact]
    public async Task An_Empty_Cut_Gives_A_Fault_And_A_Repair_Clears_It()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var cut = await Subtract(session, await Extrude(session, sketch, 5), await Box(session, 20, 20, 20));
        await Move(session, sketch, 1, 9, 2);
        var kept = await Record(session, cut);

        var broken = await Move(session, sketch, 2, 9, 8);

        var warning = Assert.Single(broken.Diagnostics);
        Assert.Equal((DiagnosticCodes.SketchRebuildFailed, (object?)cut), (warning.Code, warning.Data!["bodyId"]));
        var faulted = await Record(session, cut);
        Assert.Equal(kept.Geometry, faulted.Geometry);
        Assert.Contains("empty", faulted.Fault!.Message);

        await Move(session, sketch, 2, 11, 8);
        Assert.Null((await Record(session, cut)).Fault);
    }

    // ADR-0039 item 3 (Q1 A): an operand of another sketch follows its own sketch, also when the
    // first sketch changes later.
    [NativeManifoldFact]
    public async Task An_Operand_Of_Another_Sketch_Follows_Its_Own_Sketch()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var part = await NewSketch(session, Rectangle);
        var tool = await NewSketch(session, [(8, 0), (20, 0), (20, 10), (8, 10)]);
        var cut = await Subtract(session, await Extrude(session, part, 5), await Extrude(session, tool, 5));
        Assert.Equal((1.0, 2.0, 0.0, 8.0, 8.0, 5.0), await Bounds(session, cut));

        await Move(session, tool, 0, 6, 0);
        await Move(session, tool, 3, 6, 10);
        Assert.Equal((1.0, 2.0, 0.0, 6.0, 8.0, 5.0), await Bounds(session, cut));

        await Move(session, part, 0, 0, 2);
        Assert.Equal((0.0, 2.0, 0.0, 6.0, 8.0, 5.0), await Bounds(session, cut));
    }

    // ADR-0039 item 2: two extrudes of one sketch in one subtract give one end, one rebuild and one
    // body.changed.
    [NativeManifoldFact]
    public async Task Two_Extrudes_Of_One_Sketch_In_One_Subtract_Give_One_Change()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var top = await Extrude(session, sketch, 5);
        var lifted = await Translate(session, await Extrude(session, sketch, 2), 0, 0, 4);
        var cut = await Subtract(session, top, lifted);

        var edit = await Move(session, sketch, 2, 15, 12);

        Assert.Empty(edit.Diagnostics);
        Assert.Single(events.Snapshot(), e => e.Kind == "body.changed" && e.CauseCommandId == edit.CommandId);
        Assert.Equal((1.0, 2.0, 0.0, 15.0, 12.0, 4.0), await Bounds(session, cut));
    }

    // ADR-0039 items 2 and 8: two ends of one edit change in the log order of the commands that
    // create them, never in the order of the extrudes or of a set.
    [NativeManifoldFact]
    public async Task Two_Ends_Change_In_The_Log_Order_Of_Their_Commands()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var first = await Extrude(session, sketch, 5);
        var second = await Extrude(session, sketch, -5);
        var endOfSecond = await Translate(session, second, 0, 0, -10);
        var endOfFirst = await Translate(session, first, 0, 0, 10);

        var edit = await Move(session, sketch, 2, 15, 12);

        // The seeds come in the order first, second; their ends in the order of the log.
        var changed = events.Snapshot().Where(e => e.Kind == "body.changed" && e.CauseCommandId == edit.CommandId)
            .Select(e => ((IReadOnlyDictionary<string, object?>)e.Payload!)["bodyId"]).ToList();
        Assert.Equal([endOfSecond, endOfFirst], changed);
    }

    // ADR-0039 item 4: an edit that gives the same profile changes no body.
    [NativeManifoldFact]
    public async Task An_Edit_With_The_Same_Profile_Changes_Nothing()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var moved = await Translate(session, await Extrude(session, sketch, 5), 0, 0, 10);
        var before = await Record(session, moved);

        var edit = await Move(session, sketch, 2, 11, 8);

        Assert.Empty(edit.Diagnostics);
        Assert.Equal(before, await Record(session, moved));
        Assert.DoesNotContain(events.Snapshot(), e => e.Kind == "body.changed" && e.CauseCommandId == edit.CommandId);
    }

    // ADR-0039 item 1: the graph of a log gives the creator and the consumer of each handle, and an
    // undone step is not in the graph.
    [Fact]
    public void The_Graph_Gives_The_Creator_And_The_Consumer_Of_Each_Handle()
    {
        var box = new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 };
        var translate = new TranslateCommand { BodyId = box.CommandId, Dx = 1, Dy = 0, Dz = 0 };
        var other = new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 };
        var subtract = new SubtractCommand { MinuendBodyId = translate.CommandId, SubtrahendBodyId = other.CommandId };
        Command[] commands = [box, translate, other, subtract, new UndoCommand()];
        var log = commands.Select(command => new LogEntry(command, new BackendIdentity("test", "1"))).ToList();

        var graph = FeatureGraph.Of(log);

        Assert.Same(box, graph.Creator(box.CommandId));
        Assert.Same(translate, graph.Consumer(box.CommandId));
        Assert.Null(graph.Consumer(translate.CommandId));
        Assert.Null(graph.Creator(subtract.CommandId));
        Assert.True(graph.Order(box.CommandId) < graph.Order(other.CommandId));
    }

    // ADR-0039 item 8 and ADR-0015: an open of the file applies each entry again, and gives the
    // same records, keys and boxes as the live session.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_Keys_And_Boxes_Of_The_Chain()
    {
        var path = Path.Combine(_folder, "chain" + DocumentFile.Extension);
        IReadOnlyList<BodyRecord> expected;
        (double, double, double, double, double, double) box;
        Guid end;
        using (var backend = new ManifoldGeometryBackend())
        {
            var (live, _) = NewSession(backend);
            DocumentFile.Create(live, path);
            var sketch = await NewSketch(live, Rectangle);
            var cutter = await Translate(live, await Box(live, 4, 4, 20), 1, 2, 0);
            end = await Translate(live, await Subtract(live, await Extrude(live, sketch, 5), cutter), 0, 0, 3);
            await Move(live, sketch, 2, 15, 12);
            await Move(live, sketch, 1, 13, 1);
            expected = await live.Read((document, _) => document.Bodies.ToList());
            box = await Bounds(live, end);
            live.Close();
        }

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
