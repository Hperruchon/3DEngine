using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// A datum on a face of a feature (ADR-0051, TASK-0166). ADR-0051 names this file in enforced-by. The
// frames of the caps and the sides of an extrude and of the faces of a box, the place of a reference
// in the log, a sketch and an extrude on a datum, the rebuild after an edit of the base, a fault, the
// refusals, an undo and a redo, and an open.
public sealed class DatumTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-datum-" + Guid.NewGuid().ToString("N"));

    public DatumTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    private static readonly DatumAxis X = new(1, 0, 0);
    private static readonly DatumAxis Y = new(0, 1, 0);
    private static readonly DatumAxis Z = new(0, 0, 1);

    private static DatumAxis Minus(DatumAxis a) => new(-a.X, -a.Y, -a.Z);

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

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(IGeometryBackend backend)
    {
        var kit = EngineHosting.CreateDefault(backend);
        return (new DocumentSession(kit), kit.Events);
    }

    // A closed sketch of the corners on a datum.
    private static async Task<Sketch> NewSketch(DocumentSession session, Guid datum, (double X, double Y)[] corners)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
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

    private static async Task<Guid> Datum(DocumentSession session, Guid feature, string element, Guid? item = null)
        => Output(await Applied(session, new CreateDatumCommand { FeatureId = feature, Element = element, ItemId = item }), "datumId");

    private static async Task<DatumRecord> Get(DocumentSession session, Guid datum)
    {
        var result = await session.Query<DatumRecord>(new GetDatumQuery { DatumId = datum });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!;
    }

    private static async Task<DatumFrame> Frame(DocumentSession session, Guid datum) => (await Get(session, datum)).Frame;

    private static async Task<(double, double, double, double, double, double)> Bounds(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        var box = result.Result;
        return (Math.Round(box.MinX, 9), Math.Round(box.MinY, 9), Math.Round(box.MinZ, 9), Math.Round(box.MaxX, 9), Math.Round(box.MaxY, 9), Math.Round(box.MaxZ, 9));
    }

    private static DatumAxis Cross(DatumAxis a, DatumAxis b)
        => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    // Item 3: the end cap is at the distance with the frame of the sketch, and the start cap is on
    // the plane of the sketch, turned so that its normal goes out of the solid; a negative distance
    // swaps the two.
    [NativeManifoldFact]
    public async Task The_Caps_Of_An_Extrude_Give_Their_Frames()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var up = await Extrude(session, await NewSketch(session, Datums.XY.Id, Rectangle), 5);
        var down = await Extrude(session, await NewSketch(session, Datums.XY.Id, Rectangle), -5);

        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, 5), X, Y, Z), await Frame(session, await Datum(session, up, "end")));
        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, 0), X, Minus(Y), Minus(Z)), await Frame(session, await Datum(session, up, "start")));
        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, -5), X, Minus(Y), Minus(Z)), await Frame(session, await Datum(session, down, "end")));
        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, 0), X, Y, Z), await Frame(session, await Datum(session, down, "start")));
    }

    // Item 3: the side of a line starts at the first end of the line in the counter-clockwise loop,
    // runs along it, has the normal of the sketch as v, and faces out of the solid. A clockwise record
    // gives the same outward sides.
    [NativeManifoldFact]
    public async Task The_Side_Of_A_Line_Faces_Out_Of_The_Solid()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, sketch, 5);

        Assert.Equal(new DatumFrame(new DatumPoint(1, 2, 0), X, Z, Minus(Y)), await Frame(session, await Datum(session, body, "side", sketch.Lines[0])));
        Assert.Equal(new DatumFrame(new DatumPoint(11, 2, 0), Y, Z, X), await Frame(session, await Datum(session, body, "side", sketch.Lines[1])));

        var clockwise = await NewSketch(session, Datums.XY.Id, [.. Rectangle.Reverse()]);
        var other = await Extrude(session, clockwise, 5);
        // The first line of the record goes from (1, 8) to (11, 8), the top, and the loop runs it from (11, 8) to (1, 8).
        Assert.Equal(new DatumFrame(new DatumPoint(11, 8, 0), Minus(X), Z, Y), await Frame(session, await Datum(session, other, "side", clockwise.Lines[0])));
    }

    // Item 3: the six faces of a box of 40 by 30 by 10 with its centre at the origin.
    [Theory]
    [InlineData("+x", 20, 0, 0)]
    [InlineData("-x", -20, 0, 0)]
    [InlineData("+y", 0, 15, 0)]
    [InlineData("-y", 0, -15, 0)]
    [InlineData("+z", 0, 0, 5)]
    [InlineData("-z", 0, 0, -5)]
    public async Task Each_Face_Of_A_Box_Gives_A_Right_Handed_Frame_That_Faces_Out(string element, double x, double y, double z)
    {
        var (session, _) = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");

        var frame = await Frame(session, await Datum(session, box, element));

        Assert.Equal(new DatumPoint(x, y, z), frame.Origin);
        Assert.Equal(frame.Normal, Cross(frame.U, frame.V));
        var sign = element[0] == '+' ? 1 : -1;
        var outward = element[1] switch { 'x' => X, 'y' => Y, _ => Z };
        Assert.Equal(sign > 0 ? outward : Minus(outward), frame.Normal);
    }

    // Item 2: a Translate before the datum moves it, and a Translate after it does not, also when a
    // later edit resolves the datum again.
    [NativeManifoldFact]
    public async Task A_Translate_Before_The_Datum_Moves_It_And_A_Later_One_Does_Not()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var body = await Extrude(session, await NewSketch(session, Datums.XY.Id, Rectangle), 5);
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 1, Dy = 2, Dz = 3 }), "bodyId");
        var datum = await Datum(session, body, "end");
        await Applied(session, new TranslateCommand { BodyId = moved, Dx = 100, Dy = 0, Dz = 0 });

        Assert.Equal(new DatumPoint(1, 2, 8), (await Frame(session, datum)).Origin);
        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = body, Distance = 7 });
        Assert.Equal(new DatumPoint(1, 2, 10), (await Frame(session, datum)).Origin);
    }

    // Items 2 and 8: a Subtract on the chain passes the carrier with no change, and a Translate after
    // it, before the datum, moves the datum. The face +z of the minuend is at 5, moved by 3.
    [NativeManifoldFact]
    public async Task A_Subtract_Passes_The_Carrier_And_A_Translate_After_It_Moves_It()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var minuend = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");
        var subtrahend = Output(await Applied(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 40 }), "bodyId");
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend }), "bodyId");
        await Applied(session, new TranslateCommand { BodyId = cut, Dx = 0, Dy = 0, Dz = 3 });

        Assert.Equal(new DatumPoint(0, 0, 8), (await Frame(session, await Datum(session, minuend, "+z"))).Origin);
        Assert.Equal(new DatumPoint(0, 0, 23), (await Frame(session, await Datum(session, subtrahend, "+z"))).Origin);
    }

    // Item 5: a sketch on a datum extrudes from its face: on the top of a box, and on its face -x,
    // whose u is +Z and v is +Y.
    [NativeManifoldFact]
    public async Task A_Sketch_On_A_Datum_Extrudes_From_Its_Face()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 30, SizeZ = 10 }), "bodyId");

        var top = await Extrude(session, await NewSketch(session, await Datum(session, box, "+z"), Rectangle), 4);
        var side = await Extrude(session, await NewSketch(session, await Datum(session, box, "-x"), Rectangle), 4);

        Assert.Equal((1.0, 2.0, 5.0, 11.0, 8.0, 9.0), await Bounds(session, top));
        Assert.Equal((-24.0, 2.0, 1.0, -20.0, 8.0, 11.0), await Bounds(session, side));
    }

    // Item 6: a new distance of the base moves the datum on its end, and the extrude of the sketch on
    // the datum follows, in the same commit, with datum.changed after the body events.
    [NativeManifoldFact]
    public async Task An_Edit_Of_The_Base_Moves_The_Datum_And_The_Solid_On_It()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, events) = NewSession(backend);
        var base_ = await Extrude(session, await NewSketch(session, Datums.XY.Id, Rectangle), 5);
        var datum = await Datum(session, base_, "end");
        var boss = await Extrude(session, await NewSketch(session, datum, [(2, 3), (4, 3), (4, 5), (2, 5)]), 2);
        Assert.Equal((2.0, 3.0, 5.0, 4.0, 5.0, 7.0), await Bounds(session, boss));

        var edit = await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = base_, Distance = 8 });

        Assert.Empty(edit.Diagnostics);
        Assert.Equal(new DatumPoint(0, 0, 8), (await Frame(session, datum)).Origin);
        Assert.Equal((2.0, 3.0, 8.0, 4.0, 5.0, 10.0), await Bounds(session, boss));
        var kinds = events.Snapshot().Where(e => e.CauseCommandId == edit.CommandId).Select(e => e.Kind).ToList();
        Assert.Equal(["command.applied", "body.changed", "body.changed", "datum.changed"], kinds);
    }

    // Item 6: a move of the points of a line moves the datum on its side, and the solid on it.
    [NativeManifoldFact]
    public async Task An_Edit_Of_The_Base_Sketch_Moves_A_Datum_On_A_Side()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var base_ = await Extrude(session, sketch, 5);
        var datum = await Datum(session, base_, "side", sketch.Lines[1]);
        var boss = await Extrude(session, await NewSketch(session, datum, [(1, 1), (3, 1), (3, 3), (1, 3)]), 2);
        Assert.Equal((11.0, 3.0, 1.0, 13.0, 5.0, 3.0), await Bounds(session, boss));

        await Applied(session, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[1], X = 15, Y = 2 });
        await Applied(session, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = 15, Y = 8 });

        Assert.Equal(new DatumFrame(new DatumPoint(15, 2, 0), Y, Z, X), await Frame(session, datum));
        Assert.Equal((15.0, 3.0, 1.0, 17.0, 5.0, 3.0), await Bounds(session, boss));
    }

    // Item 6 (a failure): a delete of the line of the side breaks the reference. The datum keeps its
    // last plane and gets W-DATUM-REFERENCE-FAILED, the edit applies, and the solid on the datum keeps
    // its place. An undo gives back the record before the edit, with no fault.
    [NativeManifoldFact]
    public async Task A_Reference_That_Does_Not_Resolve_Keeps_Its_Last_Plane_With_A_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var base_ = await Extrude(session, sketch, 5);
        var datum = await Datum(session, base_, "side", sketch.Lines[1]);
        var boss = await Extrude(session, await NewSketch(session, datum, [(1, 1), (3, 1), (3, 3), (1, 3)]), 2);
        var before = await Get(session, datum);

        var delete = await Applied(session, new DeleteSketchItemCommand { SketchId = sketch.Id, ItemId = sketch.Lines[1] });

        var after = await Get(session, datum);
        Assert.Equal(before.Frame, after.Frame);
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, after.Fault?.Code);
        Assert.Contains(delete.Diagnostics, d => d.Code == DiagnosticCodes.DatumReferenceFailed && Equals(d.Data?["datumId"], datum));
        Assert.Equal((11.0, 3.0, 1.0, 13.0, 5.0, 3.0), await Bounds(session, boss));

        await Applied(session, new UndoCommand());
        Assert.Equal(before, await Get(session, datum));
    }

    // Item 7: each refusal of CreateDatum, GetDatum and CreateSketch.
    [NativeManifoldFact]
    public async Task Each_Refusal_Gives_Its_Code()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, _) = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, sketch, 5);
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }), "bodyId");
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = box, Dx = 1, Dy = 0, Dz = 0 }), "bodyId");

        foreach (var (feature, element, item) in new (Guid, string, Guid?)[]
        {
            (Guid.NewGuid(), "end", null),
            (sketch.Id, "end", null),
            (moved, "+x", null),
            (body, "startEdge", sketch.Lines[0]),
            (body, "top", null),
            (body, "side", null),
            (body, "end", sketch.Lines[0]),
            (body, "side", sketch.Points[0]),
            (body, "side", Guid.NewGuid()),
            (box, "end", null),
            (box, "+x", sketch.Lines[0]),
        })
            Assert.Equal(DiagnosticCodes.DatumReferenceInvalid, await Refused(session, new CreateDatumCommand { FeatureId = feature, Element = element, ItemId = item }));

        // A name of K0h gives the reason that names K0h, not the reason of a wrong item.
        var reserved = await session.Apply(new CreateDatumCommand { FeatureId = body, Element = "startEdge", ItemId = sketch.Lines[0] });
        Assert.Equal(DiagnosticCodes.DatumReferenceInvalid, reserved.Error?.Code);
        Assert.Contains("K0h", reserved.Error!.Message, StringComparison.Ordinal);

        Assert.Equal(DiagnosticCodes.DatumNameInvalid, await Refused(session, new CreateDatumCommand { FeatureId = body, Element = "end", DatumName = "a\nb" }));
        // A command sent again after the idempotency cache forgot it, as in ExtrudeDistanceTests.
        var create = new CreateDatumCommand { FeatureId = body, Element = "end" };
        await Applied(session, create);
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand());
        Assert.Equal(DiagnosticCodes.DatumIdInUse, await Refused(session, create with { Element = "start" }));
        Assert.Equal(DiagnosticCodes.DatumNotFound, (await session.Query<DatumRecord>(new GetDatumQuery { DatumId = Datums.XY.Id })).Error?.Code);
        Assert.Equal(DiagnosticCodes.SketchDatumNotFound, await Refused(session, new CreateSketchCommand { DatumId = Guid.NewGuid() }));

        // An undone extrude is not in effect.
        var undone = await Extrude(session, sketch, 3);
        await Applied(session, new UndoCommand());
        Assert.Equal(DiagnosticCodes.DatumReferenceInvalid, await Refused(session, new CreateDatumCommand { FeatureId = undone, Element = "end" }));
    }

    // Item 4: an undo removes the datum with datum.removed, and a redo gives it back with datum.created.
    [Fact]
    public async Task An_Undo_And_A_Redo_Remove_And_Give_Back_A_Datum()
    {
        var (session, events) = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 }), "bodyId");
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z", DatumName = "Top" }), "datumId");
        var record = await Get(session, datum);
        Assert.Equal(("Top", (DatumFault?)null), (record.Name, record.Fault));

        var undo = await Applied(session, new UndoCommand());
        Assert.Equal(DiagnosticCodes.DatumNotFound, (await session.Query<DatumRecord>(new GetDatumQuery { DatumId = datum })).Error?.Code);
        var redo = await Applied(session, new RedoCommand());

        Assert.Equal(record, await Get(session, datum));
        Assert.Contains(events.Snapshot(), e => e.Kind == "datum.removed" && e.CauseCommandId == undo.CommandId);
        Assert.Contains(events.Snapshot(), e => e.Kind == "datum.created" && e.CauseCommandId == redo.CommandId);
    }

    // Item 2: the open of a file applies each entry, so it gives the same datums and the same solids.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_Datums_And_Solids()
    {
        var path = Path.Combine(_folder, "datum" + DocumentFile.Extension);
        IReadOnlyList<DatumRecord> datums;
        IReadOnlyList<BodyRecord> bodies;
        (double, double, double, double, double, double) box;
        Guid boss;
        using (var backend = new ManifoldGeometryBackend())
        {
            var (live, _) = NewSession(backend);
            DocumentFile.Create(live, path);
            var sketch = await NewSketch(live, Datums.XY.Id, Rectangle);
            var base_ = await Extrude(live, sketch, 5);
            var datum = await Datum(live, base_, "side", sketch.Lines[2]);
            boss = await Extrude(live, await NewSketch(live, datum, [(1, 1), (3, 1), (3, 3), (1, 3)]), 2);
            await Applied(live, new SetExtrudeDistanceCommand { ExtrudeId = base_, Distance = 9 });
            datums = await live.Read((document, _) => document.Datums.ToList());
            bodies = await live.Read((document, _) => document.Bodies.ToList());
            box = await Bounds(live, boss);
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var (opened, _) = NewSession(backend);
            await DocumentFile.Open(opened, path);

            Assert.Equal(datums, await opened.Read((document, _) => document.Datums.ToList()));
            Assert.Equal(bodies, await opened.Read((document, _) => document.Bodies.ToList()));
            Assert.Equal(box, await Bounds(opened, boss));
            opened.Close();
        }
    }
}
