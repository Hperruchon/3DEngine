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

namespace Engine.Tests.Commands;

// ADR-0112 (TASK-0335): the command Rotate turns a copy of a body about the origin of the Document by a quaternion.
// ADR-0112 names this file in enforced-by. The turn of a quaternion, the box of the shelf set upright, the refusals, the
// faces of the owned kernel, the frame of a datum, the rebuild after an edit, an undo and a redo, and an open.
public sealed class RotateCommandTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-rotate-" + Guid.NewGuid().ToString("N"));

    public RotateCommandTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A quarter turn about +x: y goes to z, and z goes to -y.
    private static RotateCommand QuarterAboutX(Guid body) => new() { BodyId = body, Qw = 1, Qx = 1, Qy = 0, Qz = 0 };

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

    private static async Task<(double, double, double, double, double, double)> Bounds(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        var box = result.Result;
        return (Math.Round(box.MinX, 9), Math.Round(box.MinY, 9), Math.Round(box.MinZ, 9), Math.Round(box.MaxX, 9), Math.Round(box.MaxY, 9), Math.Round(box.MaxZ, 9));
    }

    private static async Task<DatumFrame> Frame(DocumentSession session, Guid datum)
    {
        var result = await session.Query<DatumRecord>(new GetDatumQuery { DatumId = datum });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result!.Frame;
    }

    // Item 2: a quaternion of small integers gives exact entries; q, -q and 2q give one turn; a general quaternion gives a
    // turn whose axes have the length 1 and are at right angles, with a determinant of 1; no turn for a zero, a value that
    // is not finite, or a square sum that overflows.
    [Fact]
    public void A_Quaternion_Gives_A_Turn_Or_Nothing()
    {
        var quarter = Rotation.FromQuaternion(1, 1, 0, 0);
        Assert.Equal(new Rotation(1, 0, 0, 0, 0, 1, 0, -1, 0), quarter);
        Assert.Equal(quarter, Rotation.FromQuaternion(-1, -1, 0, 0));
        Assert.Equal(quarter, Rotation.FromQuaternion(2, 2, 0, 0));
        Assert.Equal(new Rotation(1, 0, 0, 0, 1, 0, 0, 0, 1), Rotation.FromQuaternion(1, 0, 0, 0));

        var r = Rotation.FromQuaternion(1, 2, 3, 4)!.Value;
        (double, double, double) X = (r.Xx, r.Xy, r.Xz), Y = (r.Yx, r.Yy, r.Yz), Z = (r.Zx, r.Zy, r.Zz);
        static double Dot((double A, double B, double C) a, (double A, double B, double C) b) => (a.A * b.A) + (a.B * b.B) + (a.C * b.C);
        static (double, double, double) Cross((double A, double B, double C) a, (double A, double B, double C) b)
            => ((a.B * b.C) - (a.C * b.B), (a.C * b.A) - (a.A * b.C), (a.A * b.B) - (a.B * b.A));
        foreach (var (a, b, expected) in new[] { (X, X, 1.0), (Y, Y, 1.0), (Z, Z, 1.0), (X, Y, 0.0), (Y, Z, 0.0), (Z, X, 0.0) })
            Assert.Equal(expected, Dot(a, b), 1e-15);
        Assert.Equal(1.0, Dot(X, Cross(Y, Z)), 1e-15);

        Assert.Null(Rotation.FromQuaternion(0, 0, 0, 0));
        Assert.Null(Rotation.FromQuaternion(double.NaN, 1, 0, 0));
        Assert.Null(Rotation.FromQuaternion(1, double.PositiveInfinity, 0, 0));
        Assert.Null(Rotation.FromQuaternion(1e200, 1e200, 0, 0));
    }

    // Items 1 and 3 and the reason of R-0057: a box built with y up, 10 by 30 by 20 on x, y and z, stands with z up after a
    // quarter turn about x. The command consumes the source, and the new body keeps its name and its visibility.
    [NativeManifoldFact]
    public async Task A_Quarter_Turn_About_X_Sets_A_Box_With_Y_Up_Upright()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 10, 30, 20);
        await Applied(session, new RenameBodyCommand { BodyId = box, NewName = "Shelf" });
        var turn = QuarterAboutX(box);

        var turned = Output(await Applied(session, turn), "bodyId");

        Assert.Equal(turn.CommandId, turned);
        Assert.Equal((-5.0, -10.0, -15.0, 5.0, 10.0, 15.0), await Bounds(session, turned));
        var bodies = await session.Read((document, _) => document.Bodies.ToList());
        var record = Assert.Single(bodies);
        Assert.Equal(new BodyRecord(new BodyHandle(turned), "Solid", "Shelf", true), record);
    }

    // Item 2: a quaternion that gives no turn is refused before the capability; an unknown body is refused; the managed
    // backend has no ITransformOps.
    [Fact]
    public async Task A_Quaternion_With_No_Turn_An_Unknown_Body_And_A_Backend_With_No_Transform_Are_Refused()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 1, 1, 1);

        var zero = await session.Apply(new RotateCommand { BodyId = box, Qw = 0, Qx = 0, Qy = 0, Qz = 0 });
        var nan = await session.Apply(new RotateCommand { BodyId = box, Qw = double.NaN, Qx = 1, Qy = 0, Qz = 0 });
        var unknown = await session.Apply(QuarterAboutX(Guid.NewGuid()));
        var stub = await session.Apply(QuarterAboutX(box));

        Assert.Equal(DiagnosticCodes.GeomInvalidParam, zero.Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, nan.Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, unknown.Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, stub.Error?.Code);
    }

    // ADR-0114 item 1 (finding E201 of the codebase review of 2026-11-10, TASK-0339): parts near 1e-162 have subnormal squares,
    // and the entries gave a scale of 5, so a cube of 10 became 50. A sum of squares below 2^-969 gives no turn, and the
    // command refuses it with its own code, before the capability. A sum of exactly 2^-969 gives the quarter turn exactly.
    [Fact]
    public async Task A_Quaternion_Of_Very_Small_Parts_Is_Refused_With_Its_Code()
    {
        var half = Math.ScaleB(1.0, -485);
        Assert.Equal(new Rotation(1, 0, 0, 0, 0, 1, 0, -1, 0), Rotation.FromQuaternion(half, half, 0, 0));
        Assert.Null(Rotation.FromQuaternion(half, 0, 0, 0));
        Assert.Null(Rotation.FromQuaternion(1e-162, 1e-162, 1e-162, 1e-162));

        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 1, 1, 1);
        var tiny = await session.Apply(new RotateCommand { BodyId = box, Qw = 1e-162, Qx = 1e-162, Qy = 1e-162, Qz = 1e-162 });
        var below = await session.Apply(new RotateCommand { BodyId = box, Qw = half, Qx = 0, Qy = 0, Qz = 0 });
        var zero = await session.Apply(new RotateCommand { BodyId = box, Qw = 0, Qx = 0, Qy = 0, Qz = 0 });

        Assert.Equal(DiagnosticCodes.GeomQuaternionTooSmall, tiny.Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomQuaternionTooSmall, below.Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, zero.Error?.Code);
        Assert.Equal(box, Assert.Single(await session.Read((document, _) => document.Bodies.ToList())).Handle.Id);
    }

    // Item 5: the owned kernel turns the faces with the body, so each face keeps its name, the normal of +z points to -y,
    // and a general turn keeps the area of each face.
    [NativeManifoldFact]
    public async Task The_Faces_Of_The_Kernel_Turn_With_The_Body()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 40, 30, 10);
        var quarter = Output(await Applied(session, QuarterAboutX(box)), "bodyId");
        var other = await Box(session, 40, 30, 10);
        var general = Output(await Applied(session, new RotateCommand { BodyId = other, Qw = 1, Qx = 2, Qy = 3, Qz = 4 }), "bodyId");

        var faces = (await session.Query<FacesInfo>(new GetFacesQuery { BodyId = quarter })).Result!.Faces;
        Assert.Equal(["+x", "+y", "+z", "-x", "-y", "-z"], faces.Select(face => face.Reference.Element).Order(StringComparer.Ordinal).ToArray());
        var top = Assert.Single(faces, face => face.Reference.Element == "+z");
        Assert.Equal(new DatumAxis(0, -1, 0), top.Normal);
        Assert.Equal(-5, top.Point.Y);

        var areas = (await session.Query<FacesInfo>(new GetFacesQuery { BodyId = general })).Result!.Faces.ToDictionary(face => face.Reference.Element, face => face.Area);
        Assert.Equal(1200, areas["+z"], 1e-9);
        Assert.Equal(300, areas["+x"], 1e-9);
        Assert.Equal(400, areas["+y"], 1e-9);
    }

    // Item 7: a datum on a face after the turn has the turned frame; a datum before it does not turn, as a datum before a
    // Translate does not move (ADR-0051 item 2).
    [NativeManifoldFact]
    public async Task A_Turn_Before_The_Datum_Turns_Its_Frame_And_A_Later_One_Does_Not()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 40, 30, 10);
        var before = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z" }), "datumId");
        var turned = Output(await Applied(session, QuarterAboutX(box)), "bodyId");
        var after = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z" }), "datumId");
        await Applied(session, new TranslateCommand { BodyId = turned, Dx = 0, Dy = 0, Dz = 100 });

        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, 5), new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1)), await Frame(session, before));
        Assert.Equal(new DatumFrame(new DatumPoint(0, -5, 0), new DatumAxis(1, 0, 0), new DatumAxis(0, 0, 1), new DatumAxis(0, -1, 0)), await Frame(session, after));
    }

    // Items 6 and 7: the end of the chain passes a turn, so a face that a cut removed is gone also after a turn of the
    // result; a wall of the hole turns with the body, after the turn of a face of a subtrahend (ADR-0059 item 4).
    [NativeManifoldFact]
    public async Task A_Face_That_A_Cut_Removed_Is_Gone_Also_After_A_Turn()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var minuend = await Box(session, 40, 30, 10);
        var subtrahend = await Box(session, 4, 4, 40);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = minuend, SubtrahendBodyId = subtrahend }), "bodyId");
        await Applied(session, QuarterAboutX(cut));

        var gone = await session.Apply(new CreateDatumCommand { FeatureId = subtrahend, Element = "+z" });
        Assert.Equal(DiagnosticCodes.DatumReferenceInvalid, gone.Error?.Code);
        Assert.Contains("is gone", gone.Error!.Message, StringComparison.Ordinal);
        var wall = Output(await Applied(session, new CreateDatumCommand { FeatureId = subtrahend, Element = "+x" }), "datumId");
        Assert.Equal(
            new DatumFrame(new DatumPoint(2, 0, 0), new DatumAxis(0, 0, 1), new DatumAxis(0, 1, 0), new DatumAxis(-1, 0, 0)),
            await Frame(session, wall));
    }

    // Item 6: an edit of an extrude before the turn runs the turn again, so the turned body follows the new distance.
    [NativeManifoldFact]
    public async Task An_Edit_Before_The_Turn_Turns_The_New_Solid()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(1, 2), (11, 2), (11, 8), (1, 8)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        var extrude = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }), "bodyId");
        var turned = Output(await Applied(session, QuarterAboutX(extrude)), "bodyId");

        Assert.Equal((1.0, -5.0, 2.0, 11.0, 0.0, 8.0), await Bounds(session, turned));
        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = extrude, Distance = 7 });
        Assert.Equal((1.0, -7.0, 2.0, 11.0, 0.0, 8.0), await Bounds(session, turned));
    }

    // ADR-0031: an undo of the turn gives the source back, and a redo turns it again.
    [NativeManifoldFact]
    public async Task An_Undo_Gives_The_Source_Back_And_A_Redo_Turns_It_Again()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 10, 30, 20);
        var turned = Output(await Applied(session, QuarterAboutX(box)), "bodyId");

        await Applied(session, new UndoCommand());
        Assert.Equal([box], await session.Read((document, _) => document.Bodies.Select(b => b.Handle.Id).ToList()));
        Assert.Equal((-5.0, -15.0, -10.0, 5.0, 15.0, 10.0), await Bounds(session, box));

        await Applied(session, new RedoCommand());
        Assert.Equal([turned], await session.Read((document, _) => document.Bodies.Select(b => b.Handle.Id).ToList()));
        Assert.Equal((-5.0, -10.0, -15.0, 5.0, 10.0, 15.0), await Bounds(session, turned));
    }

    // Items 1 and 8: the log holds the four numbers, so an open of the file gives the same bodies and the same solid.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_Turned_Solid()
    {
        var path = Path.Combine(_folder, "rotate" + DocumentFile.Extension);
        IReadOnlyList<BodyRecord> bodies;
        (double, double, double, double, double, double) bounds;
        Guid turned;
        using (var backend = new ManifoldGeometryBackend())
        {
            var live = NewSession(backend);
            DocumentFile.Create(live, path);
            var box = await Box(live, 10, 30, 20);
            turned = Output(await Applied(live, new RotateCommand { BodyId = box, Qw = 1, Qx = 2, Qy = 3, Qz = 4 }), "bodyId");
            bodies = await live.Read((document, _) => document.Bodies.ToList());
            bounds = await Bounds(live, turned);
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var opened = NewSession(backend);
            await DocumentFile.Open(opened, path);

            Assert.Equal(bodies, await opened.Read((document, _) => document.Bodies.ToList()));
            Assert.Equal(bounds, await Bounds(opened, turned));
            opened.Close();
        }
    }

    // Finding E200 of the codebase review of 2026-11-10 (phase P0.140, TASK-0338): after a Translate along the normal by a
    // decimal, or a general Rotate, a pocket from a datum on the moved +z face opens that face in the owned kernel: +z has
    // one hole and the area 1100, and no closed cavity stays. Until TASK-0338 the kernel took the frame of the datum in
    // doubles, one unit in the last place off the exact face, and +z stayed whole with an area of 1200. The first cases are
    // the probes of the review; the last one turns after a move.
    [NativeManifoldTheory]
    [InlineData(7.0, 0.0, 0.0, 0.3, 1.0, 0.0, 0.0, 0.0)]
    [InlineData(9.7, 0.3, 0.6, 0.1, 1.0, 0.0, 0.0, 0.0)]
    [InlineData(7.3, 0.37, 0.91, 0.13, 1.0, 0.0, 0.0, 0.0)]
    [InlineData(10.0, 0.0, 0.0, 0.0, 5.0, 1.0, 2.0, 3.0)]
    [InlineData(10.0, 0.0, 0.0, 0.0, 2.0, 3.0, 5.0, 7.0)]
    [InlineData(7.0, 0.1, 0.2, 0.3, 5.0, 1.0, 2.0, 3.0)]
    public async Task A_Pocket_From_A_Datum_On_A_Moved_Or_Turned_Face_Opens_It(double height, double dx, double dy, double dz, double qw, double qx, double qy, double qz)
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = await Box(session, 40, 30, height);
        var box = body;
        if (dx != 0 || dy != 0 || dz != 0)
            body = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }), "bodyId");
        if (qx != 0 || qy != 0 || qz != 0)
            body = Output(await Applied(session, new RotateCommand { BodyId = body, Qw = qw, Qx = qx, Qy = qy, Qz = qz }), "bodyId");
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z" }), "datumId");
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
        (double X, double Y)[] corners = [(-5, -5), (5, -5), (5, 5), (-5, 5)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        var tool = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = -2 }), "bodyId");
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = body, SubtrahendBodyId = tool }), "bodyId");

        var faces = (await session.Query<FacesInfo>(new GetFacesQuery { BodyId = cut })).Result!.Faces;
        var text = string.Join("; ", faces.Select(face => $"{face.Reference.Element} holes={face.Holes} area={Math.Round(face.Area, 3)}"));
        var top = faces.Single(face => face.Reference.Element == "+z");
        Assert.True(top.Holes == 1 && Math.Abs(top.Area - 1100) < 1e-6, text);
        Assert.DoesNotContain(faces, face => face.Reference.Element == "start");
    }

    // A square of the half size on the datum, extruded by the distance.
    private static async Task<Guid> Square(DocumentSession session, Guid datum, double x, double y, double half, double distance)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
        (double X, double Y)[] corners = [(x - half, y - half), (x + half, y - half), (x + half, y + half), (x - half, y + half)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        return Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = distance }), "bodyId");
    }

    // E200 (TASK-0338): a datum on the end or on a side of a post, which an extrude made on the moved or turned top of a
    // box, takes its exact frame from the exact frame of the sketch of the post; so a pocket there opens the face. Until
    // TASK-0338 a post of 4.7 on a box of 7 failed also with no move: the end of the datum was 3.5 + 4.7 in doubles.
    [NativeManifoldTheory]
    [InlineData(0.0, 1.0, 0.0, 0.0, 0.0, 4.7)]
    [InlineData(0.3, 1.0, 0.0, 0.0, 0.0, 4.7)]
    [InlineData(0.0, 5.0, 1.0, 2.0, 3.0, 4.7)]
    [InlineData(0.13, 2.0, 3.0, 5.0, 7.0, 3.3)]
    public async Task A_Pocket_On_The_End_And_On_A_Side_Of_A_Post_Opens_Each_Face(double dz, double qw, double qx, double qy, double qz, double height)
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 40, 30, 7);
        var body = box;
        if (dz != 0)
            body = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = dz }), "bodyId");
        if (qx != 0 || qy != 0 || qz != 0)
            body = Output(await Applied(session, new RotateCommand { BodyId = body, Qw = qw, Qx = qx, Qy = qy, Qz = qz }), "bodyId");
        var top = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z" }), "datumId");
        var post = await Square(session, top, 0, 0, 8, height);
        var postSketch = (await session.Read((document, _) => document.Sketches.ToList())).Single();
        var end = Output(await Applied(session, new CreateDatumCommand { FeatureId = post, Element = "end" }), "datumId");
        var first = await Square(session, end, 0, 0, 3, -2);
        var once = Output(await Applied(session, new SubtractCommand { MinuendBodyId = post, SubtrahendBodyId = first }), "bodyId");
        var side = Output(await Applied(session, new CreateDatumCommand { FeatureId = post, Element = "side", ItemId = postSketch.Lines[0].Id }), "datumId");
        var second = await Square(session, side, 8, height / 2, 1, -1);
        var twice = Output(await Applied(session, new SubtractCommand { MinuendBodyId = once, SubtrahendBodyId = second }), "bodyId");

        var faces = (await session.Query<FacesInfo>(new GetFacesQuery { BodyId = twice })).Result!.Faces;
        var text = string.Join("; ", faces.Select(face => $"{face.Reference.Element} holes={face.Holes} area={Math.Round(face.Area, 3)}"));
        var endFace = faces.Single(face => face.Reference.FeatureId == post && face.Reference.Element == "end");
        Assert.True(endFace.Holes == 1 && Math.Abs(endFace.Area - 220) < 1e-6, text);
        var sideFace = faces.Single(face => face.Reference.FeatureId == post && face.Reference.Element == "side" && face.Reference.ItemId == postSketch.Lines[0].Id);
        Assert.True(sideFace.Holes == 1 && Math.Abs(sideFace.Area - ((16 * height) - 4)) < 1e-6, text);
    }
}
