using System.Buffers.Binary;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry;

namespace Engine.Tests.Queries;

// Row K15a (TASK-0432): the measure queries GetMassProperties, GetDistance and GetAngle for bodies of plane faces, and
// the volume of the facets of a curved body. ADR-0180 names this file in enforced-by. Each expected value comes from a
// computation by hand, not from the code under test.
public sealed class MeasureQueryTests
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

    private static async Task<T> Answer<T>(DocumentSession session, Query query)
    {
        var result = await session.Query<T>(query);
        Assert.True(result.Error is null, $"{query.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static async Task<string?> Refusal<T>(DocumentSession session, Query query)
        => (await session.Query<T>(query)).Error?.Code;

    private static Task<MassProperties> Mass(DocumentSession session, Guid body)
        => Answer<MassProperties>(session, new GetMassPropertiesQuery { BodyId = body });

    private static GetAngleQuery Angle(Guid bodyA, Guid featureA, string elementA, Guid bodyB, Guid featureB, string elementB, Guid? itemA = null, Guid? itemB = null)
        => new() { BodyIdA = bodyA, FeatureIdA = featureA, ElementA = elementA, ItemIdA = itemA, BodyIdB = bodyB, FeatureIdB = featureB, ElementB = elementB, ItemIdB = itemB };

    // The feature of the faces of a body, as GetFaces gives it: the CreateBox under each Translate.
    private static async Task<Guid> Feature(DocumentSession session, Guid body)
        => (await Answer<FacesInfo>(session, new GetFacesQuery { BodyId = body })).Faces[0].Reference.FeatureId;

    // Items 3 and 8: a box gives its exact volume and its centre, the origin, and no flag of facets. The sizes 3, 7 and 11
    // give half sizes that are not integers.
    [Fact]
    public async Task The_Mass_Of_A_Box_Is_Its_Exact_Volume_At_Its_Centre()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 10, 20, 40);
        var odd = await Box(session, 3, 7, 11);

        Assert.Equal(new MassProperties(8000, new DatumPoint(0, 0, 0), false), await Mass(session, box));
        Assert.Equal(new MassProperties(231, new DatumPoint(0, 0, 0), false), await Mass(session, odd));
    }

    // Item 7: each query refuses a body that is not live, and a mesh body of an STL file, which the owned kernel does not
    // hold.
    [Fact]
    public async Task A_Body_That_Is_Not_Live_Or_A_Mesh_Body_Is_Refused()
    {
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend(), assets: assets));
        var box = await Box(session, 1, 1, 1);
        var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Triangle()) }));

        foreach (var (body, code) in new[] { (Guid.NewGuid(), DiagnosticCodes.GeomBodyNotFound), (mesh, DiagnosticCodes.MeshUnsupported) })
        {
            Assert.Equal(code, await Refusal<MassProperties>(session, new GetMassPropertiesQuery { BodyId = body }));
            Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = box, BodyIdB = body }));
            Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = body, BodyIdB = box }));
            Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(box, box, "+x", body, body, "+x")));
            Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(body, body, "+x", box, box, "+x")));
        }
    }

    // Items 5 and 8: the angle between the outward normals of two faces of a box, as a cosine and a sine.
    [Fact]
    public async Task The_Angle_Between_Two_Faces_Of_A_Box_Is_A_Cosine_And_A_Sine()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 10, 6, 4);

        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(box, box, "+x", box, box, "+x")));
        Assert.Equal(new AngleInfo(-1, 0), await Answer<AngleInfo>(session, Angle(box, box, "+x", box, box, "-x")));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(box, box, "+x", box, box, "+y")));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(box, box, "+z", box, box, "-y")));
    }

    // Items 2 and 7: a face or an edge that the body does not have, and a part of a name, give E-MEASURE-NAME-NOT-FOUND.
    [Fact]
    public async Task A_Name_That_The_Body_Does_Not_Have_Is_Refused()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 10, 10, 10);
        var code = DiagnosticCodes.MeasureNameNotFound;

        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(box, box, "+w", box, box, "+x")));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(box, box, "+x", box, Guid.NewGuid(), "+x")));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(box, box, "+x", box, box, "+x", itemB: Guid.NewGuid())));
        GetDistanceQuery[] refused =
        [
            new() { BodyIdA = box, FeatureIdA = box, BodyIdB = box },
            new() { BodyIdA = box, ElementA = "+x", BodyIdB = box },
            new() { BodyIdA = box, BodyIdB = box, ItemIdB = Guid.NewGuid() },
            new() { BodyIdA = box, BodyIdB = box, SecondFeatureIdB = box, SecondElementB = "+x" },
            new() { BodyIdA = box, BodyIdB = box, FeatureIdB = box, ElementB = "+x", SecondFeatureIdB = box },
            new() { BodyIdA = box, BodyIdB = box, FeatureIdB = box, ElementB = "+x", SecondFeatureIdB = box, SecondElementB = "-x" },
            new() { BodyIdA = box, BodyIdB = box, FeatureIdB = box, ElementB = "+x", SecondFeatureIdB = box, SecondElementB = "+x" },
            new() { BodyIdA = box, BodyIdB = box, FeatureIdB = box, ElementB = "+x", SecondFeatureIdB = box, SecondElementB = "+w" },
        ];
        foreach (var query in refused)
            Assert.Equal(code, await Refusal<DistanceInfo>(session, query));
    }

    // Item 4: the distance between two faces, two edges, a face and an edge, and a body and itself, on one box of 10 mm.
    // An edge is the pair of the names of its faces in either order.
    [Fact]
    public async Task The_Distance_Between_Faces_And_Edges_Of_One_Box()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = await Box(session, 10, 10, 10);

        var faces = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = box, FeatureIdA = box, ElementA = "+x", BodyIdB = box, FeatureIdB = box, ElementB = "-x" });
        Assert.Equal(10, faces.Distance);
        Assert.Equal((5.0, -5.0), (faces.PointA.X, faces.PointB.X));
        Assert.Equal((faces.PointA.Y, faces.PointA.Z), (faces.PointB.Y, faces.PointB.Z));

        var edges = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = box, FeatureIdA = box, ElementA = "+x", SecondFeatureIdA = box, SecondElementA = "+y",
            BodyIdB = box, FeatureIdB = box, ElementB = "-y", SecondFeatureIdB = box, SecondElementB = "-x",
        });
        Assert.Equal(Math.Sqrt(200), edges.Distance);
        Assert.Equal((5.0, 5.0, -5.0, -5.0), (edges.PointA.X, edges.PointA.Y, edges.PointB.X, edges.PointB.Y));
        Assert.Equal(edges.PointA.Z, edges.PointB.Z);

        var faceToEdge = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = box, FeatureIdA = box, ElementA = "+z",
            BodyIdB = box, FeatureIdB = box, ElementB = "-z", SecondFeatureIdB = box, SecondElementB = "-x",
        });
        Assert.Equal(10, faceToEdge.Distance);
        Assert.Equal((5.0, -5.0, -5.0), (faceToEdge.PointA.Z, faceToEdge.PointB.Z, faceToEdge.PointB.X));

        var onFace = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = box, FeatureIdA = box, ElementA = "+x",
            BodyIdB = box, FeatureIdB = box, ElementB = "+y", SecondFeatureIdB = box, SecondElementB = "+x",
        });
        Assert.Equal(0, onFace.Distance);
        Assert.Equal(onFace.PointA, onFace.PointB);

        Assert.Equal(0, (await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = box, BodyIdB = box })).Distance);
    }

    // Item 4: the exact steps between pieces. A segment through a triangle meets it at its crossing point; two skew
    // segments are at the exact distance of their common normal; a point above a polygon has its foot inside it.
    [Fact]
    public void The_Nearest_Points_Of_Two_Pieces_Are_Exact()
    {
        static Vector3R P(long x, long y, long z) => new(x, y, z);
        var triangle = new NearestPoints.Piece([P(0, 0, 0), P(4, 0, 0), P(0, 4, 0)]);
        var through = new NearestPoints.Piece([P(1, 1, -3), P(1, 1, 5)]);
        var crossing = NearestPoints.Between(through, triangle);
        Assert.Equal((Rational.Zero, P(1, 1, 0), P(1, 1, 0)), (crossing.Squared, crossing.A, crossing.B));

        var skew = NearestPoints.Between(new NearestPoints.Piece([P(0, 0, 0), P(4, 0, 0)]), new NearestPoints.Piece([P(2, -3, 7), P(2, 3, 7)]));
        Assert.Equal(((Rational)49, P(2, 0, 0), P(2, 0, 7)), (skew.Squared, skew.A, skew.B));

        // Two parallel segments that do not overlap: the nearest pair is an end of each, on either side.
        var base0 = new NearestPoints.Piece([P(0, 0, 0), P(2, 0, 0)]);
        var right = NearestPoints.Between(base0, new NearestPoints.Piece([P(5, 1, 0), P(9, 1, 0)]));
        Assert.Equal(((Rational)10, P(2, 0, 0), P(5, 1, 0)), (right.Squared, right.A, right.B));
        var left = NearestPoints.Between(base0, new NearestPoints.Piece([P(-9, 1, 0), P(-5, 1, 0)]));
        Assert.Equal(((Rational)26, P(0, 0, 0), P(-5, 1, 0)), (left.Squared, left.A, left.B));

        var above = new NearestPoints.Piece([P(1, 1, 3), P(1, 2, 3)]);
        var foot = NearestPoints.Between(triangle, above);
        Assert.Equal(((Rational)9, P(1, 1, 0), P(1, 1, 3)), (foot.Squared, foot.A, foot.B));

        // Two pieces at the same distance: the nearest pair of a set is the first in the order of the pieces.
        var beside = new NearestPoints.Piece([P(2, 1, 3), P(2, 0, 3)]);
        var far = new NearestPoints.Piece([P(100, 100, 100), P(101, 100, 100)]);
        Assert.Equal(foot, NearestPoints.Nearest([triangle], [far, above, beside]));
        Assert.Equal(P(2, 1, 3), NearestPoints.Nearest([triangle], [beside, above, far])!.Value.B);
        Assert.Equal(Rational.Zero, NearestPoints.Nearest([triangle], [far, above, through])!.Value.Squared);
    }

    // Item 4: two boxes apart on one axis and on two axes, touching, overlapping, and one inside the other. A body is a
    // solid, so a box inside another is at the distance 0, with its first vertex as the common point.
    [NativeManifoldFact]
    public async Task Two_Boxes_Apart_Touching_Overlapping_And_One_Inside_The_Other()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var a = await Box(session, 10, 10, 10);
        async Task<DistanceInfo> To(Guid other) => await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = a, BodyIdB = other });

        var apart = await To(await Moved(session, await Box(session, 10, 10, 10), 30, 0, 0));
        Assert.Equal(20, apart.Distance);
        Assert.Equal((5.0, 25.0), (apart.PointA.X, apart.PointB.X));
        Assert.Equal((apart.PointA.Y, apart.PointA.Z), (apart.PointB.Y, apart.PointB.Z));

        var corner = await To(await Moved(session, await Box(session, 10, 10, 10), 30, 40, 0));
        Assert.Equal(Math.Sqrt(1300), corner.Distance);
        Assert.Equal((5.0, 5.0, 25.0, 35.0), (corner.PointA.X, corner.PointA.Y, corner.PointB.X, corner.PointB.Y));

        Assert.Equal(0, (await To(await Moved(session, await Box(session, 10, 10, 10), 10, 0, 0))).Distance);
        Assert.Equal(0, (await To(await Moved(session, await Box(session, 10, 10, 10), 5, 3, 0))).Distance);

        var small = await Box(session, 2, 2, 2);
        var inside = await To(small);
        Assert.Equal(0, inside.Distance);
        Assert.Equal(inside.PointA, inside.PointB);
        Assert.True(Math.Abs(inside.PointA.X) == 1 && Math.Abs(inside.PointA.Y) == 1 && Math.Abs(inside.PointA.Z) == 1, $"{inside.PointA}");
        Assert.Equal(0, (await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = small, BodyIdB = a })).Distance);

        // A face inside the other body is at the distance 0 too, and a face of the small box to a face of the large box is
        // the gap between them.
        Assert.Equal(0, (await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = a, BodyIdB = small, FeatureIdB = small, ElementB = "+x" })).Distance);
        Assert.Equal(4, (await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = a, FeatureIdA = a, ElementA = "+x", BodyIdB = small, FeatureIdB = small, ElementB = "+x" })).Distance);
    }

    // Items 3 and 8: a plate of 100 by 60 by 10 with a pocket of 20 by 20 by 3 at (30, 10, 3.5) gives 60000 - 1200 = 58800
    // mm³ and the centre -1200 (30, 10, 3.5) / 58800 = (-30/49, -10/49, -1/14). A box turned a quarter about z gives the
    // same volume, and its centre (10, 0, 0) turns to (0, 10, 0).
    [NativeManifoldFact]
    public async Task The_Mass_Of_A_Plate_With_A_Pocket_And_Of_A_Turned_Body()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 100, 60, 10);
        var tool = await Moved(session, await Box(session, 20, 20, 4), 30, 10, 4);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }));

        Assert.Equal(new MassProperties(58800, new DatumPoint(-30.0 / 49, -10.0 / 49, -1.0 / 14), false), await Mass(session, cut));

        var moved = await Moved(session, await Box(session, 10, 20, 40), 10, 0, 0);
        var turned = Output(await Applied(session, new RotateCommand { BodyId = moved, Qw = 1, Qx = 0, Qy = 0, Qz = 1 }));
        Assert.Equal(new MassProperties(8000, new DatumPoint(0, 10, 0), false), await Mass(session, turned));
    }

    // Items 3 and 5: an extrude by 4 of the profile (0, 0), (10, 0), (10, 5), (5, 10), (0, 10) has a chamfer at 45 degrees.
    // The area is 100 - 12.5 = 87.5, so the volume is 350; the centre of the profile is (100 (5, 5) - 12.5 (25/3, 25/3)) /
    // 87.5 = (95/21, 95/21), and the centre of the body is at the height 2. The chamfer and the side x = 10 make 45 degrees:
    // the cosine and the sine are each the root of 1/2.
    [NativeManifoldFact]
    public async Task The_Chamfer_Of_An_Extrude_Is_At_45_Degrees()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 5), (5, 10), (0, 10)];
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        var lines = new Guid[corners.Length];
        for (var i = 0; i < lines.Length; i++)
            lines[i] = Output(await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] }), "lineId");
        var body = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 4 }));
        var feature = await Feature(session, body);

        Assert.Equal(new MassProperties(350, new DatumPoint(95.0 / 21, 95.0 / 21, 2), false), await Mass(session, body));
        var half = Math.Sqrt(0.5);
        Assert.Equal(new AngleInfo(half, half), await Answer<AngleInfo>(session, Angle(body, feature, "side", body, feature, "side", lines[1], lines[2])));
        Assert.Equal(new AngleInfo(-half, half), await Answer<AngleInfo>(session, Angle(body, feature, "side", body, feature, "side", lines[2], lines[4])));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(body, feature, "side", body, feature, "end", lines[2])));

        var across = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = body, FeatureIdA = feature, ElementA = "side", ItemIdA = lines[1],
            BodyIdB = body, FeatureIdB = feature, ElementB = "side", ItemIdB = lines[4],
        });
        Assert.Equal((10.0, 10.0, 0.0), (across.Distance, across.PointA.X, across.PointB.X));
    }

    // Items 3 and 6 (answer Q2 A): a cylinder gives the exact volume and centre of its facets, with the flag, and the
    // volume is below that of the curve. GetDistance and GetAngle refuse its curved face, a body with it, and an edge of
    // it; a plane face of the cylinder is measured.
    [NativeManifoldFact]
    public async Task A_Curved_Body_Gives_The_Volume_Of_Its_Facets_And_Is_Refused_Where_A_Plane_Is_Needed()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var box = await Box(session, 4, 4, 4);

        var mass = await Mass(session, cylinder);
        var facets = await session.Read((document, _) => KernelProjection.Of(document, cylinder).Solid!.Volume().ToDouble());
        Assert.True(mass.Facets);
        Assert.Equal(facets, mass.Volume);
        Assert.True(mass.Volume < Math.PI * 250 && mass.Volume > 0.98 * Math.PI * 250, $"{mass.Volume}");
        Assert.True(Math.Abs(mass.Centre.X) < 1e-9 && Math.Abs(mass.Centre.Y) < 1e-9 && Math.Abs(mass.Centre.Z) < 1e-9, $"{mass.Centre}");

        var code = DiagnosticCodes.MeasureCurved;
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = cylinder, BodyIdB = box }));
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = box, BodyIdB = cylinder, FeatureIdB = cylinder, ElementB = "side" }));
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = box, BodyIdB = cylinder, FeatureIdB = cylinder, ElementB = "+z", SecondFeatureIdB = cylinder, SecondElementB = "side",
        }));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(cylinder, cylinder, "side", box, box, "+z")));

        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(cylinder, cylinder, "+z", box, box, "+z")));
        var top = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = cylinder, FeatureIdA = cylinder, ElementA = "+z", BodyIdB = box, FeatureIdB = box, ElementB = "+z" });
        Assert.Equal((3.0, 5.0, 2.0), (top.Distance, top.PointA.Z, top.PointB.Z));
    }

    // Row K15a on the bookcase of K10a: the gap between the two shelves, the gap between the outer modules, two modules
    // that touch, the inner width of the open case between its sides, the volume and the centre of the back panel, and the
    // angle between a side and the back. Each value comes from the table of BookcaseCase.
    [NativeManifoldFact]
    public async Task The_Bookcase_Gives_Its_Gaps_Its_Back_Panel_And_A_Right_Angle()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var bodies = await BookcaseCase.Build(session);
        Task<DistanceInfo> Between(string a, string b) => Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = bodies[a], BodyIdB = bodies[b] });

        // "Bas" ends at z 618 and "Haut" starts at z 1382.
        var shelves = await Between("Bas", "Haut");
        Assert.Equal((764.0, 618.0, 1382.0), (shelves.Distance, shelves.PointA.Z, shelves.PointB.Z));
        Assert.Equal(1800, (await Between("Module 1 - volume indicatif", "Module 2 - volume indicatif")).Distance);
        Assert.Equal(0, (await Between("Module 6 - volume indicatif", "Module 9 - volume indicatif")).Distance);

        var (left, right, back) = (bodies["Cote gauche"], bodies["Cote droit"], bodies["Fond"]);
        var (leftFeature, rightFeature, backFeature) = (await Feature(session, left), await Feature(session, right), await Feature(session, back));
        var width = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = left, FeatureIdA = leftFeature, ElementA = "+x", BodyIdB = right, FeatureIdB = rightFeature, ElementB = "-x" });
        Assert.Equal((1164.0, -282.0, 882.0), (width.Distance, width.PointA.X, width.PointB.X));

        Assert.Equal(new MassProperties(1164 * 6 * 800, new DatumPoint(300, 197, 1000), false), await Mass(session, back));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(left, leftFeature, "+x", back, backFeature, "-y")));
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
