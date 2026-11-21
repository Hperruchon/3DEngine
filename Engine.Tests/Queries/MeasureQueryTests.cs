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

    // Items 3, 6 and 10 to 12 (answer Q2 A; phases K15b and K15c, TASK-0434 and TASK-0444): a notch, a Subtract whose drill
    // crosses the face +x of the plate, has no closed form, so it gives the exact volume and centre of its facets, with the
    // flag; the volume is above that of the curve, because the facets of the drill lie inside the curve. The curve removes
    // 10 times the part of the disc of radius 3 left of x = 10: 9 π less the segment 9 acos(1/3) - √8. Two holes that meet
    // have no closed form either (item 14). Each case with no
    // closed form is refused with E-MEASURE-CURVED: a body with a curved face, the side of the notch and an edge of it, a
    // torus, the side of an arc whose nearest point of the whole turn is not on the arc, a sphere for an angle, and two
    // curved faces that items 16 and 22 do not measure: a cone and a cylinder on two parallel axes (phase K15d, TASK-0450:
    // on one axis they have a closed form). A plane face of a cylinder is measured.
    [NativeManifoldFact]
    public async Task A_Notch_Gives_The_Volume_Of_Its_Facets_And_Each_Case_With_No_Closed_Form_Is_Refused()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = await Box(session, 20, 20, 10);
        var drill = Output(await Applied(session, new CreateCylinderCommand { Radius = 3, Height = 20 }));
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = await Moved(session, drill, 9, 0, 0) }));
        var box = await Box(session, 4, 4, 4);

        var mass = await Mass(session, cut);
        var facets = await session.Read((document, _) => KernelProjection.Of(document, cut).Solid!.Volume().ToDouble());
        Assert.True(mass.Facets);
        Assert.Equal(facets, mass.Volume);
        var curve = 4000 - (10 * ((9 * Math.PI) - (9 * Math.Acos(1.0 / 3)) + Math.Sqrt(8)));
        Assert.True(mass.Volume > curve && mass.Volume < curve + 5, $"{mass.Volume} against {curve}");

        // Two holes of radius 3 whose axes are 4 apart meet, so the second has no closed form either.
        async Task<Guid> Drill(Guid body, double x)
            => Output(await Applied(session, new SubtractCommand
            {
                MinuendBodyId = body, SubtrahendBodyId = await Moved(session, Output(await Applied(session, new CreateCylinderCommand { Radius = 3, Height = 20 })), x, 0, 0),
            }));
        Assert.True((await Mass(session, await Drill(await Drill(await Box(session, 20, 20, 10), 0), 4))).Facets);

        var code = DiagnosticCodes.MeasureCurved;
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = cut, BodyIdB = box }));
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = box, BodyIdB = cut, FeatureIdB = drill, ElementB = "side" }));
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = box, BodyIdB = cut, FeatureIdB = plate, ElementB = "+z", SecondFeatureIdB = drill, SecondElementB = "side",
        }));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(cut, drill, "side", box, box, "+z")));

        var torus = Output(await Applied(session, new CreateTorusCommand { Radius = 10, TubeRadius = 2 }));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 2 }));
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var other = Output(await Applied(session, new CreateCylinderCommand { Radius = 1, Height = 2 }));
        var (arc, arcItem) = await ArcExtrude(session);
        GetDistanceQuery Faces(Guid a, string elementA, Guid b, string elementB, Guid? itemA = null)
            => new() { BodyIdA = a, FeatureIdA = a, ElementA = elementA, ItemIdA = itemA, BodyIdB = b, FeatureIdB = b, ElementB = elementB };
        Assert.Equal(code, await Refusal<DistanceInfo>(session, Faces(torus, "surface", box, "+x")));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(torus, torus, "surface", box, box, "+x")));
        var aside = await Moved(session, Output(await Applied(session, new CreateCylinderCommand { Radius = 1, Height = 2 })), 20, 0, 0);
        Assert.Equal(code, await Refusal<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = cone, FeatureIdA = cone, ElementA = "side", BodyIdB = aside, FeatureIdB = await Feature(session, aside), ElementB = "side",
        }));
        Assert.Equal(code, await Refusal<DistanceInfo>(session, Faces(arc, "side", other, "side", arcItem)));
        Assert.Equal(code, await Refusal<DistanceInfo>(session, Faces(arc, "side", box, "-x", arcItem)));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(sphere, sphere, "surface", box, box, "+z")));
        Assert.Equal(code, await Refusal<AngleInfo>(session, Angle(sphere, sphere, "surface", cylinder, cylinder, "side")));

        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(cylinder, cylinder, "+z", box, box, "+z")));
        var top = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = cylinder, FeatureIdA = cylinder, ElementA = "+z", BodyIdB = box, FeatureIdB = box, ElementB = "+z" });
        Assert.Equal((3.0, 5.0, 2.0), (top.Distance, top.PointA.Z, top.PointB.Z));
    }

    // Item 10 (phase K15b, TASK-0434): a cylinder, a cone, a sphere and a torus give the volume and the centre of the curve
    // of the design, with no flag of facets: π r² h, π r² h / 3 with the centre a quarter of the height over the base,
    // 4 π r³ / 3, and 2 π² R r². A Translate and a Rotate move the centre: (10, 20, 30) turned a quarter about z is
    // (-20, 10, 30).
    [NativeManifoldFact]
    public async Task A_Cylinder_A_Cone_A_Sphere_And_A_Torus_Give_The_Volume_Of_The_Curve()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 2 }));
        var torus = Output(await Applied(session, new CreateTorusCommand { Radius = 10, TubeRadius = 2 }));
        var placed = await Moved(session, Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 })), 10, 20, 30);
        var turned = Output(await Applied(session, new RotateCommand { BodyId = placed, Qw = 1, Qx = 0, Qy = 0, Qz = 1 }));
        var raised = await Moved(session, Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 })), 0, 0, 7);

        AssertMass(250 * Math.PI, (0, 0, 0), await Mass(session, cylinder));
        AssertMass(12 * Math.PI, (0, 0, -1), await Mass(session, cone));
        AssertMass(32 * Math.PI / 3, (0, 0, 0), await Mass(session, sphere));
        AssertMass(80 * Math.PI * Math.PI, (0, 0, 0), await Mass(session, torus));
        AssertMass(250 * Math.PI, (-20, 10, 30), await Mass(session, turned));
        AssertMass(12 * Math.PI, (0, 0, 6), await Mass(session, raised));
    }

    // Item 10 (phase K15b, TASK-0434): an extrude gives the volume of its profile with its exact arcs. The rectangle of 10
    // by 10 with a half disc of radius 5 on its right side has the area 100 + 12.5 π; the half disc has its centre
    // 20 / (3 π) right of x = 10, so the moment about x = 0 is 500 + 125 π + 250 / 3. The same half disc as a bite into the
    // square, an arc that the loop walks from its end to its start, gives 100 - 12.5 π and 500 - 125 π + 250 / 3. A circle
    // of radius 3 at (2, 1) extruded by 5 gives 45 π at (2, 1, 2.5).
    [NativeManifoldFact]
    public async Task An_Extrude_Of_Arcs_Or_Of_A_Circle_Gives_The_Volume_Of_The_Curve()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (bulge, _) = await ArcExtrude(session);
        var (bite, _) = await ArcExtrude(session, bite: true);

        var area = 100 + (12.5 * Math.PI);
        AssertMass(4 * area, ((500 + (125 * Math.PI) + (250.0 / 3)) / area, 5, 2), await Mass(session, bulge));
        var less = 100 - (12.5 * Math.PI);
        AssertMass(4 * less, ((500 - (125 * Math.PI) + (250.0 / 3)) / less, 5, 2), await Mass(session, bite));

        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 2, Y = 1 }), "pointId");
        await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 3 });
        var disc = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 }));
        AssertMass(45 * Math.PI, (2, 1, 2.5), await Mass(session, disc));
    }

    // Item 11 (phase K15b, TASK-0434): the distance from the surface of a sphere of radius 5 at the origin to a plane face
    // 15 from its centre, to the same box as a body, to a box of 2 inside it (5 - √3 to a corner), to two faces that cross
    // it, from inside a large box (0), and to three spheres: apart, inside it and crossing it.
    [NativeManifoldFact]
    public async Task The_Distance_From_A_Sphere_To_Faces_Bodies_And_Spheres()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 5 }));
        Task<DistanceInfo> To(Guid body, Guid? feature = null, string? element = null)
            => Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = sphere, FeatureIdA = sphere, ElementA = "surface", BodyIdB = body, FeatureIdB = feature, ElementB = element });

        var far = await Moved(session, await Box(session, 10, 10, 10), 20, 0, 0);
        var face = await To(far, await Feature(session, far), "-x");
        AssertDistance(10, (5, 0, 0), (15, 0, 0), face);
        Assert.Equal(10, (await To(far)).Distance);

        var inside = await To(await Box(session, 2, 2, 2));
        AssertNear(5 - Math.Sqrt(3), inside.Distance);
        Assert.Equal((1.0, 1.0, 1.0), (Math.Abs(inside.PointB.X), Math.Abs(inside.PointB.Y), Math.Abs(inside.PointB.Z)));
        AssertNear(5 / Math.Sqrt(3), Math.Abs(inside.PointA.X));
        AssertNear(5, Length(inside.PointA));

        var crossing = await Moved(session, await Box(session, 10, 10, 10), 4, 0, 0);
        var cross = await To(crossing, await Feature(session, crossing), "-x");
        Assert.Equal(0, cross.Distance);
        Assert.Equal(cross.PointA, cross.PointB);
        AssertNear(-1, cross.PointA.X);
        AssertNear(5, Length(cross.PointA));

        // A face from y = 3 to y = 13 that crosses the sphere: the point where they meet is on the face, so y ≥ 3.
        var aside = await Moved(session, await Box(session, 10, 10, 10), 4, 8, 0);
        var edge = await To(aside, await Feature(session, aside), "-x");
        Assert.Equal(0, edge.Distance);
        AssertNear(-1, edge.PointA.X);
        AssertNear(5, Length(edge.PointA));
        Assert.True(edge.PointA.Y is >= 3 and <= 13 && Math.Abs(edge.PointA.Z) <= 5, $"{edge.PointA}");

        var around = await To(await Box(session, 40, 40, 40));
        Assert.Equal((0.0, 5.0, 5.0), (around.Distance, around.PointA.X, around.PointB.X));

        async Task<DistanceInfo> Sphere(double radius, double x)
        {
            var other = await Moved(session, Output(await Applied(session, new CreateSphereCommand { Radius = radius })), x, 0, 0);
            return await Answer<DistanceInfo>(session, new GetDistanceQuery
            {
                BodyIdA = sphere, FeatureIdA = sphere, ElementA = "surface", BodyIdB = other, FeatureIdB = (await Feature(session, other)), ElementB = "surface",
            });
        }

        AssertDistance(3, (5, 0, 0), (8, 0, 0), await Sphere(2, 10));
        AssertDistance(3, (5, 0, 0), (2, 0, 0), await Sphere(1, 1));
        var meet = await Sphere(2, 6);
        Assert.Equal(0, meet.Distance);
        AssertNear(5, Length(meet.PointA));
        AssertNear(2, Length(new DatumPoint(meet.PointA.X - 6, meet.PointA.Y, meet.PointA.Z)));
    }

    // Item 11 (phase K15b, TASK-0434): the distance from the side of a cylinder of radius 5 from z = -5 to z = 5 to a face
    // 15 from its axis, to the same box as a body, to a face that goes past the top, to an edge (√250 - 5), to a box of 2
    // inside it (5 - √2), to a face that crosses it, from inside a large box, and from the side of the cylinder of an
    // extrude of a circle. A face over the top is nearest to the rim of the top (phase K15c, TASK-0444): 2 mm. A turned box
    // with a corner past the top can be nearest past the end of the side, and its slanted faces have no closed form there.
    [NativeManifoldFact]
    public async Task The_Distance_From_The_Side_Of_A_Cylinder_To_Faces_Edges_And_Bodies()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        GetDistanceQuery From(Guid body, Guid? feature = null, string? element = null, string? second = null)
            => new() { BodyIdA = cylinder, FeatureIdA = cylinder, ElementA = "side", BodyIdB = body, FeatureIdB = feature, ElementB = element, SecondFeatureIdB = second is null ? null : feature, SecondElementB = second };
        Task<DistanceInfo> To(Guid body, Guid? feature = null, string? element = null, string? second = null)
            => Answer<DistanceInfo>(session, From(body, feature, element, second));

        var far = await Moved(session, await Box(session, 10, 10, 10), 20, 0, 0);
        var farFeature = await Feature(session, far);
        var face = await To(far, farFeature, "-x");
        Assert.Equal((10.0, 5.0, 15.0), (face.Distance, face.PointA.X, face.PointB.X));
        Assert.Equal((face.PointA.Y, face.PointA.Z), (face.PointB.Y, face.PointB.Z));
        Assert.Equal(10, (await To(far)).Distance);
        AssertNear(Math.Sqrt(250) - 5, (await To(far, farFeature, "-x", "+y")).Distance);

        var high = await Moved(session, await Box(session, 10, 10, 10), 20, 0, 8);
        var past = await To(high, await Feature(session, high), "-x");
        Assert.Equal((10.0, 5.0, 15.0), (past.Distance, past.PointA.X, past.PointB.X));
        Assert.True(past.PointA.Z is >= 3 and <= 5, $"{past.PointA}");

        var inside = await To(await Box(session, 2, 2, 2));
        AssertNear(5 - Math.Sqrt(2), inside.Distance);
        Assert.Equal((1.0, 1.0), (Math.Abs(inside.PointB.X), Math.Abs(inside.PointB.Y)));
        AssertNear(5 / Math.Sqrt(2), Math.Abs(inside.PointA.X));
        Assert.Equal(inside.PointA.Z, inside.PointB.Z);

        var crossing = await Moved(session, await Box(session, 10, 10, 10), 8, 0, 0);
        var cross = await To(crossing, await Feature(session, crossing), "-x");
        Assert.Equal(0, cross.Distance);
        Assert.Equal(cross.PointA, cross.PointB);
        AssertNear(3, cross.PointA.X);
        AssertNear(5, Math.Sqrt((cross.PointA.X * cross.PointA.X) + (cross.PointA.Y * cross.PointA.Y)));

        Assert.Equal(0, (await To(await Box(session, 40, 40, 40))).Distance);

        var over = await Moved(session, await Box(session, 10, 10, 10), 0, 0, 12);
        AssertNear(2, (await To(over, await Feature(session, over), "-z")).Distance);

        // A box of 4 turned 45 degrees about y with its centre at (8.5, 0, 6): its lowest corner is in the slab, 1.67 from
        // the side, but its corner at (5.67, 0, 6) is past the top and nearer to the rim.
        var turned = Output(await Applied(session, new RotateCommand { BodyId = await Box(session, 4, 4, 4), Qw = 1 + Math.Sqrt(0.5), Qx = 0, Qy = Math.Sqrt(0.5), Qz = 0 }));
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, From(await Moved(session, turned, 8.5, 0, 6))));

        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var circle = Output(await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 3 }), "circleId");
        var disc = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 6 }));
        var near = await Moved(session, await Box(session, 2, 2, 2), 10, 0, 3);
        var side = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = disc, FeatureIdA = await Feature(session, disc), ElementA = "side", ItemIdA = circle, BodyIdB = near, FeatureIdB = await Feature(session, near), ElementB = "-x",
        });
        Assert.Equal((6.0, 3.0, 9.0), (side.Distance, side.PointA.X, side.PointB.X));
    }

    // Item 12 (phase K15b, TASK-0434): the angle between the side of a cylinder or a cone and a plane face is the angle
    // between the normal of the plane and the nearest outward normal of the side. The side of a cylinder along z makes a
    // right angle with a face +z and the angle 0 with a face +x; turned 45 degrees about x, it makes 45 degrees with +z.
    // A cone of radius 3 and height 4 has outward normals at cos β = 3/5 to its axis: its side and its base give the
    // cosine -3/5 and the sine 4/5, a face +x 4/5 and 3/5, and a face +z 3/5 and 4/5.
    [NativeManifoldFact]
    public async Task The_Angle_Between_The_Side_Of_A_Cylinder_Or_A_Cone_And_A_Plane_Face()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        var box = await Box(session, 4, 4, 4);

        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(cylinder, cylinder, "side", box, box, "+z")));
        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(box, box, "+x", cylinder, cylinder, "side")));
        var tilted = Output(await Applied(session, new RotateCommand { BodyId = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 })), Qw = 1 + Math.Sqrt(0.5), Qx = Math.Sqrt(0.5), Qy = 0, Qz = 0 }));
        var slant = await Answer<AngleInfo>(session, Angle(tilted, await Feature(session, tilted), "side", box, box, "+z"));
        AssertNear(Math.Sqrt(0.5), slant.Cosine);
        AssertNear(Math.Sqrt(0.5), slant.Sine);

        Assert.Equal(new AngleInfo(-0.6, 0.8), await Answer<AngleInfo>(session, Angle(cone, cone, "side", cone, cone, "-z")));
        Assert.Equal(new AngleInfo(0.8, 0.6), await Answer<AngleInfo>(session, Angle(cone, cone, "side", box, box, "+x")));
        Assert.Equal(new AngleInfo(0.6, 0.8), await Answer<AngleInfo>(session, Angle(box, box, "+z", cone, cone, "side")));
    }

    // Item 14 (phase K15c, TASK-0444): a plate of 100 by 50 by 10 with a hole of radius 5 through it at (30, 10) gives
    // 50000 - 250 π, and the hole moves the centre to -250 π (30, 10, 0) / V. A blind hole of radius 5 and depth 5 from
    // the top at (-30, -10) removes 125 π more, at the height 2.5, and a hole of radius 2 at the centre, an extrude of a
    // circle, removes 40 π. A Translate and a Rotate after the Subtract move and turn the centre.
    [NativeManifoldFact]
    public async Task A_Plate_With_Holes_Gives_The_Volume_Of_The_Curve()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        async Task<Guid> Cut(Guid body, Guid tool, double x, double y, double z)
            => Output(await Applied(session, new SubtractCommand { MinuendBodyId = body, SubtrahendBodyId = await Moved(session, tool, x, y, z) }));
        async Task<Guid> Drill(double radius, double height) => Output(await Applied(session, new CreateCylinderCommand { Radius = radius, Height = height }));

        var plate = await Cut(await Box(session, 100, 50, 10), await Drill(5, 20), 30, 10, 0);
        var hole = 250 * Math.PI;
        AssertMass(50000 - hole, (-hole * 30 / (50000 - hole), -hole * 10 / (50000 - hole), 0), await Mass(session, plate));

        var blind = await Cut(plate, await Drill(5, 10), -30, -10, 5);
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 2 });
        var three = await Cut(blind, Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 12 })), 0, 0, -6);
        var volume = 50000 - (415 * Math.PI);
        (double X, double Y, double Z) expected = (-3750 * Math.PI / volume, -1250 * Math.PI / volume, -312.5 * Math.PI / volume);
        AssertMass(volume, expected, await Mass(session, three));

        var raised = await Moved(session, three, 0, 0, 100);
        AssertMass(volume, (expected.X, expected.Y, expected.Z + 100), await Mass(session, raised));
        var turned = Output(await Applied(session, new RotateCommand { BodyId = raised, Qw = 1, Qx = 0, Qy = 0, Qz = 1 }));
        AssertMass(volume, (-expected.Y, expected.X, expected.Z + 100), await Mass(session, turned));
    }

    // Item 14 (phase K15c, TASK-0444): a cone of radius 5 and height 10 with its base at z = 0 removes from a plate of 20 by
    // 20 by 10 the frustum from z = 0 to z = 5: 875 π / 12, with its centre 55 / 28 over the base. A sphere of radius 6 at
    // (2, 3, 10) removes the cap under z = 5, from u = -6 to u = -5 about its centre: π (36 - (216 - 125) / 3) = 17 π / 3,
    // with its centre at u = -30.25 / (17 / 3). A cap keeps the test short: the exact facets of a sphere through the whole
    // plate take about 45 s.
    [NativeManifoldFact]
    public async Task A_Cone_Or_A_Sphere_Through_A_Plate_Gives_The_Volume_Of_The_Curve()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        async Task<Guid> Cut(Command tool, double x, double y, double z)
            => Output(await Applied(session, new SubtractCommand { MinuendBodyId = await Box(session, 20, 20, 10), SubtrahendBodyId = await Moved(session, Output(await Applied(session, tool)), x, y, z) }));

        var cone = 875 * Math.PI / 12;
        AssertMass(4000 - cone, (0, 0, -cone * 55 / 28 / (4000 - cone)), await Mass(session, await Cut(new CreateConeCommand { Radius = 5, Height = 10 }, 0, 0, 5)));

        var cap = 17 * Math.PI / 3;
        var z = 10 - (90.75 / 17);
        var left = 4000 - cap;
        AssertMass(left, (-cap * 2 / left, -cap * 3 / left, -cap * z / left), await Mass(session, await Cut(new CreateSphereCommand { Radius = 6 }, 2, 3, 10)));
    }

    // Item 15 (phase K15c, TASK-0444): the side of a cylinder of radius 5 from z = -5 to z = 5. A point past an end is
    // nearest to the rim of that end. A face over the top at z = 7 that covers the rim is 2 from it, and a face under the
    // bottom too. A box over the top and beside it is √8 from the rim at (5, 0, 5), by its body, its edge and its face. A
    // small box over the middle is nearest by its corner at ρ = √2: √(14² + (5 - √2)²).
    [NativeManifoldFact]
    public async Task The_Distance_Past_An_End_Of_The_Side_Of_A_Cylinder_Is_To_Its_Rim()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        async Task<DistanceInfo> To(Guid body, string? element = null, string? second = null)
        {
            Guid? feature = element is null ? null : await Feature(session, body);
            return await Answer<DistanceInfo>(session, new GetDistanceQuery
            {
                BodyIdA = cylinder, FeatureIdA = cylinder, ElementA = "side",
                BodyIdB = body, FeatureIdB = feature, ElementB = element, SecondFeatureIdB = second is null ? null : feature, SecondElementB = second,
            });
        }

        var over = await Moved(session, await Box(session, 10, 10, 10), 0, 0, 12);
        var top = await To(over, "-z");
        AssertNear(2, top.Distance);
        Assert.Equal((5.0, 7.0), (top.PointA.Z, top.PointB.Z));
        AssertNear(5, Math.Sqrt((top.PointA.X * top.PointA.X) + (top.PointA.Y * top.PointA.Y)));
        AssertNear(top.PointA.X, top.PointB.X);
        AssertNear(top.PointA.Y, top.PointB.Y);
        AssertNear(2, (await To(over)).Distance);

        var bottom = await To(await Moved(session, await Box(session, 10, 10, 10), 0, 0, -12), "+z");
        AssertNear(2, bottom.Distance);
        Assert.Equal((-5.0, -7.0), (bottom.PointA.Z, bottom.PointB.Z));

        var corner = await Moved(session, await Box(session, 10, 10, 10), 12, 0, 12);
        AssertDistance(Math.Sqrt(8), (5, 0, 5), (7, 0, 7), await To(corner));
        AssertDistance(Math.Sqrt(8), (5, 0, 5), (7, 0, 7), await To(corner, "-x", "-z"));
        AssertDistance(Math.Sqrt(8), (5, 0, 5), (7, 0, 7), await To(corner, "-x"));

        // The edge along the axis at (7, 7) over the top is nearest by its lower end: √((√98 - 5)² + 2²).
        var diagonal = await Moved(session, await Box(session, 10, 10, 10), 12, 12, 12);
        var gap = Math.Sqrt(98) - 5;
        AssertDistance(Math.Sqrt((gap * gap) + 4), (5 / Math.Sqrt(2), 5 / Math.Sqrt(2), 5), (7, 7, 7), await To(diagonal, "-x", "-y"));

        var high = await Moved(session, await Box(session, 2, 2, 2), 0, 0, 20);
        var small = await To(high);
        AssertNear(Math.Sqrt(196 + ((5 - Math.Sqrt(2)) * (5 - Math.Sqrt(2)))), small.Distance);
        AssertNear(small.Distance, (await To(high, "-z")).Distance);
        Assert.Equal((1.0, 1.0, 19.0), (Math.Abs(small.PointB.X), Math.Abs(small.PointB.Y), small.PointB.Z));
        AssertNear(5 / Math.Sqrt(2), Math.Abs(small.PointA.X));
        Assert.Equal(5.0, small.PointA.Z);
    }

    // Item 15 (phase K15c, TASK-0444): the side of a cone of radius 3 and height 4, from its base at z = -2 to its apex at
    // z = 2. A face beside it at x = 10 is 7 from the rim at (3, 0, -2); a face over the apex at z = 3 is 1 from the apex;
    // a face under the base at z = -3 is 1 from the rim; a face at x = 1 crosses the side; a large box holds it; and a
    // turned box with slanted faces near the side has no closed form.
    [NativeManifoldFact]
    public async Task The_Distance_From_The_Side_Of_A_Cone()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        GetDistanceQuery From(Guid body, Guid? feature, string? element)
            => new() { BodyIdA = cone, FeatureIdA = cone, ElementA = "side", BodyIdB = body, FeatureIdB = feature, ElementB = element };
        async Task<DistanceInfo> To(Guid body, string? element = null)
            => await Answer<DistanceInfo>(session, From(body, element is null ? null : await Feature(session, body), element));

        var beside = await Moved(session, await Box(session, 10, 10, 10), 15, 0, 0);
        AssertDistance(7, (3, 0, -2), (10, 0, -2), await To(beside, "-x"));
        AssertNear(7, (await To(beside)).Distance);

        AssertDistance(1, (0, 0, 2), (0, 0, 3), await To(await Moved(session, await Box(session, 10, 10, 10), 0, 0, 8), "-z"));

        var under = await To(await Moved(session, await Box(session, 10, 10, 10), 0, 0, -8), "+z");
        AssertNear(1, under.Distance);
        Assert.Equal((-2.0, -3.0), (under.PointA.Z, under.PointB.Z));
        AssertNear(3, Math.Sqrt((under.PointB.X * under.PointB.X) + (under.PointB.Y * under.PointB.Y)));
        AssertNear(under.PointA.X, under.PointB.X);
        AssertNear(under.PointA.Y, under.PointB.Y);

        var cross = await To(await Moved(session, await Box(session, 10, 10, 10), 6, 0, 0), "-x");
        Assert.Equal(0, cross.Distance);
        Assert.Equal(cross.PointA, cross.PointB);
        Assert.Equal(1.0, cross.PointA.X);

        // An edge along the axis at (10, 10) from z = 0 to z = 10 is nearest to the rim by its lower end:
        // √((√200 - 3)² + 2²). One at (1, 1) from z = -5 to z = 5 crosses the side where the radius is √2, at
        // z = 2 - 4 √2 / 3.
        async Task<DistanceInfo> Edge(double x, double y, double z)
        {
            var box = await Moved(session, await Box(session, 10, 10, 10), x, y, z);
            var feature = await Feature(session, box);
            return await Answer<DistanceInfo>(session, new GetDistanceQuery
            {
                BodyIdA = cone, FeatureIdA = cone, ElementA = "side", BodyIdB = box, FeatureIdB = feature, ElementB = "-x", SecondFeatureIdB = feature, SecondElementB = "-y",
            });
        }

        var rim = Math.Sqrt(200) - 3;
        AssertDistance(Math.Sqrt((rim * rim) + 4), (3 / Math.Sqrt(2), 3 / Math.Sqrt(2), -2), (10, 10, 0), await Edge(15, 15, 5));
        var through = await Edge(6, 6, 0);
        Assert.Equal(0, through.Distance);
        Assert.Equal(through.PointA, through.PointB);
        Assert.Equal((1.0, 1.0), (through.PointA.X, through.PointA.Y));
        AssertNear(2 - (4 * Math.Sqrt(2) / 3), through.PointA.Z);

        Assert.Equal(0, (await To(await Box(session, 40, 40, 40))).Distance);

        var turned = Output(await Applied(session, new RotateCommand { BodyId = await Box(session, 4, 4, 4), Qw = 1 + Math.Sqrt(0.5), Qx = 0, Qy = Math.Sqrt(0.5), Qz = 0 }));
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, From(await Moved(session, turned, 6, 0, 0), null, null)));
    }

    // Item 16 (phase K15c, TASK-0444): the sides of two cylinders with parallel axes. With the distance D of the axes, the
    // sides are D - r - s apart, |r - s| - D one inside the other, or they cross; a gap g between their spans along the axis
    // adds as √(d² + g²). A cylinder of radius 5 from z = -5 to 5 and one of radius 2: at (10, 0) 3, also 10 higher √18, at
    // (1, 0) inside 2, at (6, 0) crossing, and the side of an extrude of a circle of radius 3 on the same axis 2. Two axes
    // that are not parallel are refused when the common perpendicular of the axes is past an end of a side (phase K15d,
    // TASK-0450: item 22 measures the other case).
    [NativeManifoldFact]
    public async Task The_Distance_Between_The_Sides_Of_Two_Cylinders_With_Parallel_Axes()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        GetDistanceQuery Sides(Guid body, Guid feature, Guid? item = null)
            => new() { BodyIdA = cylinder, FeatureIdA = cylinder, ElementA = "side", BodyIdB = body, FeatureIdB = feature, ElementB = "side", ItemIdB = item };
        async Task<DistanceInfo> To(double x, double z)
        {
            var other = await Moved(session, Output(await Applied(session, new CreateCylinderCommand { Radius = 2, Height = 4 })), x, 0, z);
            return await Answer<DistanceInfo>(session, Sides(other, await Feature(session, other)));
        }

        var apart = await To(10, 0);
        Assert.Equal((3.0, 5.0, 8.0), (apart.Distance, apart.PointA.X, apart.PointB.X));
        Assert.Equal(apart.PointA.Z, apart.PointB.Z);
        Assert.True(apart.PointA.Z is >= -2 and <= 2, $"{apart.PointA}");

        AssertDistance(Math.Sqrt(18), (5, 0, 5), (8, 0, 8), await To(10, 10));

        var inside = await To(1, 0);
        Assert.Equal((2.0, 5.0, 3.0), (inside.Distance, inside.PointA.X, inside.PointB.X));

        var cross = await To(6, 0);
        Assert.Equal(0, cross.Distance);
        Assert.Equal(cross.PointA, cross.PointB);
        AssertNear(5, Math.Sqrt((cross.PointA.X * cross.PointA.X) + (cross.PointA.Y * cross.PointA.Y)));
        AssertNear(2, Math.Sqrt(((cross.PointA.X - 6) * (cross.PointA.X - 6)) + (cross.PointA.Y * cross.PointA.Y)));

        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        var centre = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = 0, Y = 0 }), "pointId");
        var circle = Output(await Applied(session, new AddSketchCircleCommand { SketchId = sketch, CenterPointId = centre, Radius = 3 }), "circleId");
        var disc = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 6 }));
        var coaxial = await Answer<DistanceInfo>(session, Sides(disc, disc, circle));
        Assert.Equal(2, coaxial.Distance);
        Assert.Equal(coaxial.PointA.Z, coaxial.PointB.Z);

        var across = Output(await Applied(session, new RotateCommand { BodyId = Output(await Applied(session, new CreateCylinderCommand { Radius = 1, Height = 2 })), Qw = 1, Qx = 1, Qy = 0, Qz = 0 }));
        var moved = await Moved(session, across, 20, 0, 8);
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, Sides(moved, await Feature(session, moved))));
    }

    // Item 16 (phase K15c, TASK-0444): a sphere with the centre c and the radius s against the side of a cylinder or a cone.
    // With the distances m and M of the nearest and the farthest point of the side from c, the sphere is m - s from the
    // side, s - M around it, or it crosses it. Against the cylinder of radius 5 from z = -5 to 5: a sphere of 2 at (10, 0, 0)
    // 3; at (0, 0, 9) √41 - 2 to the rim; of 1 at (2, 0, 0) 2; of 2 at (5, 0, 0) and of 3 at (6, 0, 4) crossing; of 20 at
    // the origin 20 - √50. A
    // sphere of 1 at (0, 0, 5) is 2 from the apex of the cone of radius 3 and height 4.
    [NativeManifoldFact]
    public async Task The_Distance_Between_A_Sphere_And_The_Side_Of_A_Cylinder_Or_A_Cone()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        async Task<(Guid Body, Guid Feature)> Sphere(double radius, double x, double y, double z)
        {
            var body = await Moved(session, Output(await Applied(session, new CreateSphereCommand { Radius = radius })), x, y, z);
            return (body, await Feature(session, body));
        }

        async Task<DistanceInfo> To(Guid side, string element, (Guid Body, Guid Feature) sphere)
            => await Answer<DistanceInfo>(session, new GetDistanceQuery
            {
                BodyIdA = side, FeatureIdA = side, ElementA = element, BodyIdB = sphere.Body, FeatureIdB = sphere.Feature, ElementB = "surface",
            });

        AssertDistance(3, (5, 0, 0), (8, 0, 0), await To(cylinder, "side", await Sphere(2, 10, 0, 0)));

        var high = await Sphere(2, 0, 0, 9);
        var rim = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = high.Body, FeatureIdA = high.Feature, ElementA = "surface", BodyIdB = cylinder, FeatureIdB = cylinder, ElementB = "side",
        });
        AssertNear(Math.Sqrt(41) - 2, rim.Distance);
        Assert.Equal(5.0, rim.PointB.Z);
        AssertNear(5, Math.Sqrt((rim.PointB.X * rim.PointB.X) + (rim.PointB.Y * rim.PointB.Y)));
        AssertNear(2, Length(new DatumPoint(rim.PointA.X, rim.PointA.Y, rim.PointA.Z - 9)));

        AssertDistance(2, (5, 0, 0), (3, 0, 0), await To(cylinder, "side", await Sphere(1, 2, 0, 0)));

        var cross = await To(cylinder, "side", await Sphere(2, 5, 0, 0));
        Assert.Equal(0, cross.Distance);
        Assert.Equal(cross.PointA, cross.PointB);
        AssertNear(2, Length(new DatumPoint(cross.PointA.X - 5, cross.PointA.Y, cross.PointA.Z)));
        AssertNear(5, Math.Sqrt((cross.PointA.X * cross.PointA.X) + (cross.PointA.Y * cross.PointA.Y)));

        // A sphere of 3 at (6, 0, 4) crosses the side at z = 4 - √8; the other root, z = 4 + √8, is past the top.
        var top = await To(cylinder, "side", await Sphere(3, 6, 0, 4));
        Assert.Equal(0, top.Distance);
        AssertNear(3, Length(new DatumPoint(top.PointA.X - 6, top.PointA.Y, top.PointA.Z - 4)));
        AssertNear(5, Math.Sqrt((top.PointA.X * top.PointA.X) + (top.PointA.Y * top.PointA.Y)));
        AssertNear(4 - Math.Sqrt(8), top.PointA.Z);

        AssertNear(20 - Math.Sqrt(50), (await To(cylinder, "side", await Sphere(20, 0, 0, 0))).Distance);

        AssertDistance(2, (0, 0, 2), (0, 0, 4), await To(cone, "side", await Sphere(1, 0, 0, 5)));
    }

    // Items 15 and 17 (phase K15c, TASK-0444): the side of an arc of radius 5 about (10, 5), from (10, 0) to (10, 10)
    // counter-clockwise, extruded by 4. A face at x = 19 is 4 from it, because the nearest point of the whole turn is on
    // the arc. A face at x = -4 is nearest to a point of the turn that is not on the arc, so it is refused. The outward
    // normals of the arc point to +x and to each direction between -y and +y through +x: a face +x is at 0 degrees, and a
    // face -x and a face +z at 90 degrees. TASK-0449 (finding E235): the same half circle as a bite into the profile has
    // its material outside the circle, so its outward normals point to the centre: to +x and to each direction between -y
    // and +y through +x again. A face +x is at 0 degrees, and a face -x and a face +z at 90 degrees.
    [NativeManifoldFact]
    public async Task The_Distance_And_The_Angle_Of_The_Side_Of_An_Arc()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (arc, item) = await ArcExtrude(session);
        async Task<GetDistanceQuery> To(Guid body, string element)
            => new() { BodyIdA = arc, FeatureIdA = arc, ElementA = "side", ItemIdA = item, BodyIdB = body, FeatureIdB = await Feature(session, body), ElementB = element };

        var right = await Moved(session, await Box(session, 2, 2, 2), 20, 5, 2);
        var near = await Answer<DistanceInfo>(session, await To(right, "-x"));
        Assert.Equal((4.0, 15.0, 19.0, 5.0, 5.0), (near.Distance, near.PointA.X, near.PointB.X, near.PointA.Y, near.PointB.Y));
        Assert.Equal(near.PointA.Z, near.PointB.Z);

        var left = await Moved(session, await Box(session, 2, 2, 2), -5, 5, 2);
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, await To(left, "+x")));

        var box = await Box(session, 4, 4, 4);
        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(arc, arc, "side", box, box, "+x", itemA: item)));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(arc, arc, "side", box, box, "-x", itemA: item)));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(box, box, "+z", arc, arc, "side", itemB: item)));

        var (bite, biteItem) = await ArcExtrude(session, bite: true);
        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(bite, bite, "side", box, box, "+x", itemA: biteItem)));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(bite, bite, "side", box, box, "-x", itemA: biteItem)));
        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(box, box, "+z", bite, bite, "side", itemB: biteItem)));
    }

    // Item 17 (phase K15c, TASK-0444): the angle of two sides of a cylinder, a cone or an arc is the angle between their
    // axes, as lines, from 0 to 90 degrees. A cylinder along z and one turned 45 degrees about x: √½ and √½; a cone and the
    // side of an arc along z: 0 degrees; a cylinder turned 90 degrees: 90 degrees; turned 180 degrees: 0 degrees. A sphere
    // has no axis, so it is refused.
    [NativeManifoldFact]
    public async Task The_Angle_Between_Two_Curved_Faces_Is_The_Angle_Between_Their_Axes()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 2 }));
        var (arc, item) = await ArcExtrude(session);
        async Task<AngleInfo> Turned(double qw, double qx)
        {
            var body = Output(await Applied(session, new RotateCommand { BodyId = Output(await Applied(session, new CreateCylinderCommand { Radius = 1, Height = 2 })), Qw = qw, Qx = qx, Qy = 0, Qz = 0 }));
            return await Answer<AngleInfo>(session, Angle(cylinder, cylinder, "side", body, await Feature(session, body), "side"));
        }

        var slant = await Turned(1 + Math.Sqrt(0.5), Math.Sqrt(0.5));
        AssertNear(Math.Sqrt(0.5), slant.Cosine);
        AssertNear(Math.Sqrt(0.5), slant.Sine);
        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(cylinder, cylinder, "side", cone, cone, "side")));
        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(arc, arc, "side", cone, cone, "side", itemA: item)));
        var right = await Turned(1, 1);
        Assert.True(Math.Abs(right.Cosine) <= 1e-12, $"{right}");
        AssertNear(1, right.Sine);
        Assert.Equal(new AngleInfo(1, 0), await Turned(0, 1));
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<AngleInfo>(session, Angle(cylinder, cylinder, "side", sphere, sphere, "surface")));
    }

    // Item 21 (phase K15d, TASK-0450): the side of a hole is the side of its tool on the parts of the axis that the body
    // holds. A plate of 100 by 50 by 10 with a hole of radius 5 at (30, 10): the side is 15 from the face +x (x = 50), 10
    // from the face +y (y = 25), 10 from a box over the plate (z from 15) by its rim, and 40 from the side of a hole at
    // (-20, 10). Its outward normals point to the axis, so a face +z is at 90 degrees and a face +x at 0 degrees. A clevis,
    // a box of 20 with a slot from x = -5 and z from -5 to 5, has a hole of radius 2 at (3, 0) in two parts: each is 6 from
    // the face of the slot at x = -5, at the plane of the slot, and the upper part is 2 from a box over the clevis (z from
    // 12), at its rim at z = 10.
    [NativeManifoldFact]
    public async Task The_Side_Of_A_Hole_Gives_Its_Distance_And_Its_Angle()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        async Task<Guid> Cut(Guid body, Guid tool, double x, double y, double z)
            => Output(await Applied(session, new SubtractCommand { MinuendBodyId = body, SubtrahendBodyId = await Moved(session, tool, x, y, z) }));
        async Task<Guid> Drill(double radius, double height) => Output(await Applied(session, new CreateCylinderCommand { Radius = radius, Height = height }));

        var box = await Box(session, 100, 50, 10);
        var drill = await Drill(5, 20);
        var plate = await Cut(box, drill, 30, 10, 0);
        GetDistanceQuery From(Guid body, Guid? feature = null, string? element = null)
            => new() { BodyIdA = plate, FeatureIdA = drill, ElementA = "side", BodyIdB = body, FeatureIdB = feature, ElementB = element };

        var side = await Answer<DistanceInfo>(session, From(plate, box, "+x"));
        Assert.Equal((15.0, 35.0, 50.0, 10.0, 10.0), (side.Distance, side.PointA.X, side.PointB.X, side.PointA.Y, side.PointB.Y));
        Assert.True(side.PointA.Z == side.PointB.Z && side.PointA.Z is >= -5 and <= 5, $"{side.PointA}");
        var front = await Answer<DistanceInfo>(session, From(plate, box, "+y"));
        Assert.Equal((10.0, 15.0, 25.0), (front.Distance, front.PointA.Y, front.PointB.Y));

        var over = await Moved(session, await Box(session, 10, 10, 10), 30, 10, 20);
        var rim = await Answer<DistanceInfo>(session, From(over));
        AssertNear(10, rim.Distance);
        Assert.Equal((5.0, 15.0), (rim.PointA.Z, rim.PointB.Z));
        AssertNear(5, Math.Sqrt(((rim.PointA.X - 30) * (rim.PointA.X - 30)) + ((rim.PointA.Y - 10) * (rim.PointA.Y - 10))));

        Assert.Equal(new AngleInfo(0, 1), await Answer<AngleInfo>(session, Angle(plate, drill, "side", plate, box, "+z")));
        Assert.Equal(new AngleInfo(1, 0), await Answer<AngleInfo>(session, Angle(plate, box, "+x", plate, drill, "side")));

        var other = await Drill(5, 20);
        var two = await Cut(plate, other, -20, 10, 0);
        var holes = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = two, FeatureIdA = drill, ElementA = "side", BodyIdB = two, FeatureIdB = other, ElementB = "side" });
        Assert.Equal((40.0, 25.0, -15.0), (holes.Distance, holes.PointA.X, holes.PointB.X));

        var block = await Box(session, 20, 20, 20);
        var slot = await Box(session, 25, 30, 10);
        var through = await Drill(2, 40);
        var clevis = await Cut(await Cut(block, slot, 7.5, 0, 0), through, 3, 0, 0);
        var pin = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = clevis, FeatureIdA = through, ElementA = "side", BodyIdB = clevis, FeatureIdB = slot, ElementB = "-x",
        });
        Assert.Equal((6.0, 1.0, -5.0, 0.0), (pin.Distance, pin.PointA.X, pin.PointB.X, pin.PointA.Y));
        Assert.Equal((5.0, 5.0), (Math.Abs(pin.PointA.Z), Math.Abs(pin.PointB.Z)));
        var cap = await Moved(session, await Box(session, 10, 10, 10), 3, 0, 17);
        var top = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = clevis, FeatureIdA = through, ElementA = "side", BodyIdB = cap });
        AssertNear(2, top.Distance);
        Assert.Equal((10.0, 12.0), (top.PointA.Z, top.PointB.Z));
    }

    // Item 21 (phase K15d, TASK-0450): a countersink, a cone of radius 6 and height 6 with its apex down and its base on the
    // top of a plate of 40 by 40 by 10, over a hole of radius 2. The outward normals of the side of the cone point up and to
    // the axis, at 45 degrees to the face +z; the tool alone looks the other way, at 135 degrees. A box over the plate (z
    // from 8) is 3 from the rim of radius 6, and a pin of radius 1 on the axis is 1 from the bottom of the cone, where its
    // radius is 2 (item 22, two sides on one axis).
    [NativeManifoldFact]
    public async Task The_Side_Of_A_Countersink_Gives_Its_Distance_And_Its_Angle()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (plate, box, cone) = await Countersink(session);
        var half = Math.Sqrt(0.5);
        var angle = await Answer<AngleInfo>(session, Angle(plate, cone, "side", plate, box, "+z"));
        AssertNear(half, angle.Cosine);
        AssertNear(half, angle.Sine);
        var alone = Output(await Applied(session, new CreateConeCommand { Radius = 6, Height = 6 }));
        var turned = Output(await Applied(session, new RotateCommand { BodyId = alone, Qw = 0, Qx = 1, Qy = 0, Qz = 0 }));
        var tool = await Answer<AngleInfo>(session, Angle(turned, alone, "side", plate, box, "+z"));
        AssertNear(-half, tool.Cosine);
        AssertNear(half, tool.Sine);

        var over = await Moved(session, await Box(session, 20, 20, 20), 0, 0, 18);
        var rim = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = plate, FeatureIdA = cone, ElementA = "side", BodyIdB = over });
        AssertNear(3, rim.Distance);
        Assert.Equal((5.0, 8.0), (rim.PointA.Z, rim.PointB.Z));
        AssertNear(6, Math.Sqrt((rim.PointA.X * rim.PointA.X) + (rim.PointA.Y * rim.PointA.Y)));

        var pin = await Moved(session, Output(await Applied(session, new CreateCylinderCommand { Radius = 1, Height = 10 })), 0, 0, 5);
        var bottom = await Answer<DistanceInfo>(session, new GetDistanceQuery
        {
            BodyIdA = plate, FeatureIdA = cone, ElementA = "side", BodyIdB = pin, FeatureIdB = await Feature(session, pin), ElementB = "side",
        });
        AssertNear(1, bottom.Distance);
        AssertNear(1, bottom.PointA.Z);
        AssertNear(1, bottom.PointB.Z);
        AssertNear(2, Math.Sqrt((bottom.PointA.X * bottom.PointA.X) + (bottom.PointA.Y * bottom.PointA.Y)));
        AssertNear(1, Math.Sqrt((bottom.PointB.X * bottom.PointB.X) + (bottom.PointB.Y * bottom.PointB.Y)));
    }

    // Item 20 (phase K15d, TASK-0450): tools on one axis join. A plate of 40 by 40 by 10 with a hole of radius 3 and a
    // counterbore of radius 5 from z = 1 removes 9 π 6 + 25 π 4 = 154 π, with the moment 192 π along z. The countersink
    // removes 4 π 6 and π ∫ (z + 1)² from z = 1 to 5: 280 π / 3, with the moment 608 π / 3. The clevis keeps 5000 less the
    // two parts of its hole, 40 π, at x = 3. A tube, a cylinder of radius 10 and height 20 less one of radius 6 on its axis,
    // keeps 1280 π; a cone of radius 6 and height 12 less a bore of radius 3 keeps 72 π at z = -4.125. The outer side of
    // the tube is 10 from a face at x = 20, and its inner side 4 from a sphere of radius 2 at its centre.
    [NativeManifoldFact]
    public async Task Holes_On_One_Axis_And_A_Tube_Give_The_Volume_Of_The_Curve()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        async Task<Guid> Cut(Guid body, Command tool, double x, double y, double z)
            => Output(await Applied(session, new SubtractCommand { MinuendBodyId = body, SubtrahendBodyId = await Moved(session, Output(await Applied(session, tool)), x, y, z) }));

        var bored = await Cut(await Cut(await Box(session, 40, 40, 10), new CreateCylinderCommand { Radius = 3, Height = 20 }, 0, 0, 0), new CreateCylinderCommand { Radius = 5, Height = 8 }, 0, 0, 5);
        var left = 16000 - (154 * Math.PI);
        AssertMass(left, (0, 0, -192 * Math.PI / left), await Mass(session, bored));

        var (sunk, _, _) = await Countersink(session);
        left = 16000 - (280 * Math.PI / 3);
        AssertMass(left, (0, 0, -608 * Math.PI / 3 / left), await Mass(session, sunk));

        var slotted = await Cut(await Box(session, 20, 20, 20), new CreateBoxCommand { SizeX = 25, SizeY = 30, SizeZ = 10 }, 7.5, 0, 0);
        var clevis = await Cut(slotted, new CreateCylinderCommand { Radius = 2, Height = 40 }, 3, 0, 0);
        left = 5000 - (40 * Math.PI);
        AssertMass(left, ((-7500 - (120 * Math.PI)) / left, 0, 0), await Mass(session, clevis));

        var outer = Output(await Applied(session, new CreateCylinderCommand { Radius = 10, Height = 20 }));
        var tube = await Cut(outer, new CreateCylinderCommand { Radius = 6, Height = 30 }, 0, 0, 0);
        AssertMass(1280 * Math.PI, (0, 0, 0), await Mass(session, tube));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 6, Height = 12 }));
        AssertMass(72 * Math.PI, (0, 0, -4.125), await Mass(session, await Cut(cone, new CreateCylinderCommand { Radius = 3, Height = 20 }, 0, 0, 0)));

        var wall = await Moved(session, await Box(session, 10, 10, 10), 25, 0, 0);
        var outside = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = tube, FeatureIdA = outer, ElementA = "side", BodyIdB = wall, FeatureIdB = await Feature(session, wall), ElementB = "-x" });
        Assert.Equal((10.0, 10.0, 20.0, 0.0), (outside.Distance, outside.PointA.X, outside.PointB.X, outside.PointA.Y));
        var ball = Output(await Applied(session, new CreateSphereCommand { Radius = 2 }));
        var bore = (await Answer<FacesInfo>(session, new GetFacesQuery { BodyId = tube })).Faces.Select(face => face.Reference).First(reference => reference.Element == "side" && reference.FeatureId != outer);
        var inside = await Answer<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = tube, FeatureIdA = bore.FeatureId, ElementA = "side", BodyIdB = ball, FeatureIdB = ball, ElementB = "surface" });
        AssertNear(4, inside.Distance);
        AssertNear(6, Length(inside.PointA));
        AssertNear(2, Length(inside.PointB));
    }

    // Item 22 (phase K15d, TASK-0450): two sides on one axis are as far apart as their profiles in the half-plane (ρ, z).
    // The cone of radius 3 and height 4 from z = -2 and a cylinder of radius 5 from z = -1 to 1 are √5 apart, from the rim
    // (3, -2) to (5, -1); a cone of radius 2 from z = 3 to 7 is 2.2 from the first, from (0.24, 1.68) to its rim (2, 3).
    // A cone and a cylinder on two parallel axes have no closed form.
    [NativeManifoldFact]
    public async Task Two_Sides_On_One_Axis_Are_As_Far_Apart_As_Their_Profiles()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 3, Height = 4 }));
        async Task<DistanceInfo> To(Command command, double z)
        {
            var body = await Moved(session, Output(await Applied(session, command)), 0, 0, z);
            return await Answer<DistanceInfo>(session, new GetDistanceQuery
            {
                BodyIdA = cone, FeatureIdA = cone, ElementA = "side", BodyIdB = body, FeatureIdB = await Feature(session, body), ElementB = "side",
            });
        }

        AssertDistance(Math.Sqrt(5), (0, 3, -2), (0, 5, -1), await To(new CreateCylinderCommand { Radius = 5, Height = 2 }, 0));
        AssertDistance(2.2, (0, 0.24, 1.68), (0, 2, 3), await To(new CreateConeCommand { Radius = 2, Height = 4 }, 5));
    }

    // Item 22 (phase K15d, TASK-0450): two cylinders whose axes are not parallel. Each point of a side is within its radius
    // of its axis, so D - r - s is a lower bound, and the common perpendicular of the axes reaches it when its two feet are
    // on the two spans. The cylinder of radius 5 from z = -5 to 5 and one of radius 1 along y at x = 10 and z = 2 are 4
    // apart. At z = 7 the foot is past the top, and at x = 5 the two cross: both have no closed form.
    [NativeManifoldFact]
    public async Task The_Sides_Of_Two_Cylinders_Whose_Axes_Are_Not_Parallel()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        async Task<GetDistanceQuery> To(double x, double z)
        {
            var across = Output(await Applied(session, new RotateCommand { BodyId = Output(await Applied(session, new CreateCylinderCommand { Radius = 1, Height = 20 })), Qw = 1, Qx = 1, Qy = 0, Qz = 0 }));
            var body = await Moved(session, across, x, 0, z);
            return new() { BodyIdA = cylinder, FeatureIdA = cylinder, ElementA = "side", BodyIdB = body, FeatureIdB = await Feature(session, body), ElementB = "side" };
        }

        AssertDistance(4, (5, 0, 2), (9, 0, 2), await Answer<DistanceInfo>(session, await To(10, 2)));
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, await To(10, 7)));
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, await To(5, 0)));
    }

    // Item 22 (phase K15d, TASK-0450): the side of an arc with a curved face is measured as its whole turn when the point of
    // the turn is on the arc. The arc of radius 5 about (10, 5) that bulges to +x is 4 from a sphere of radius 1 at
    // (20, 5, 2) and 4 from a cylinder of radius 1 along z at (20, 5); a sphere at (0, 5, 2) is nearest to a point of the
    // turn that is not on the arc, so it is refused.
    [NativeManifoldFact]
    public async Task The_Side_Of_An_Arc_And_A_Curved_Face()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (arc, item) = await ArcExtrude(session);
        async Task<GetDistanceQuery> To(Command command, double x, string element)
        {
            var body = await Moved(session, Output(await Applied(session, command)), x, 5, 2);
            return new() { BodyIdA = arc, FeatureIdA = arc, ElementA = "side", ItemIdA = item, BodyIdB = body, FeatureIdB = await Feature(session, body), ElementB = element };
        }

        AssertDistance(4, (15, 5, 2), (19, 5, 2), await Answer<DistanceInfo>(session, await To(new CreateSphereCommand { Radius = 1 }, 20, "surface")));
        var post = await Answer<DistanceInfo>(session, await To(new CreateCylinderCommand { Radius = 1, Height = 4 }, 20, "side"));
        Assert.Equal((4.0, 15.0, 19.0, 5.0, 5.0), (post.Distance, post.PointA.X, post.PointB.X, post.PointA.Y, post.PointB.Y));
        Assert.Equal(post.PointA.Z, post.PointB.Z);
        Assert.Equal(DiagnosticCodes.MeasureCurved, await Refusal<DistanceInfo>(session, await To(new CreateSphereCommand { Radius = 1 }, 0, "surface")));
    }

    // The plate of 40 by 40 by 10 with a hole of radius 2 and a countersink: a cone of radius 6 and height 6, turned a half
    // turn about x so that its apex is down, with its base at z = 5. The body, the box of the plate and the cone.
    private static async Task<(Guid Plate, Guid Box, Guid Cone)> Countersink(DocumentSession session)
    {
        var box = await Box(session, 40, 40, 10);
        var drill = Output(await Applied(session, new CreateCylinderCommand { Radius = 2, Height = 20 }));
        var holed = Output(await Applied(session, new SubtractCommand { MinuendBodyId = box, SubtrahendBodyId = drill }));
        var cone = Output(await Applied(session, new CreateConeCommand { Radius = 6, Height = 6 }));
        var down = Output(await Applied(session, new RotateCommand { BodyId = cone, Qw = 0, Qx = 1, Qy = 0, Qz = 0 }));
        var plate = Output(await Applied(session, new SubtractCommand { MinuendBodyId = holed, SubtrahendBodyId = await Moved(session, down, 0, 0, 2) }));
        return (plate, box, cone);
    }

    // Item 13 (phase K15b, TASK-0434): the angle of an arc comes from + - * / and the square root only, so it is the same
    // on each platform. It agrees with Math.Atan2, which the test may call, to 1e-15 at each of 3600 directions.
    [Fact]
    public void The_Angle_Of_A_Direction_Uses_Plus_Minus_Times_Divide_And_Root_Only()
    {
        for (var i = 0; i < 3600; i++)
        {
            var (x, y) = (Math.Cos(i * Math.PI / 1800), Math.Sin(i * Math.PI / 1800));
            var expected = Math.Atan2(y, x);
            if (expected < 0)
                expected += 2 * Math.PI;
            var actual = MeasureCurve.Angle(x, y);
            Assert.True(Math.Abs(actual - expected) <= 1e-15 * Math.Max(1, expected) || Math.Abs(actual - expected - (2 * Math.PI)) <= 1e-14, $"{i}: {actual} against {expected}");
        }

        Assert.Equal((0.0, Math.PI / 2, Math.PI, 3 * Math.PI / 2), (MeasureCurve.Angle(1, 0), MeasureCurve.Angle(0, 2), MeasureCurve.Angle(-3, 0), MeasureCurve.Angle(0, -1)));
    }

    // TASK-0438 (finding E232 of the second codebase review of 2026-11-21): each halving of the arctangent rounds, so item 13
    // gives at most about ten units of the last place, not an exact angle. Angle(1, t) is the arctangent of t, with no
    // rounding of a quotient. Against Math.Atan, which the test may call, the error stays at most ten units of the last place
    // of the result at 100,000 tangents from 1e-6 to 1e6 (7 at most today; the review found 8.8 against 240 bits). A
    // direction just under the axis x gives exactly 2π, so the range is [0, 2π], with 2π as a full turn.
    [Fact]
    public void The_Arctangent_Is_At_Most_Ten_Units_Of_The_Last_Place_From_The_Angle_And_The_Range_Ends_At_Two_Pi()
    {
        var random = new Random(438);
        var worst = (Units: 0.0, Tangent: 0.0);
        for (var i = 0; i < 100_000; i++)
        {
            var t = Math.Pow(10, (random.NextDouble() * 12) - 6);
            var expected = Math.Atan(t);
            var units = Math.Abs(MeasureCurve.Angle(1, t) - expected) / (Math.BitIncrement(expected) - expected);
            if (units > worst.Units)
                worst = (units, t);
        }

        Assert.True(worst.Units <= 10, $"{worst.Units} units of the last place at the tangent {worst.Tangent:R}.");
        Assert.Equal(2 * Math.PI, MeasureCurve.Angle(1, -1e-17));
    }

    // The extrude by 4 of the square (0, 0), (10, 0), (10, 10), (0, 10) whose right side is a half circle about (10, 5): a
    // bulge when the arc goes from (10, 0) to (10, 10) counter-clockwise, and a bite when it goes from (10, 10) to (10, 0).
    // The body and the item of the side of the arc.
    private static async Task<(Guid Body, Guid Arc)> ArcExtrude(DocumentSession session, bool bite = false)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        async Task<Guid> Point(double x, double y) => Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }), "pointId");
        var (p0, p1, p2, p3, centre) = (await Point(0, 0), await Point(10, 0), await Point(10, 10), await Point(0, 10), await Point(10, 5));
        foreach (var (start, end) in new[] { (p0, p1), (p2, p3), (p3, p0) })
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = start, EndPointId = end });
        var arc = Output(await Applied(session, new AddSketchArcCommand
        {
            SketchId = sketch, CenterPointId = centre, StartPointId = bite ? p2 : p1, EndPointId = bite ? p1 : p2,
        }), "arcId");
        return (Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 4 })), arc);
    }

    // A volume within 1e-12 of the expected value, relative, the flag of facets false, and a centre within 1e-9 mm.
    private static void AssertMass(double volume, (double X, double Y, double Z) centre, MassProperties mass)
    {
        Assert.False(mass.Facets, $"{mass}");
        Assert.True(Math.Abs(mass.Volume - volume) <= 1e-12 * volume, $"{mass.Volume} against {volume}");
        Assert.True(Math.Abs(mass.Centre.X - centre.X) <= 1e-9 && Math.Abs(mass.Centre.Y - centre.Y) <= 1e-9 && Math.Abs(mass.Centre.Z - centre.Z) <= 1e-9, $"{mass.Centre} against {centre}");
    }

    // A distance and its two points, each within 1e-12.
    private static void AssertDistance(double distance, (double X, double Y, double Z) a, (double X, double Y, double Z) b, DistanceInfo info)
    {
        AssertNear(distance, info.Distance);
        foreach (var (expected, actual) in new[] { (a.X, info.PointA.X), (a.Y, info.PointA.Y), (a.Z, info.PointA.Z), (b.X, info.PointB.X), (b.Y, info.PointB.Y), (b.Z, info.PointB.Z) })
            AssertNear(expected, actual);
    }

    private static void AssertNear(double expected, double actual)
        => Assert.True(Math.Abs(actual - expected) <= 1e-12 * Math.Max(1, Math.Abs(expected)), $"{actual} against {expected}");

    private static double Length(DatumPoint p) => Math.Sqrt((p.X * p.X) + (p.Y * p.Y) + (p.Z * p.Z));

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
