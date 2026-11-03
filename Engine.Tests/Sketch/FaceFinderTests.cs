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

// The face under a point of a hit (ADR-0053, TASK-0171). ADR-0053 names this file in enforced-by. The faces of a box and
// of an extrude, the walk through a Translate and a Subtract, the extent and the normal of a face, a curved side, the
// reuse of a datum, and the frame of a sketch in GetSketch.
public sealed class FaceFinderTests
{
    // A rectangle of 10 by 6 with its first corner at (1, 2), counter-clockwise.
    private static readonly (double X, double Y)[] Rectangle = [(1, 2), (11, 2), (11, 8), (1, 8)];

    private sealed record Sketch(Guid Id, Guid[] Points, Guid[] Lines);

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

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

    private static async Task<QueryResult<FaceInfo>> At(DocumentSession session, Guid body, (double X, double Y, double Z) point, (double X, double Y, double Z) normal)
        => await session.Query<FaceInfo>(new GetFaceAtQuery { BodyId = body, X = point.X, Y = point.Y, Z = point.Z, Nx = normal.X, Ny = normal.Y, Nz = normal.Z });

    private static async Task<DatumReference?> Face(DocumentSession session, Guid body, (double X, double Y, double Z) point, (double X, double Y, double Z) normal)
        => (await At(session, body, point, normal)).Result?.Reference;

    // Items 1 and 3: each face of a box at its centre. A point off the rectangle of a face, or a normal of another face,
    // gives no face; the box has its centre at the origin.
    [NativeManifoldFact]
    public async Task Each_Face_Of_A_Box_Is_Found_By_Its_Point_And_Normal()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 6, SizeZ = 4 }), "bodyId");

        Assert.Equal(new DatumReference(box, "+x", null), await Face(session, box, (5, 1, 1), (1, 0, 0)));
        Assert.Equal(new DatumReference(box, "-y", null), await Face(session, box, (2, -3, -1), (0, -1, 0)));
        Assert.Equal(new DatumReference(box, "+z", null), await Face(session, box, (4.9, 2.9, 2), (0, 0, 2)));

        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, box, (5, 4, 1), (1, 0, 0))).Error?.Code);
        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, box, (5, 1, 1), (0, 0, 1))).Error?.Code);
        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, box, (5.1, 1, 1), (1, 0, 0))).Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await At(session, Guid.NewGuid(), (5, 1, 1), (1, 0, 0))).Error?.Code);
    }

    // Item 3: the caps of an extrude hold the points inside the loop of the profile, and a side holds the rectangle of its
    // line and of the height. The plane of the end cap holds (0, 0, 5) too, but the loop does not.
    [NativeManifoldFact]
    public async Task The_Caps_And_The_Sides_Of_An_Extrude_Are_Found()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, sketch, 5);

        Assert.Equal(new DatumReference(body, "end", null), await Face(session, body, (6, 5, 5), (0, 0, 1)));
        Assert.Equal(new DatumReference(body, "start", null), await Face(session, body, (1, 2, 0), (0, 0, -1)));
        Assert.Equal(new DatumReference(body, "side", sketch.Lines[1]), await Face(session, body, (11, 5, 2), (1, 0, 0)));
        Assert.Equal(new DatumReference(body, "side", sketch.Lines[0]), await Face(session, body, (3, 2, 5), (0, -1, 0)));

        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, body, (0, 0, 5), (0, 0, 1))).Error?.Code);
        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, body, (11, 5, 5.1), (1, 0, 0))).Error?.Code);
    }

    // Item 2: a Translate moves each face, and the walk finds the face of the feature at its new place. A face of the
    // minuend of a Subtract is found; the wall of the hole is a face of the subtrahend, so it gives no face.
    [NativeManifoldFact]
    public async Task The_Walk_Goes_Through_A_Translate_And_To_The_Minuend()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, sketch, 5);
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = 10 }), "bodyId");

        Assert.Equal(new DatumReference(body, "end", null), await Face(session, moved, (6, 5, 15), (0, 0, 1)));
        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, moved, (6, 5, 5), (0, 0, 1))).Error?.Code);

        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 40, SizeZ = 4 }), "bodyId");
        var tool = Output(await Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }), "bodyId");
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        Assert.Equal(new DatumReference(plate, "+z", null), await Face(session, cut, (15, 15, 2), (0, 0, 1)));
        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, cut, (5, 0, 0), (-1, 0, 0))).Error?.Code);
    }

    // Items 1 and 3: the side of a circle is curved, so no datum names it, and a cap of the circle is a face.
    [NativeManifoldFact]
    public async Task A_Curved_Side_Is_Not_A_Face()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 5 });
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 3 }), "bodyId");

        Assert.Equal(new DatumReference(body, "end", null), await Face(session, body, (1, 1, 3), (0, 0, 1)));
        Assert.Equal(DiagnosticCodes.DatumFaceNotFound, (await At(session, body, (5, 0, 1), (1, 0, 0))).Error?.Code);
    }

    // Item 1: a datum with the reference and the frame of the face now is reused. A Translate after the datum moves the
    // face and not the datum (ADR-0051 item 2), so the datum is not on the face and the query gives no datum.
    [NativeManifoldFact]
    public async Task A_Datum_On_The_Face_Now_Is_Reused()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XY.Id, Rectangle);
        var body = await Extrude(session, sketch, 5);
        Assert.Null((await At(session, body, (6, 5, 5), (0, 0, 1))).Result?.DatumId);

        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = body, Element = "end" }), "datumId");
        Assert.Equal(datum, (await At(session, body, (6, 5, 5), (0, 0, 1))).Result?.DatumId);

        var moved = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 0, Dy = 0, Dz = 10 }), "bodyId");
        var after = (await At(session, moved, (6, 5, 15), (0, 0, 1))).Result;
        Assert.Equal(new DatumReference(body, "end", null), after?.Reference);
        Assert.Null(after?.DatumId);
    }

    // Item 7: GetSketch gives the frame of the datum of the sketch, an origin plane or a datum of the Document.
    [NativeManifoldFact]
    public async Task GetSketch_Gives_The_Frame_Of_Its_Datum()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Datums.XZ.Id, Rectangle);
        var body = await Extrude(session, sketch, 5);
        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = body, Element = "end" }), "datumId");
        var top = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");

        var onPlane = (await session.Query<SketchInfo>(new GetSketchQuery { SketchId = sketch.Id })).Result!;
        var onFace = (await session.Query<SketchInfo>(new GetSketchQuery { SketchId = top })).Result!;
        var record = (await session.Query<DatumRecord>(new GetDatumQuery { DatumId = datum })).Result!;

        Assert.Equal(new DatumFrame(new DatumPoint(0, 0, 0), Datums.XZ.U, Datums.XZ.V, Datums.XZ.Normal), onPlane.Frame);
        Assert.Equal(record.Frame, onFace.Frame);
    }
}
