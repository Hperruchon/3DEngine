using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Commands;

// The cylinder is a primitive of plane facets (ADR-0076, phase K5a, TASK-0238). ADR-0076 names this file in
// enforced-by. The refusals, the record of the inputs, the three face names, the polygon of ADR-0040, a datum on the
// top and a refusal on the side, and a cut of a plate by a cylinder.
public sealed class CreateCylinderCommandTests
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

    private static async Task<Guid> Cylinder(DocumentSession session, double radius, double height)
        => Output(await Applied(session, new CreateCylinderCommand { Radius = radius, Height = height }), "bodyId");

    private static async Task<IReadOnlyList<FaceEntry>> Faces(DocumentSession session, Guid body)
    {
        var result = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!.Faces;
    }

    private static FaceEntry Named(IReadOnlyList<FaceEntry> faces, Guid feature, string element)
        => Assert.Single(faces, face => face.Reference == new DatumReference(feature, element, null));

    // Item 2: a radius or a height that is not finite and greater than zero gives E-GEOM-INVALID-PARAM, before the check
    // of the capability.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -2)]
    [InlineData(double.NaN, 1)]
    [InlineData(1, double.PositiveInfinity)]
    public async Task A_Size_That_Is_Not_Positive_And_Finite_Is_Refused(double radius, double height)
    {
        var session = NewSession(new InProcessMeshBackend());

        var result = await session.Apply(new CreateCylinderCommand { Radius = radius, Height = height });

        Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.GeomInvalidParam), (result.Status, result.Error?.Code));
    }

    // Item 2: a backend with no IExtrudeOps gives E-GEOM-CAP-MISSING.
    [Fact]
    public async Task A_Backend_With_No_Extrude_Gives_The_Missing_Capability()
    {
        var session = NewSession(new InProcessMeshBackend());

        var result = await session.Apply(new CreateCylinderCommand { Radius = 2, Height = 3 });

        Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.GeomCapMissing), (result.Status, result.Error?.Code));
    }

    // Item 1: the record holds the inputs only, and the catalog gives the command to each host.
    [Fact]
    public void The_Record_Holds_The_Radius_And_The_Height_Only()
    {
        var handler = new CreateCylinderCommandHandler();
        var command = new CreateCylinderCommand { Radius = 2.5, Height = 7 };

        var encoded = handler.Encode(command);

        Assert.Equal(["radius", "height"], encoded.Keys);
        Assert.Equal((2.5, 7.0), ((double)encoded["radius"]!, (double)encoded["height"]!));
        Assert.Contains(HandlerCatalog.CommandHandlers(), item => item.CommandName == "CreateCylinder");
    }

    // Item 2: the loop is the circle of ADR-0040 item 2, the circle of a sketch with the centre at the origin: 64
    // vertices, the first at (radius, 0), counter-clockwise.
    [Fact]
    public void The_Circle_Is_The_Circle_Of_A_Sketch()
    {
        var circle = CreateCylinderCommandHandler.Circle(5);

        Assert.Equal(64, circle.Count);
        Assert.Equal((5.0, 0.0), circle[0]);
        Assert.Equal(Engine.Core.Sketch.SketchProfile.Turn((0, 0), (5, 0), null), circle);
        Assert.True(circle[1].Y > 0, $"{circle[1]}");
    }

    // Items 2 and 3: the body has the three names +z, -z and side with no item. The top and the bottom are planes at
    // ±height / 2 with the area of the polygon; the side is one part with no normal.
    [NativeManifoldFact]
    public async Task A_Cylinder_Gives_Three_Named_Faces()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = await Cylinder(session, 5, 10);

        var faces = await Faces(session, body);

        Assert.Equal(["+z", "-z", "side"], faces.Select(face => face.Reference.Element).Order(StringComparer.Ordinal));
        Assert.All(faces, face => Assert.Equal((body, (Guid?)null, 1, 0), (face.Reference.FeatureId, face.Reference.ItemId, face.Parts, face.Holes)));
        var (top, bottom, side) = (Named(faces, body, "+z"), Named(faces, body, "-z"), Named(faces, body, "side"));
        Assert.Equal((new DatumAxis(0, 0, 1), new DatumAxis(0, 0, -1), (DatumAxis?)null), (top.Normal, bottom.Normal, side.Normal));
        Assert.Equal((5.0, -5.0), (top.Point.Z, bottom.Point.Z));

        var polygon = 32 * 25 * Math.Sin(Math.PI / 32);
        Assert.True(Math.Abs(top.Area - polygon) < 1e-9 * polygon, $"{top.Area} against {polygon}");
        var wall = 2 * 64 * 5 * Math.Sin(Math.PI / 64) * 10;
        Assert.True(Math.Abs(side.Area - wall) < 1e-6 * wall, $"{side.Area} against {wall}");

        // The mesh of Manifold has the bounds of the circle: the first vertex is (5, 0) exactly, and the vertex across from
        // it comes from 32 turns, so it is near (-5, 0).
        var bounds = backend.TryGet<IGeometryQuery>()!.GetBoundingBox(new BodyHandle(body));
        Assert.Equal((5.0, -5.0, 5.0), (bounds.MaxX, bounds.MinZ, bounds.MaxZ));
        Assert.True(Math.Abs(bounds.MinX + 5) < 1e-12, $"{bounds.MinX}");
    }

    // Item 4: a datum on +z of a moved cylinder has the frame of the top, moved; the side has no plane.
    [NativeManifoldFact]
    public async Task A_Datum_Takes_The_Top_And_Refuses_The_Side()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = await Cylinder(session, 2, 6);
        await Applied(session, new TranslateCommand { BodyId = body, Dx = 1, Dy = 2, Dz = 3 });

        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = body, Element = "+z" }), "datumId");
        var frame = await session.Read((document, _) => document.Datums.Single(item => item.Id == datum).Frame);
        Assert.Equal(new DatumFrame(new DatumPoint(1, 2, 6), new DatumAxis(1, 0, 0), new DatumAxis(0, 1, 0), new DatumAxis(0, 0, 1)), frame);

        var side = await session.Apply(new CreateDatumCommand { FeatureId = body, Element = "side" });
        Assert.Equal(CommandStatus.Rejected, side.Status);
        Assert.Contains("curved", side.Error!.Message, StringComparison.Ordinal);
    }

    // Item 3 with ADR-0057: a cylinder through a plate cuts a hole of 64 walls with the name side into +z and -z of the
    // plate, and the kernel volume is the plate less the prism.
    [NativeManifoldFact]
    public async Task A_Cylinder_Cuts_A_Hole_Of_One_Name()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 }), "bodyId");
        var tool = await Cylinder(session, 3, 10);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        var faces = await Faces(session, cut);

        Assert.Equal((1, 1), (Named(faces, plate, "+z").Holes, Named(faces, plate, "-z").Holes));
        Assert.Equal((1, (DatumAxis?)null), (Named(faces, tool, "side").Parts, Named(faces, tool, "side").Normal));
        Assert.DoesNotContain(faces, face => face.Reference.FeatureId == tool && face.Reference.Element != "side");
        var area = 400 - (32 * 9 * Math.Sin(Math.PI / 32));
        Assert.True(Math.Abs(Named(faces, plate, "+z").Area - area) < 1e-9 * area, $"{Named(faces, plate, "+z").Area} against {area}");
    }

    // Item 6: the regeneration passes a cylinder as it passes a box. A plate extruded from a sketch and cut by a cylinder
    // takes a new distance, and the result keeps its hole at the new height.
    [NativeManifoldFact]
    public async Task An_Edit_Upstream_Of_A_Cut_By_A_Cylinder_Rebuilds_The_Hole()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(-10, -10), (10, -10), (10, 10), (-10, 10)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        var plate = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 4 }), "bodyId");
        var tool = await Cylinder(session, 3, 20);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = plate, Distance = 6 });

        var faces = await Faces(session, cut);
        var top = Named(faces, plate, "end");
        Assert.Equal((6.0, 1), (top.Point.Z, top.Holes));
        Assert.Equal(1, Named(faces, tool, "side").Parts);
    }
}
