using System.Runtime.InteropServices;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Sketch;

// The extrude of a closed profile of a sketch (ADR-0036, TASK-0103). ADR-0036 names this
// file in enforced-by. The native exports, the solid on each origin plane in each
// direction, the order of the loop, each refusal, the managed stub, the file, a replay
// and an undo.
public sealed class ExtrudeTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-extrude-" + Guid.NewGuid().ToString("N"));

    public ExtrudeTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : Guid.Empty;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    // A sketch on the datum with a closed chain of lines through the positions, in order.
    private static async Task<(Guid Sketch, Guid[] Points, Guid[] Lines)> Chain(
        DocumentSession session, Datum datum, params (double X, double Y)[] positions)
    {
        var sketch = await Applied(session, new CreateSketchCommand { DatumId = datum.Id }, "sketchId");
        var points = new Guid[positions.Length];
        for (var i = 0; i < positions.Length; i++)
            points[i] = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = positions[i].X, Y = positions[i].Y }, "pointId");
        var lines = new Guid[positions.Length];
        for (var i = 0; i < positions.Length; i++)
            lines[i] = await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }, "lineId");
        return (sketch, points, lines);
    }

    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    // KD5 of the roadmap: the native package exports each function of the extrude on
    // the platform of the run. A load of each name proves more than the search of the
    // binaries of 2026-10-29.
    [NativeManifoldFact]
    public void The_Native_Library_Exports_Each_Function_Of_The_Extrude()
    {
        Assert.True(NativeLibrary.TryLoad(ManifoldGeometryBackend.NativeLibraryName, typeof(ManifoldGeometryBackend).Assembly, null, out var library));
        try
        {
            string[] names =
            [
                "manifold_extrude", "manifold_simple_polygon", "manifold_polygons", "manifold_transform",
                "manifold_simple_polygon_size", "manifold_polygons_size",
                "manifold_destruct_simple_polygon", "manifold_destruct_polygons",
            ];
            var missing = names.Where(name => !NativeLibrary.TryGetExport(library, name, out _)).ToList();
            Assert.True(missing.Count == 0, "Missing exports: " + string.Join(", ", missing));
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    // ADR-0036 items 3 and 6: a rectangle of 10 by 6 on each origin plane, extruded by 5
    // and by −5, gives the volume 300 and the box that the frame of its datum gives. A
    // negative distance goes along −n.
    [NativeManifoldTheory]
    [InlineData("XY", 5, 1, 2, 0, 11, 8, 5)]
    [InlineData("XY", -5, 1, 2, -5, 11, 8, 0)]
    [InlineData("XZ", 5, 1, -5, 2, 11, 0, 8)]
    [InlineData("XZ", -5, 1, 0, 2, 11, 5, 8)]
    [InlineData("YZ", 5, 0, 1, 2, 5, 11, 8)]
    [InlineData("YZ", -5, -5, 1, 2, 0, 11, 8)]
    public async Task A_Rectangle_On_Each_Plane_Gives_The_Solid_Of_Its_Frame(
        string plane, double distance, double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var datum = Datums.All.Single(d => d.Name == plane);
        var (sketch, _, _) = await Chain(session, datum, Rectangle);

        var body = await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = distance }, "bodyId");

        Assert.Equal(300.0, backend.Volume(new BodyHandle(body)), 9);
        var box = (await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result;
        Assert.Equal((minX, minY, minZ, maxX, maxY, maxZ), (box.MinX, box.MinY, box.MinZ, box.MaxX, box.MaxY, box.MaxZ));
        var info = (await session.Query<BodyInfo>(new GetBodyInfoQuery { BodyId = body })).Result!;
        Assert.Equal("Extrude", info.Kind);
    }

    // ADR-0036 item 2: a loop drawn clockwise turns round, so the solid has a volume and
    // is not a hole.
    [NativeManifoldFact]
    public async Task A_Clockwise_Profile_Gives_The_Same_Solid()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var (sketch, _, _) = await Chain(session, Datums.XY, [.. Enumerable.Reverse(Rectangle)]);

        var body = await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }, "bodyId");

        Assert.Equal(300.0, backend.Volume(new BodyHandle(body)), 9);
    }

    // ADR-0036 item 2: the loop starts at the start of the first line, follows the lines,
    // and turns round when its area is negative. The order of the record decides it.
    [Fact]
    public void The_Loop_Follows_The_Record_And_Is_Counter_Clockwise()
    {
        var sketch = SketchModel.New(Guid.NewGuid(), Datums.XY.Id, null);
        var a = new SketchPoint(Guid.NewGuid(), 0, 0);
        var b = new SketchPoint(Guid.NewGuid(), 0, 4);
        var c = new SketchPoint(Guid.NewGuid(), 3, 4);
        var d = new SketchPoint(Guid.NewGuid(), 3, 0);
        // A clockwise square whose lines are not in the order of the loop, and the line
        // CD has its ends the other way.
        var clockwise = sketch with
        {
            Points = [.. sketch.Points, a, b, c, d],
            Lines =
            [
                new SketchLine(Guid.NewGuid(), a.Id, b.Id),
                new SketchLine(Guid.NewGuid(), d.Id, a.Id),
                new SketchLine(Guid.NewGuid(), d.Id, c.Id),
                new SketchLine(Guid.NewGuid(), b.Id, c.Id),
            ],
        };

        var (loop, reason) = SketchProfile.Find(clockwise);

        Assert.Null(reason);
        Assert.Equal([(3.0, 0.0), (3.0, 4.0), (0.0, 4.0), (0.0, 0.0)], loop!);
    }

    // ADR-0036 items 2 and 4: each profile that is not one closed loop, and a sketch that
    // is not solved, gives E-SKETCH-PROFILE-INVALID with its reason, and the Document
    // does not change. The checks come before the backend, so the managed stub runs them.
    [Theory]
    [InlineData("open", "is the end of 1 line,")]
    [InlineData("branch", "is the end of 3 lines")]
    [InlineData("two loops", "more than one loop")]
    [InlineData("crossing", "cross or touch")]
    [InlineData("touch", "cross or touch")]
    [InlineData("area zero", "area of the loop is zero")]
    [InlineData("no line", "no line")]
    [InlineData("not solved", "not solved")]
    public async Task Each_Profile_That_Is_Not_One_Closed_Loop_Is_Refused(string shape, string reason)
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var sketch = shape switch
        {
            "open" => await Open(session),
            "branch" => await Branch(session),
            "two loops" => await TwoLoops(session),
            "crossing" => (await Chain(session, Datums.XY, (0, 0), (10, 6), (10, 0), (0, 6))).Sketch,
            "touch" => (await Chain(session, Datums.XY, (0, 0), (10, 0), (10, 6), (5, 0))).Sketch,
            "area zero" => (await Chain(session, Datums.XY, (0, 0), (5, 0), (10, 0))).Sketch,
            "no line" => await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId"),
            _ => await NotSolved(session),
        };
        var before = await session.Read((document, _) => (document.Version, document.Bodies.Count));

        var result = await session.Apply(new ExtrudeCommand { SketchId = sketch, Distance = 5 });

        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(DiagnosticCodes.SketchProfileInvalid, result.Error!.Code);
        Assert.Contains(reason, result.Error.Message);
        Assert.Equal(before, await session.Read((document, _) => (document.Version, document.Bodies.Count)));
    }

    private static async Task<Guid> Open(DocumentSession session)
    {
        var (sketch, _, lines) = await Chain(session, Datums.XY, Rectangle);
        await Applied(session, new DeleteSketchItemCommand { SketchId = sketch, ItemId = lines[3] }, "itemId");
        return sketch;
    }

    private static async Task<Guid> Branch(DocumentSession session)
    {
        var (sketch, points, _) = await Chain(session, Datums.XY, Rectangle);
        var tail = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = -5, Y = -5 }, "pointId");
        await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[0], EndPointId = tail }, "lineId");
        return sketch;
    }

    private static async Task<Guid> TwoLoops(DocumentSession session)
    {
        var (sketch, _, _) = await Chain(session, Datums.XY, (0, 0), (4, 0), (0, 4));
        var p = new Guid[3];
        (double X, double Y)[] second = [(10, 0), (14, 0), (10, 4)];
        for (var i = 0; i < 3; i++)
            p[i] = await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = second[i].X, Y = second[i].Y }, "pointId");
        for (var i = 0; i < 3; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = p[i], EndPointId = p[(i + 1) % 3] }, "lineId");
        return sketch;
    }

    // Two distances on one line that conflict: the solver finds no solution (ADR-0035).
    private static async Task<Guid> NotSolved(DocumentSession session)
    {
        var (sketch, _, lines) = await Chain(session, Datums.XY, Rectangle);
        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 10 }, "constraintId");
        await Applied(session, new AddSketchConstraintCommand { SketchId = sketch, Kind = "distance", First = lines[0], Value = 7 }, "constraintId");
        return sketch;
    }

    // ADR-0036 items 3 and 4, and ADR-0012 item 7: the other refusals, in the order of the
    // handler. The managed stub has no IExtrudeOps.
    [Fact]
    public async Task The_Distance_The_Sketch_And_The_Capability_Are_Checked()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var (sketch, _, _) = await Chain(session, Datums.XY, Rectangle);

        Assert.Equal(DiagnosticCodes.SketchNotFound, await Refused(session, new ExtrudeCommand { SketchId = Guid.NewGuid(), Distance = 5 }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new ExtrudeCommand { SketchId = sketch, Distance = 0 }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new ExtrudeCommand { SketchId = sketch, Distance = double.NaN }));
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, await Refused(session, new ExtrudeCommand { SketchId = sketch, Distance = double.NegativeInfinity }));
        Assert.Equal(DiagnosticCodes.GeomCapMissing, await Refused(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }));
    }

    // ADR-0036 items 7 and 8, and ADR-0015: the sketch stays live and does not change, an
    // edit of the sketch leaves the solid as it was (K0b changes that), an open of the
    // file gives the same solid, and an undo removes it.
    [NativeManifoldFact]
    public async Task The_Sketch_Stays_The_File_Replays_The_Solid_And_An_Undo_Removes_It()
    {
        var path = Path.Combine(_folder, "extrude" + DocumentFile.Extension);
        Aabb expected;
        Guid body;
        using (var backend = new ManifoldGeometryBackend())
        {
            var live = new DocumentSession(EngineHosting.CreateDefault(backend));
            DocumentFile.Create(live, path);
            var (sketch, points, _) = await Chain(live, Datums.XZ, Rectangle);
            var before = await live.Read((document, _) => document.Sketches.Single());

            body = await Applied(live, new ExtrudeCommand { SketchId = sketch, Distance = -5 }, "bodyId");

            Assert.Equal(before, await live.Read((document, _) => document.Sketches.Single()));
            expected = (await live.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result;
            await Applied(live, new MoveSketchPointCommand { SketchId = sketch, PointId = points[2], X = 20, Y = 20 }, "pointId");
            Assert.Equal(expected, (await live.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result);
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var opened = new DocumentSession(EngineHosting.CreateDefault(backend));
            await DocumentFile.Open(opened, path);
            Assert.Equal(expected, (await opened.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result);
            Assert.Equal(300.0, backend.Volume(new BodyHandle(body)), 9);

            await Applied(opened, new UndoCommand(), "commandId");
            await Applied(opened, new UndoCommand(), "commandId");
            Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await opened.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Error?.Code);
            Assert.Equal(1, await opened.Read((document, _) => document.Sketches.Count));
            opened.Close();
        }
    }
}
