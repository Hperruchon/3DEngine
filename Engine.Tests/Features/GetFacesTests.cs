using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Features;

// The engine reads the owned kernel (ADR-0058, phase K4b, TASK-0184). ADR-0058 names this file in enforced-by. The item
// of each side of a profile, the faces of a box, an extrude, a circle, a translate and a cut, the faults, the cache, and
// the bounds of the kernel against those of Manifold.
public sealed class GetFacesTests
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

    private static async Task<Guid> Extrude(DocumentSession session, Guid sketch, double distance)
        => Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = distance }), "bodyId");

    private static async Task<Guid> Box(DocumentSession session, double x, double y, double z)
        => Output(await Applied(session, new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }), "bodyId");

    private static async Task<QueryResult<FacesInfo>> Query(DocumentSession session, Guid body)
        => await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });

    private static async Task<IReadOnlyList<FaceEntry>> Faces(DocumentSession session, Guid body)
    {
        var result = await Query(session, body);
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!.Faces;
    }

    private static FaceEntry Named(IReadOnlyList<FaceEntry> faces, Guid feature, string element, Guid? item = null)
        => Assert.Single(faces, face => face.Reference == new DatumReference(feature, element, item));

    // Item 2: each vertex of the loop carries the item of the side from it to the next vertex. A loop drawn clockwise
    // turns, and each side keeps its line; the facets of a circle carry the circle.
    [Fact]
    public void Each_Side_Of_A_Profile_Carries_Its_Item()
    {
        var (a, b, c, d) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (l0, l1, l2, l3) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        SketchRecord Of((double X, double Y)[] corners) => new(
            Guid.NewGuid(),
            Datums.XY.Id,
            null,
            [new SketchPoint(a, corners[0].X, corners[0].Y), new SketchPoint(b, corners[1].X, corners[1].Y), new SketchPoint(c, corners[2].X, corners[2].Y), new SketchPoint(d, corners[3].X, corners[3].Y)],
            [new SketchLine(l0, a, b), new SketchLine(l1, b, c), new SketchLine(l2, c, d), new SketchLine(l3, d, a)],
            [],
            [],
            []);

        var forward = SketchProfile.FindWithItems(Of(Rectangle)).Loop!;
        Assert.Equal([(1.0, 2.0, l0), (11.0, 2.0, l1), (11.0, 8.0, l2), (1.0, 8.0, l3)], forward);

        // Clockwise: a (1, 2), b (1, 8), c (11, 8), d (11, 2). After the turn the loop is d, c, b, a; the side from d to c
        // is the line c-d, l2.
        var backward = SketchProfile.FindWithItems(Of([(1, 2), (1, 8), (11, 8), (11, 2)])).Loop!;
        Assert.Equal([(11.0, 2.0, l2), (11.0, 8.0, l1), (1.0, 8.0, l0), (1.0, 2.0, l3)], backward);
        Assert.Equal(SketchProfile.Find(Of([(1, 2), (1, 8), (11, 8), (11, 2)])).Loop, backward.Select(v => (v.X, v.Y)));
    }

    // Items 2 and 4: a box gives six names with no item. Each one is one part with no hole, with the area and the unit
    // normal of its side, and a point on its plane inside it.
    [NativeManifoldFact]
    public async Task A_Box_Gives_Six_Named_Faces()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 10, 6, 4);

        var faces = await Faces(session, box);

        Assert.Equal(["+x", "-x", "+y", "-y", "+z", "-z"], faces.Select(face => face.Reference.Element));
        Assert.All(faces, face => Assert.Equal((box, (Guid?)null, 1, 0), (face.Reference.FeatureId, face.Reference.ItemId, face.Parts, face.Holes)));
        Assert.Equal([24.0, 24, 40, 40, 60, 60], faces.Select(face => face.Area));
        Assert.Equal(new DatumAxis(0, 0, 1), Named(faces, box, "+z").Normal);
        Assert.Equal(new DatumAxis(-1, 0, 0), Named(faces, box, "-x").Normal);
        var top = Named(faces, box, "+z").Point;
        Assert.True(top.Z == 2 && Math.Abs(top.X) < 5 && Math.Abs(top.Y) < 3, $"{top}");

        // ADR-0018 item 7: a host writes the result as JSON, with each field of an entry.
        var json = System.Text.Json.JsonSerializer.Serialize<object>(new FacesInfo(faces, []), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Contains("\"faces\":[{\"reference\":{\"featureId\":", json, StringComparison.Ordinal);
        Assert.Contains("\"parts\":1,\"holes\":0,\"area\":24,\"point\":{", json, StringComparison.Ordinal);
        Assert.Contains("\"normal\":{\"x\":1,\"y\":0,\"z\":0}", json, StringComparison.Ordinal);
    }

    // Items 2 and 4: an extrude gives start on the plane of the sketch, end at the distance, and one side for each line.
    // With a negative distance, start stays on the plane of the sketch and its normal turns.
    [NativeManifoldFact]
    public async Task An_Extrude_Names_Its_Caps_And_A_Side_For_Each_Line()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var up = await Extrude(session, sketch.Id, 3);
        var down = await Extrude(session, sketch.Id, -3);

        var faces = await Faces(session, up);
        Assert.Equal(6, faces.Count);
        Assert.Equal((new DatumAxis(0, 0, -1), 0.0, 60.0), (Named(faces, up, "start").Normal, Named(faces, up, "start").Point.Z, Named(faces, up, "start").Area));
        Assert.Equal((new DatumAxis(0, 0, 1), 3.0), (Named(faces, up, "end").Normal, Named(faces, up, "end").Point.Z));
        Assert.Equal((new DatumAxis(0, -1, 0), 30.0), (Named(faces, up, "side", sketch.Lines[0]).Normal, Named(faces, up, "side", sketch.Lines[0]).Area));
        Assert.Equal((new DatumAxis(1, 0, 0), 18.0), (Named(faces, up, "side", sketch.Lines[1]).Normal, Named(faces, up, "side", sketch.Lines[1]).Area));
        Assert.Equal(new DatumAxis(0, 1, 0), Named(faces, up, "side", sketch.Lines[2]).Normal);
        Assert.Equal(new DatumAxis(-1, 0, 0), Named(faces, up, "side", sketch.Lines[3]).Normal);

        var under = await Faces(session, down);
        Assert.Equal((new DatumAxis(0, 0, 1), 0.0), (Named(under, down, "start").Normal, Named(under, down, "start").Point.Z));
        Assert.Equal((new DatumAxis(0, 0, -1), -3.0), (Named(under, down, "end").Normal, Named(under, down, "end").Point.Z));
    }

    // Item 4 (the answer of the curved sides): the facets of a circle are one name and one part, with no one normal, and
    // their area is the area of the side of the polygon of the circle.
    [NativeManifoldFact]
    public async Task The_Facets_Of_A_Circle_Are_One_Face()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var circle = Output(await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 5 }), "circleId");
        var body = await Extrude(session, sketch, 10);

        var faces = await Faces(session, body);

        Assert.Equal(["start", "end", "side"], faces.Select(face => face.Reference.Element));
        var side = Named(faces, body, "side", circle);
        Assert.Equal((1, 0, (DatumAxis?)null), (side.Parts, side.Holes, side.Normal));
        var polygon = 2 * 64 * 5 * Math.Sin(Math.PI / 64) * 10;
        Assert.True(Math.Abs(side.Area - polygon) < 1e-6 * polygon, $"{side.Area} against {polygon}");
    }

    // Items 2 and 4 (the answer of a split face): a translate moves each face and keeps each name. A slot through a plate
    // cuts +z and -z of the plate into two parts each, and the walls of the slot carry the names of the faces of the slot.
    [NativeManifoldFact]
    public async Task A_Cut_Face_Is_One_Name_With_Two_Parts()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 20, 10, 2);
        var slot = await Box(session, 2, 12, 4);
        var moved = Output(await Applied(session, new TranslateCommand { BodyId = slot, Dx = 0, Dy = 0, Dz = 0.5 }), "bodyId");
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = moved }), "bodyId");

        var faces = await Faces(session, cut);

        Assert.Equal((2, 0, 2.0 * 2 * 9 * 10), (Named(faces, plate, "+z").Parts, Named(faces, plate, "+z").Holes, Named(faces, plate, "+z").Area * 2));
        Assert.Equal(2, Named(faces, plate, "-z").Parts);
        Assert.Equal(2, Named(faces, plate, "+y").Parts);
        Assert.Equal((1, 20.0), (Named(faces, slot, "+x").Parts, Named(faces, slot, "+x").Area));
        Assert.Equal(new DatumAxis(-1, 0, 0), Named(faces, slot, "+x").Normal);
        Assert.DoesNotContain(faces, face => face.Reference == new DatumReference(slot, "+z", null));
    }

    // Item 4: a box through a plate gives a ring. The top of the plate has one hole, and its area is the area of the
    // plate less the area of the hole.
    [NativeManifoldFact]
    public async Task A_Face_With_A_Hole_Has_The_Area_Less_The_Hole()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 10, 10, 2);
        var tool = await Box(session, 4, 4, 4);
        var ring = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        var faces = await Faces(session, ring);

        Assert.Equal((1, 1, 84.0), (Named(faces, plate, "+z").Parts, Named(faces, plate, "+z").Holes, Named(faces, plate, "+z").Area));
        Assert.Equal((1, 0, 8.0), (Named(faces, tool, "-y").Parts, Named(faces, tool, "-y").Holes, Named(faces, tool, "-y").Area));
        Assert.Equal(10, faces.Count);
    }

    private static async Task<IReadOnlyList<EdgeEntry>> Edges(DocumentSession session, Guid body)
    {
        var result = await Query(session, body);
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!.Edges;
    }

    // ADR-0077 items 1 and 2 (phase R12b, TASK-0245): a box gives twelve edges, each the pair of two of its faces, the lower
    // name first, with one part, one segment and the length of the side of the box that it runs along.
    [NativeManifoldFact]
    public async Task A_Box_Gives_Twelve_Edges_Named_By_Their_Faces()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 10, 6, 4);

        var edges = await Edges(session, box);

        Assert.Equal(12, edges.Count);
        Assert.All(edges, edge => Assert.Equal((1, 1), (edge.Parts, edge.Segments.Count)));
        Assert.All(edges, edge => Assert.True(string.CompareOrdinal(edge.First.Element, edge.Second.Element) < 0, $"{edge.First.Element} {edge.Second.Element}"));
        var vertical = Assert.Single(edges, edge => (edge.First.Element, edge.Second.Element) == ("+x", "+y"));
        Assert.Equal(4.0, vertical.Length);
        Assert.Equal(10.0, Assert.Single(edges, edge => (edge.First.Element, edge.Second.Element) == ("+y", "+z")).Length);
        Assert.Equal([4.0, 4, 4, 4, 6, 6, 6, 6, 10, 10, 10, 10], edges.Select(edge => edge.Length).Order());
    }

    // ADR-0077 item 1: the 64 segments of the rim of a cylinder are one edge, and the facets of the side meet in no edge.
    [NativeManifoldFact]
    public async Task The_Rim_Of_A_Cylinder_Is_One_Edge_Of_64_Segments()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }), "bodyId");

        var edges = await Edges(session, body);

        Assert.Equal(2, edges.Count);
        Assert.Contains(edges, edge => (edge.First.Element, edge.Second.Element) == ("+z", "side"));
        Assert.Contains(edges, edge => (edge.First.Element, edge.Second.Element) == ("-z", "side"));
        var rim = 2 * 64 * 5 * Math.Sin(Math.PI / 64);
        Assert.All(edges, edge =>
        {
            Assert.Equal((1, 64), (edge.Parts, edge.Segments.Count));
            Assert.True(Math.Abs(edge.Length - rim) < 1e-9 * rim, $"{edge.Length} against {rim}");
        });
    }

    // ADR-0077 item 1: a cut gives the wall of a hole its edges with no new rule: a plate with a round hole has its twelve
    // edges and the two rims of the hole, each between a face of the plate and the side of the tool.
    [NativeManifoldFact]
    public async Task A_Cut_Gives_The_Rims_Of_The_Hole_As_Edges()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 20, 20, 4);
        var tool = Output(await Applied(session, new CreateCylinderCommand { Radius = 3, Height = 10 }), "bodyId");
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        var edges = await Edges(session, cut);

        Assert.Equal(14, edges.Count);
        var rims = edges.Where(edge => edge.First.FeatureId == tool || edge.Second.FeatureId == tool).ToList();
        Assert.Equal(2, rims.Count);
        Assert.All(rims, rim => Assert.Equal((1, 64), (rim.Parts, rim.Segments.Count)));
    }

    // ADR-0075 item 2 (phase R12a, TASK-0236): the outline of each face name, which a client draws as the mark of a
    // selected face. Each face of a box has its four edges, and each edge of the box is in the outlines of two faces.
    [NativeManifoldFact]
    public async Task The_Outline_Of_A_Face_Of_A_Box_Is_Its_Four_Edges()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 10, 6, 4);

        var faces = await Faces(session, box);

        Assert.All(faces, face => Assert.Equal(4, face.Outline.Count));
        var top = Named(faces, box, "+z").Outline;
        Assert.All(top, segment => Assert.Equal((2.0, 2.0), (segment.Start.Z, segment.End.Z)));
        Assert.All(top, segment => Assert.True(Math.Abs(segment.Start.X) == 5 && Math.Abs(segment.Start.Y) == 3, $"{segment}"));
        string Key(FaceSegment segment) => string.Join(" ", new[] { segment.Start, segment.End }.Select(p => $"{p.X},{p.Y},{p.Z}").Order(StringComparer.Ordinal));
        var edges = faces.SelectMany(face => face.Outline).GroupBy(Key).ToList();
        Assert.Equal(12, edges.Count);
        Assert.All(edges, edge => Assert.Equal(2, edge.Count()));
    }

    // ADR-0075 item 2: the outline of a face with a hole holds the edges of the hole too, and the outline of the facets
    // of a circle holds the edges of its two caps and no edge between two facets.
    [NativeManifoldFact]
    public async Task The_Outline_Holds_A_Hole_And_Leaves_Out_The_Edges_Inside_A_Curve()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 10, 10, 2);
        var tool = await Box(session, 4, 4, 4);
        var ring = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var circle = Output(await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 5 }), "circleId");
        var rod = await Extrude(session, sketch, 10);

        var top = Named(await Faces(session, ring), plate, "+z").Outline;
        Assert.Equal(8, top.Count);
        Assert.Equal(4, top.Count(segment => Math.Abs(segment.Start.X) <= 2 && Math.Abs(segment.Start.Y) <= 2));

        var faces = await Faces(session, rod);
        var side = Named(faces, rod, "side", circle).Outline;
        Assert.Equal(Named(faces, rod, "start").Outline.Count + Named(faces, rod, "end").Outline.Count, side.Count);
        Assert.All(side, segment => Assert.True(segment.Start.Z == segment.End.Z, $"An edge between two facets: {segment}"));
    }

    // Item 3: a body that is not live, and a body with a fault, give no faces.
    [NativeManifoldFact]
    public async Task A_Body_That_Is_Not_Live_Or_Has_A_Fault_Gives_No_Faces()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var body = await Extrude(session, sketch.Id, 3);

        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await Query(session, Guid.NewGuid())).Error?.Code);

        // The second corner moves above the third, so two lines cross and the rebuild gives the body a fault.
        await session.Apply(new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[1], X = 5, Y = 12 });
        var record = await session.Read((document, _) => document.Bodies.Single(item => item.Handle.Id == body));
        Assert.NotNull(record.Fault);
        Assert.Equal(DiagnosticCodes.GeomBodyFaulted, (await Query(session, body)).Error?.Code);
    }

    // Item 5: the handler builds a B-Rep one time for each geometry key. A query again gives the same result; an edit of
    // the sketch gives the body a new key, so the next query builds again and gives the new faces.
    [NativeManifoldFact]
    public async Task The_Handler_Keeps_The_Faces_Of_Each_Geometry_Key()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = await NewSketch(session, Rectangle);
        var body = await Extrude(session, sketch.Id, 3);
        var handler = new GetFacesQueryHandler();
        Task<FacesInfo> Run() => session.ReadGeometry((document, geometry) =>
            (FacesInfo)handler.Handle(new GetFacesQuery { BodyId = body }, document, geometry, default).Result.Result!);

        var first = await Run();
        var second = await Run();
        Assert.Same(first, second);
        Assert.Equal(1, handler.BrepsMade);

        await Applied(session, new MoveSketchPointCommand { SketchId = sketch.Id, PointId = sketch.Points[2], X = 15, Y = 8 });
        var third = await Run();
        Assert.Equal(2, handler.BrepsMade);
        Assert.NotEqual(Named(first.Faces, body, "start").Area, Named(third.Faces, body, "start").Area);
    }

    // Items 1 and 2: for boxes, extrudes, circles, translates and subtracts that the commands make, the bounds of the
    // kernel solid agree with the bounds of the solid of Manifold.
    [NativeManifoldFact]
    public async Task The_Kernel_Solid_Of_Each_Body_Has_The_Bounds_Of_Manifold()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 30, 20, 4);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 3, Y = 1 }), "pointId");
        await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 2.5 });
        var pin = await Extrude(session, sketch, -6);
        var lifted = Output(await Applied(session, new TranslateCommand { BodyId = pin, Dx = 0.25, Dy = 0, Dz = 4 }), "bodyId");
        var holed = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = lifted }), "bodyId");
        var rectangle = await NewSketch(session, Rectangle);
        var block = await Extrude(session, rectangle.Id, 7);
        var notched = Output(await Applied(session, new SubtractCommand { MinuendBodyId = holed, SubtrahendBodyId = block }), "bodyId");
        var cube = await Box(session, 2, 2, 2);
        var away = Output(await Applied(session, new TranslateCommand { BodyId = cube, Dx = 7, Dy = -3, Dz = 11 }), "bodyId");

        var bodies = await session.Read((document, _) => document.Bodies.Select(item => item.Handle.Id).ToList());
        Assert.Equal(new[] { notched, away }.Order(), bodies.Order());
        foreach (var body in bodies)
        {
            var (solid, reason) = await session.Read((document, _) => KernelProjection.Of(document, body));
            Assert.True(solid is not null, reason);
            var points = solid!.Polygons.SelectMany(polygon => polygon.Vertices).Select(p => p.ToDoubles()).ToList();
            var box = (await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body })).Result;
            double[] kernel = [points.Min(p => p.X), points.Min(p => p.Y), points.Min(p => p.Z), points.Max(p => p.X), points.Max(p => p.Y), points.Max(p => p.Z)];
            double[] manifold = [box.MinX, box.MinY, box.MinZ, box.MaxX, box.MaxY, box.MaxZ];
            for (var i = 0; i < 6; i++)
                Assert.True(Math.Abs(kernel[i] - manifold[i]) < 1e-9, $"Body {body}, bound {i}: {kernel[i]} against {manifold[i]}.");
        }
    }
}
