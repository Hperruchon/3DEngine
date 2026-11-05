using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// The references by the identity of a face (ADR-0059, phase K4c, TASK-0185). ADR-0059 names this file in enforced-by. A
// datum on a face that a cut removes, after a Subtract and after an edit, with an undo and a redo; a sketch on the wall
// of a hole; and a face that two cuts turn two times.
public sealed class FaceGoneTests
{
    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static async Task<Guid> Box(DocumentSession session, double x, double y, double z)
        => Output(await Applied(session, new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }), "bodyId");

    private static async Task<Guid> Moved(DocumentSession session, Guid body, double dx, double dy, double dz)
        => Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }), "bodyId");

    private static async Task<Guid> Cut(DocumentSession session, Guid minuend, Guid subtrahend)
        => Output(await Applied(session, new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend }), "bodyId");

    private static async Task<Guid> Datum(DocumentSession session, Guid feature, string element, Guid? item = null)
        => Output(await Applied(session, new CreateDatumCommand { FeatureId = feature, Element = element, ItemId = item }), "datumId");

    private static async Task<DatumRecord> Record(DocumentSession session, Guid datum)
        => await session.Read((document, _) => document.Datums.Single(item => item.Id == datum));

    // Item 3: a cut that removes the whole face +x of a plate gives its datum the fault W-DATUM-REFERENCE-FAILED, and the
    // datum stays on the plane of its reference. An undo of the cut removes the fault, and a redo gives it again.
    [NativeManifoldFact]
    public async Task A_Cut_That_Removes_The_Face_Of_A_Datum_Gives_It_A_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 10, 10, 2);
        var datum = await Datum(session, plate, "+x");
        var frame = (await Record(session, datum)).Frame;
        var top = await Datum(session, plate, "+z");
        var tool = await Moved(session, await Box(session, 4, 20, 20), 5, 0, 0);

        var cut = await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool });

        var warning = Assert.Single(cut.Diagnostics, item => item.Code == DiagnosticCodes.DatumReferenceFailed);
        Assert.Equal(datum, warning.Data!["datumId"]);
        var record = await Record(session, datum);
        Assert.Equal((DiagnosticCodes.DatumReferenceFailed, frame), (record.Fault?.Code, record.Frame));
        Assert.Contains("is gone", record.Fault!.Message, StringComparison.Ordinal);
        Assert.Null((await Record(session, top)).Fault);

        await Applied(session, new UndoCommand());
        Assert.Null((await Record(session, datum)).Fault);
        await Applied(session, new RedoCommand());
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Record(session, datum)).Fault?.Code);
    }

    // Item 3: an edit of the sketch of an extrude after a cut moves its side out of the tool, and the face comes back,
    // so the fault goes. An edit that moves it back into the tool gives the fault again, with one warning.
    [NativeManifoldFact]
    public async Task An_Edit_That_Moves_A_Face_Out_Of_And_Into_A_Cut_Changes_The_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 6), (0, 6)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        var lines = new Guid[4];
        for (var i = 0; i < 4; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] }), "lineId");
        var plate = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 2 }), "bodyId");
        var side = await Datum(session, plate, "side", lines[1]);
        var tool = await Moved(session, await Box(session, 4, 20, 20), 11, 0, 0);
        await Cut(session, plate, tool);
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Record(session, side)).Fault?.Code);

        // An edit of another sketch does not change the chain of the datum, so the datum keeps its fault from the kept
        // solid of the end. Each later edit of the other sketch reads the kept solid of the end again.
        var other = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] far = [(30, 0), (34, 0), (34, 4), (30, 4)];
        var farPoints = new Guid[4];
        for (var i = 0; i < 4; i++)
            farPoints[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = other, X = far[i].X, Y = far[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = other, StartPointId = farPoints[i], EndPointId = farPoints[(i + 1) % 4] });
        await Applied(session, new ExtrudeCommand { SketchId = other, Distance = 1 });
        async Task Unrelated(double x) => await Applied(session, new MoveSketchPointCommand { SketchId = other, PointId = farPoints[2], X = x, Y = 4 });
        await Unrelated(35);
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Record(session, side)).Fault?.Code);

        await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = points[1], X = 15, Y = 0 });
        var back = await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 15, Y = 6 });
        var record = await Record(session, side);
        Assert.Null(record.Fault);
        Assert.Equal(15, record.Frame.Origin.X);
        Assert.DoesNotContain(back.Diagnostics, item => item.Code == DiagnosticCodes.DatumReferenceFailed);
        await Unrelated(36);
        Assert.Null((await Record(session, side)).Fault);

        await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = points[1], X = 10, Y = 0 });
        var again = await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 10, Y = 6 });
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Record(session, side)).Fault?.Code);
        Assert.Single(again.Diagnostics, item => item.Code == DiagnosticCodes.DatumReferenceFailed);

        // A fault that stays gives no new warning: an edit of another point keeps the side inside the tool.
        var still = await Applied(session, new MoveSketchPointCommand { SketchId = sketch, PointId = points[3], X = 0, Y = 7 });
        Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Record(session, side)).Fault?.Code);
        Assert.DoesNotContain(still.Diagnostics, item => item.Code == DiagnosticCodes.DatumReferenceFailed);
    }

    // Item 4: a datum on the wall of a hole, made after the cut, faces into the hole, so a sketch on it extrudes into the
    // hole and not into the plate. The wall +x of the tool is at x = 2, and an extrude of 1 goes to x = 1.
    [NativeManifoldFact]
    public async Task A_Sketch_On_The_Wall_Of_A_Hole_Extrudes_Into_The_Hole()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 20, 20, 2);
        var tool = await Box(session, 4, 4, 10);
        await Cut(session, plate, tool);
        var wall = await Datum(session, tool, "+x");
        Assert.Equal(new DatumAxis(-1, 0, 0), (await Record(session, wall)).Frame.Normal);

        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = wall }), "sketchId");
        (double X, double Y)[] corners = [(-0.5, -0.5), (0.5, -0.5), (0.5, 0.5), (-0.5, 0.5)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        var plug = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 1 }), "bodyId");

        var box = (await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = plug })).Result;
        Assert.Equal((1.0, 2.0), (box.MinX, box.MaxX));
    }

    // Item 2: the projection keeps the solid of a committed key, and a body with a fault gives no solid, because it keeps
    // the solid of an earlier state.
    [NativeManifoldFact]
    public async Task The_Projection_Keeps_A_Committed_Solid_And_Gives_None_For_A_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        // An extrude keeps its solid under a key that is not its handle (ADR-0037 item 3).
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (2, 0), (2, 3), (0, 3)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 4 }), "bodyId");
        var record = await session.Read((document, _) => document.Bodies.Single(item => item.Handle.Id == body));
        Assert.NotEqual(record.Handle.Id, record.GeometryKey);

        var (solid, _) = await session.Read((document, _) => Engine.Core.Features.KernelProjection.OfRecord(document, record));
        Assert.NotNull(solid);
        Assert.True(Engine.Core.Features.KernelProjection.IsKept(record.GeometryKey));

        var faulted = record with { Fault = new BodyFault(DiagnosticCodes.SketchRebuildFailed, "A test fault.") };
        var (none, reason) = await session.Read((document, _) => Engine.Core.Features.KernelProjection.OfRecord(document, faulted));
        Assert.Null(none);
        Assert.Contains(DiagnosticCodes.SketchRebuildFailed, reason, StringComparison.Ordinal);
    }

    // Item 4: a face that two cuts take as a subtrahend turns two times. A tool cuts a hole in a block, and the block
    // with the hole cuts a large box: the plug of the large box inside the hole has the face +x of the tool, with its
    // normal +x again.
    [NativeManifoldFact]
    public async Task A_Face_That_Two_Cuts_Turn_Faces_Out_Again()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var block = await Box(session, 10, 10, 10);
        var tool = await Box(session, 4, 4, 20);
        var holed = await Cut(session, block, tool);
        var large = await Box(session, 40, 40, 40);
        var cast = await Cut(session, large, holed);

        var face = await session.Query<FaceInfo>(new GetFaceAtQuery { BodyId = cast, X = 2, Y = 0, Z = 0, Nx = 1, Ny = 0, Nz = 0 });
        Assert.Equal(new DatumReference(tool, "+x", null), face.Result?.Reference);
        var datum = await Datum(session, tool, "+x");
        Assert.Equal(new DatumAxis(1, 0, 0), (await Record(session, datum)).Frame.Normal);
    }
}
