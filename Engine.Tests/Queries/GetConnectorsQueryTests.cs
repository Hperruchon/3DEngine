using System.Buffers.Binary;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry;

namespace Engine.Tests.Queries;

// Row K17 (TASK-0443): the query GetConnectors gives the named datums of a body and the implicit frames of its faces
// (ADR-0157 item 4). ADR-0183 names this file in enforced-by. Each expected value comes from a computation by hand, from
// the table of the bookcase, or from the exact turn of the nine doubles of a Rotate, not from the code under test. The
// tests that move, turn or cut a body use the native backend.
public sealed class GetConnectorsQueryTests
{
    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name = "bodyId")
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static async Task<Guid> Box(DocumentSession session, double x, double y, double z)
        => Output(await Applied(session, new CreateBoxCommand { SizeX = x, SizeY = y, SizeZ = z }));

    private static async Task<Guid> Moved(DocumentSession session, Guid body, double dx, double dy, double dz)
        => Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = dx, Dy = dy, Dz = dz }));

    private static async Task<ConnectorsInfo> Connectors(DocumentSession session, Guid body)
    {
        var result = await session.Query<ConnectorsInfo>(new GetConnectorsQuery { BodyId = body });
        Assert.True(result.Error is null, $"GetConnectors: {result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    // The faces of the body as GetFaces names them, in its order.
    private static async Task<IReadOnlyList<DatumReference>> FaceNames(DocumentSession session, Guid body)
    {
        var faces = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
        Assert.True(faces.Error is null, $"GetFaces: {faces.Error?.Code} {faces.Error?.Message}");
        return [.. faces.Result!.Faces.Select(face => face.Reference)];
    }

    private static FaceConnector Face(ConnectorsInfo info, Guid feature, string element, Guid? item = null)
        => Assert.Single(info.Faces, face => face.Reference == new DatumReference(feature, element, item));

    private static FaceConnector Plane(Guid feature, string element, (double X, double Y, double Z) centre, (double X, double Y, double Z) normal)
        => new(new DatumReference(feature, element, null), ConnectorKind.Plane, new DatumPoint(centre.X, centre.Y, centre.Z), new DatumAxis(normal.X, normal.Y, normal.Z), null);

    // A face of the kind Curved has no frame.
    private static void AssertCurved(FaceConnector face)
        => Assert.Equal(new FaceConnector(face.Reference, ConnectorKind.Curved, null, null, null), face);

    // A face of one plane with its centre within 1e-12 mm on each axis, and its normal exactly as given.
    private static void AssertPlane(FaceConnector face, (double X, double Y, double Z) centre, (double X, double Y, double Z) normal)
    {
        Assert.Equal(ConnectorKind.Plane, face.Kind);
        Assert.Null(face.Radius);
        Assert.Equal(new DatumAxis(normal.X, normal.Y, normal.Z), face.Direction);
        var point = face.Point!;
        Assert.True(Math.Abs(point.X - centre.X) <= 1e-12 && Math.Abs(point.Y - centre.Y) <= 1e-12 && Math.Abs(point.Z - centre.Z) <= 1e-12, $"{point} against {centre}");
    }

    // Items 2 and 4: a box of 3 by 7 by 11 at the origin. The centre of each face is the centre of the box moved by half a
    // size along the normal of the face, and each normal is an axis: each value is exact. The faces are in the order of
    // GetFaces, the box has no datum, and the query changes neither the version nor the bodies.
    [Fact]
    public async Task A_Box_Gives_The_Exact_Centre_And_The_Outward_Normal_Of_Each_Face()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 3, 7, 11);
        var version = await session.Read((document, _) => document.Version);

        var info = await Connectors(session, box);

        Assert.Empty(info.Datums);
        Assert.Equal(await FaceNames(session, box), [.. info.Faces.Select(face => face.Reference)]);
        Assert.Equal(Plane(box, "+x", (1.5, 0, 0), (1, 0, 0)), Face(info, box, "+x"));
        Assert.Equal(Plane(box, "-x", (-1.5, 0, 0), (-1, 0, 0)), Face(info, box, "-x"));
        Assert.Equal(Plane(box, "+y", (0, 3.5, 0), (0, 1, 0)), Face(info, box, "+y"));
        Assert.Equal(Plane(box, "-y", (0, -3.5, 0), (0, -1, 0)), Face(info, box, "-y"));
        Assert.Equal(Plane(box, "+z", (0, 0, 5.5), (0, 0, 1)), Face(info, box, "+z"));
        Assert.Equal(Plane(box, "-z", (0, 0, -5.5), (0, 0, -1)), Face(info, box, "-z"));
        Assert.Equal(version, await session.Read((document, _) => document.Version));
        Assert.Single(await session.Read((document, _) => document.Bodies));
    }

    // Items 4 and 6: the box of 3 by 7 by 11 moved by (10, 20, 30), then turned about z by the quaternion (2, 0, 0, 1) of
    // integers: cos = (4 - 1) / 5 and sin = 2 · 2 · 1 / 5, each rounded once to a double. The owned kernel reads the nine
    // doubles exactly, so the centre of each face is the exact turn of the moved centre, computed here with Rational, and
    // rounded once. The normal of +x is the first column of the turn, (0.6, 0.8, 0) as doubles, which is not exactly of the
    // length 1, so its unit is within 1e-15 of (0.6, 0.8, 0); the normal of +z is the axis z exactly.
    [NativeManifoldFact]
    public async Task A_Box_After_A_Translate_And_A_Rotate_Gives_The_Exact_Turn_Of_Each_Centre()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 3, 7, 11);
        var turned = Output(await Applied(session, new RotateCommand { BodyId = await Moved(session, box, 10, 20, 30), Qw = 2, Qx = 0, Qy = 0, Qz = 1 }));
        var turn = Rotation.FromQuaternion(2, 0, 0, 1)!.Value;
        Assert.Equal((0.6, 0.8, -0.8, 0.6, 1.0), (turn.Xx, turn.Xy, turn.Yx, turn.Yy, turn.Zz));

        var info = await Connectors(session, turned);

        Assert.Equal(await FaceNames(session, turned), [.. info.Faces.Select(face => face.Reference)]);
        (string Element, double X, double Y, double Z)[] centres =
            [("+x", 11.5, 20, 30), ("-x", 8.5, 20, 30), ("+y", 10, 23.5, 30), ("-y", 10, 16.5, 30), ("+z", 10, 20, 35.5), ("-z", 10, 20, 24.5)];
        foreach (var (element, x, y, z) in centres)
        {
            var exact = Turned(turn, x, y, z);
            Assert.Equal(new DatumPoint(exact.X, exact.Y, exact.Z), Face(info, box, element).Point);
        }

        var right = Face(info, box, "+x").Direction!;
        Assert.True(Math.Abs(right.X - 0.6) <= 1e-15 && Math.Abs(right.Y - 0.8) <= 1e-15 && right.Z == 0, $"{right}");
        Assert.Equal(new DatumAxis(-right.X, -right.Y, 0), Face(info, box, "-x").Direction);
        Assert.Equal(new DatumAxis(0, 0, 1), Face(info, box, "+z").Direction);
        Assert.Equal(new DatumAxis(0, 0, -1), Face(info, box, "-z").Direction);
    }

    // The point (x, y, z) turned exactly by the nine doubles of the turn: X x + Y y + Z z, each double read exactly.
    private static (double X, double Y, double Z) Turned(Rotation turn, double x, double y, double z)
    {
        static Rational R(double value) => Rational.FromDouble(value);
        var (px, py, pz) = (R(x), R(y), R(z));
        return (((R(turn.Xx) * px) + (R(turn.Yx) * py) + (R(turn.Zx) * pz)).ToDouble(),
            ((R(turn.Xy) * px) + (R(turn.Yy) * py) + (R(turn.Zy) * pz)).ToDouble(),
            ((R(turn.Xz) * px) + (R(turn.Yz) * py) + (R(turn.Zz) * pz)).ToDouble());
    }

    // Item 5: a cylinder of radius 5 and height 10 at the origin: the axis from the centre of its base (0, 0, -5) along z,
    // the radius 5, and its two caps as planes at z = ±5. A cylinder of radius 2 and height 6 turned a quarter about x by
    // the quaternion (1, 1, 0, 0), then moved by (1, 2, 3): the axis z goes to -y, so the base (0, 0, -3) goes to (0, 3, 0)
    // and then to (1, 5, 3); the cap +z is at y = 2 - 3 = -1 with the normal -y.
    [NativeManifoldFact]
    public async Task A_Cylinder_Gives_Its_Axis_And_Its_Radius_And_Its_Two_Caps_As_Planes()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var small = Output(await Applied(session, new CreateCylinderCommand { Radius = 2, Height = 6 }));
        var placed = await Moved(session, Output(await Applied(session, new RotateCommand { BodyId = small, Qw = 1, Qx = 1, Qy = 0, Qz = 0 })), 1, 2, 3);

        var info = await Connectors(session, cylinder);
        Assert.Equal(await FaceNames(session, cylinder), [.. info.Faces.Select(face => face.Reference)]);
        Assert.Equal(new FaceConnector(new DatumReference(cylinder, "side", null), ConnectorKind.Cylinder, new DatumPoint(0, 0, -5), new DatumAxis(0, 0, 1), 5),
            Face(info, cylinder, "side"));
        AssertPlane(Face(info, cylinder, "+z"), (0, 0, 5), (0, 0, 1));
        AssertPlane(Face(info, cylinder, "-z"), (0, 0, -5), (0, 0, -1));

        var moved = await Connectors(session, placed);
        Assert.Equal(new FaceConnector(new DatumReference(small, "side", null), ConnectorKind.Cylinder, new DatumPoint(1, 5, 3), new DatumAxis(0, -1, 0), 2),
            Face(moved, small, "side"));
        AssertPlane(Face(moved, small, "+z"), (1, -1, 3), (0, -1, 0));
        AssertPlane(Face(moved, small, "-z"), (1, 5, 3), (0, 1, 0));
    }

    // Items 4 and 5: an extrude by 5 of a circle of radius 3 about (2, 1) is a cylinder with its axis from (2, 1, 0) along z.
    // Its start is a plane with the normal -z and its end a plane at z = 5. The side of a cone, the surface of a sphere and
    // of a torus, and the side of an arc of a profile are curved faces with no frame; the base of the cone and the sides of
    // the lines of the profile are planes.
    [NativeManifoldFact]
    public async Task An_Extrude_Of_A_Circle_Gives_Its_Axis_And_Each_Other_Curved_Face_Has_No_Frame()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 2, Y = 1 }), "pointId");
        var circle = Output(await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 3 }), "circleId");
        var disc = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }));

        var info = await Connectors(session, disc);
        Assert.Equal(await FaceNames(session, disc), [.. info.Faces.Select(face => face.Reference)]);
        Assert.Equal(new FaceConnector(new DatumReference(disc, "side", circle), ConnectorKind.Cylinder, new DatumPoint(2, 1, 0), new DatumAxis(0, 0, 1), 3),
            Face(info, disc, "side", circle));
        AssertPlane(Face(info, disc, "start"), (2, 1, 0), (0, 0, -1));
        AssertPlane(Face(info, disc, "end"), (2, 1, 5), (0, 0, 1));

        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 2 }));
        var torus = Output(await Applied(session, new CreateTorusCommand { Radius = 10, TubeRadius = 2 }));
        var (arc, arcItem, lines) = await ArcExtrude(session);

        var coneFaces = await Connectors(session, cone);
        AssertCurved(Face(coneFaces, cone, "side"));
        AssertPlane(Face(coneFaces, cone, "-z"), (0, 0, -2), (0, 0, -1));
        AssertCurved(Assert.Single((await Connectors(session, sphere)).Faces));
        AssertCurved(Assert.Single((await Connectors(session, torus)).Faces));
        var arcFaces = await Connectors(session, arc);
        AssertCurved(Face(arcFaces, arc, "side", arcItem));
        Assert.Equal(new FaceConnector(new DatumReference(arc, "side", lines[2]), ConnectorKind.Plane, new DatumPoint(0, 5, 2), new DatumAxis(-1, 0, 0), null),
            Face(arcFaces, arc, "side", lines[2]));
    }

    // The extrude by 4 of the square (0, 0), (10, 0), (10, 10), (0, 10) whose right side is a half circle about (10, 5),
    // from (10, 0) to (10, 10). The body, the item of the arc, and the lines from (0, 0) to (10, 0), from (10, 10) to
    // (0, 10), and from (0, 10) to (0, 0).
    private static async Task<(Guid Body, Guid Arc, Guid[] Lines)> ArcExtrude(DocumentSession session)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        async Task<Guid> Point(double x, double y) => Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId");
        var (p0, p1, p2, p3, centre) = (await Point(0, 0), await Point(10, 0), await Point(10, 10), await Point(0, 10), await Point(10, 5));
        var lines = new List<Guid>();
        foreach (var (start, end) in new[] { (p0, p1), (p2, p3), (p3, p0) })
            lines.Add(Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = start, EndPointId = end }), "lineId"));
        var arc = Output(await Applied(session, new AddSketchArcCommand { SketchId = sketch, CenterPointId = centre, StartPointId = p1, EndPointId = p2 }), "arcId");
        return (Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 4 })), arc, [.. lines]);
    }

    // Item 3: the datums of a body are the named datums whose reference is on a feature with the body at the end of its
    // chain of consumers, in the order of the log, each with the frame of its record. A datum with no name, and a datum of
    // another body, are not connectors of the body. After a Translate of the body the datum is still a connector of the
    // new body, and its frame stays where the datum resolved it (ADR-0051 item 2), while the face moves.
    [NativeManifoldFact]
    public async Task Only_The_Named_Datums_Of_The_Body_Are_Its_Datum_Connectors()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var box = await Box(session, 10, 10, 10);
        var other = await Box(session, 4, 4, 4);
        var top = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+z", DatumName = "top" }), "datumId");
        await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "+x" });
        var seat = Output(await Applied(session, new CreateDatumCommand { FeatureId = other, Element = "+z", DatumName = "seat" }), "datumId");
        var side = Output(await Applied(session, new CreateDatumCommand { FeatureId = box, Element = "-y", DatumName = "side" }), "datumId");
        var records = await session.Read((document, _) => document.Datums.ToDictionary(datum => datum.Id));

        var info = await Connectors(session, box);
        Assert.Equal(
            [new DatumConnector(top, "top", new DatumReference(box, "+z", null), records[top].Frame, null),
             new DatumConnector(side, "side", new DatumReference(box, "-y", null), records[side].Frame, null)],
            info.Datums);
        Assert.Equal(new DatumPoint(0, 0, 5), info.Datums[0].Frame.Origin);
        Assert.Equal(new DatumAxis(0, 0, 1), info.Datums[0].Frame.Normal);
        Assert.Equal([new DatumConnector(seat, "seat", new DatumReference(other, "+z", null), records[seat].Frame, null)], (await Connectors(session, other)).Datums);

        var raised = await Moved(session, box, 0, 0, 10);
        var moved = await Connectors(session, raised);
        Assert.Equal([top, side], moved.Datums.Select(datum => datum.DatumId));
        Assert.Equal(new DatumPoint(0, 0, 5), moved.Datums[0].Frame.Origin);
        Assert.Equal(new DatumPoint(0, 0, 15), Face(moved, box, "+z").Point);
    }

    // Item 4: a plate of 100 by 60 by 10 less a box of 20 at (30, 10, 0) has a square hole. The top of the plate is the area
    // 6000 - 400 = 5600 with the centre ((0 · 6000) - (30 · 400)) / 5600 = -15/7 on x and -(10 · 400) / 5600 = -5/7 on y, at
    // z = 5. The face -x of the box is the wall x = 20 of the hole, with the outward normal +x into the hole, and its face +x
    // the wall x = 40 with the normal -x. A plate less a cylinder has the side of the hole as a curved face with no frame:
    // the body has a Subtract after the cylinder (item 5).
    [NativeManifoldFact]
    public async Task A_Plane_Face_With_A_Hole_Gives_The_Centre_Of_Its_Area_With_The_Hole()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 100, 60, 10);
        var tool = await Box(session, 20, 20, 20);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = await Moved(session, tool, 30, 10, 0) }));

        var info = await Connectors(session, cut);
        Assert.Equal(await FaceNames(session, cut), [.. info.Faces.Select(face => face.Reference)]);
        Assert.Equal(Plane(plate, "+z", (-15.0 / 7, -5.0 / 7, 5), (0, 0, 1)), Face(info, plate, "+z"));
        Assert.Equal(Plane(plate, "-z", (-15.0 / 7, -5.0 / 7, -5), (0, 0, -1)), Face(info, plate, "-z"));
        Assert.Equal(Plane(tool, "-x", (20, 10, 0), (1, 0, 0)), Face(info, tool, "-x"));
        Assert.Equal(Plane(tool, "+x", (40, 10, 0), (-1, 0, 0)), Face(info, tool, "+x"));
        Assert.Equal(Plane(plate, "+x", (50, 0, 0), (1, 0, 0)), Face(info, plate, "+x"));
        Assert.DoesNotContain(info.Faces, face => face.Reference == new DatumReference(tool, "+z", null));

        var drilled = await Box(session, 20, 20, 10);
        var drill = Output(await Applied(session, new CreateCylinderCommand { Radius = 3, Height = 20 }));
        var hole = await Connectors(session, Output(await Applied(session, new SubtractCommand { MinuendBodyId = drilled, SubtrahendBodyId = drill })));
        AssertCurved(Face(hole, drill, "side"));
        Assert.Equal(new DatumAxis(0, 0, 1), Face(hole, drilled, "+z").Direction);
        Assert.Equal(5, Face(hole, drilled, "+z").Point!.Z);
    }

    // Item 4 on the bookcase of K10a: "Module 1" is a box of 800 by 400 by 1000 with its centre at (-1300, 0, 500), so its
    // six frames are the centre moved by half a size along each axis, from the table of BookcaseCase.
    [NativeManifoldFact]
    public async Task The_Bookcase_Gives_The_Six_Frames_Of_A_Module()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var module = (await BookcaseCase.Build(session))["Module 1 - volume indicatif"];
        var info = await Connectors(session, module);
        var feature = (await FaceNames(session, module))[0].FeatureId;

        Assert.Empty(info.Datums);
        Assert.Equal(6, info.Faces.Count);
        Assert.Equal(Plane(feature, "+x", (-900, 0, 500), (1, 0, 0)), Face(info, feature, "+x"));
        Assert.Equal(Plane(feature, "-x", (-1700, 0, 500), (-1, 0, 0)), Face(info, feature, "-x"));
        Assert.Equal(Plane(feature, "+y", (-1300, 200, 500), (0, 1, 0)), Face(info, feature, "+y"));
        Assert.Equal(Plane(feature, "-y", (-1300, -200, 500), (0, -1, 0)), Face(info, feature, "-y"));
        Assert.Equal(Plane(feature, "+z", (-1300, 0, 1000), (0, 0, 1)), Face(info, feature, "+z"));
        Assert.Equal(Plane(feature, "-z", (-1300, 0, 0), (0, 0, -1)), Face(info, feature, "-z"));
    }

    // Item 7: a body that is not live, a mesh body of an STL file, and a body with a fault are refused with the codes of the
    // measure queries.
    [NativeManifoldFact]
    public async Task A_Body_That_Is_Not_Live_A_Mesh_Body_Or_A_Body_With_A_Fault_Is_Refused()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets));
        var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Triangle()) }));

        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(1, 2), (11, 2), (11, 8), (1, 8)];
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < points.Length; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] });
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 3 }));
        Assert.Equal(6, (await Connectors(session, body)).Faces.Count);

        // The second corner moves above the third, so two lines cross and the rebuild gives the body a fault.
        await session.Apply(new MoveSketchPointCommand { SketchId = sketch, PointId = points[1], X = 5, Y = 12 });
        Assert.NotNull(await session.Read((document, _) => document.Bodies.Single(item => item.Handle.Id == body).Fault));

        foreach (var (id, code) in new[] { (Guid.NewGuid(), DiagnosticCodes.GeomBodyNotFound), (mesh, DiagnosticCodes.MeshUnsupported), (body, DiagnosticCodes.GeomBodyFaulted) })
            Assert.Equal(code, (await session.Query<ConnectorsInfo>(new GetConnectorsQuery { BodyId = id })).Error?.Code);
    }

    // A binary STL file of one triangle.
    private static byte[] Triangle()
    {
        float[] triangle = [0f, 0f, 0f, 10f, 0f, 0f, 0f, 10f, 0f];
        var bytes = new byte[84 + 50];
        Encoding.ASCII.GetBytes("a test file").CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), 1);
        for (var i = 0; i < 9; i++)
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(84 + 12 + (i * 4)), triangle[i]);
        return bytes;
    }
}
