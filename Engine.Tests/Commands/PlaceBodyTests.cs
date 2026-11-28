using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Commands;

// ADR-0191 (phase P11.0, TASK-0474): the placement of a body. ADR-0191 names this file in enforced-by. Item 7 gives the list
// of the readers of the geometry, and each test of a reader builds one scene two times: once with the moves of before P11.0,
// a Rotate and then a Translate, and once with one PlaceBody of the same seven numbers. A reader that does not apply the
// placement gives the shape at the origin for the second scene, so its test fails. The other tests hold the command: its
// refusals, the booleans and the moves of a placed body, the regeneration, undo and redo, an open, the events and the cost.
public sealed class PlaceBodyTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-place-" + Guid.NewGuid().ToString("N"));

    public PlaceBodyTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // A general turn and an offset with no exact value in a short decimal.
    private static readonly BodyPlacement Where = new(0.9, 0.3, -0.2, 0.25, 12.5, -3.25, 7.1);

    // A second placement, for the second operand of a boolean.
    private static readonly BodyPlacement There = new(0.7, -0.1, 0.4, 0.2, 14, -2, 6.5);

    // The identifiers of the commands that create the shapes, the same in each scene, so a face has the same name in both.
    private static readonly Guid BoxId = new("0f6d8a51-2b47-4c1e-9a33-5d7e0c4b1a01");
    private static readonly Guid OtherId = new("0f6d8a51-2b47-4c1e-9a33-5d7e0c4b1a02");
    private static readonly Guid FixedId = new("0f6d8a51-2b47-4c1e-9a33-5d7e0c4b1a03");

    // A move of a body to a placement: it gives the identifier of the live body after the move.
    private delegate Task<Guid> Move(DocumentSession session, Guid body, BodyPlacement placement);

    // The moves of before P11.0: a Rotate, then a Translate. Each makes a new body.
    private static async Task<Guid> ByCommands(DocumentSession session, Guid body, BodyPlacement p)
    {
        var turned = Output(await Applied(session, new RotateCommand { BodyId = body, Qw = p.Qw, Qx = p.Qx, Qy = p.Qy, Qz = p.Qz }));
        return Output(await Applied(session, new TranslateCommand { BodyId = turned, Dx = p.Dx, Dy = p.Dy, Dz = p.Dz }));
    }

    // Item 3: one PlaceBody. The body keeps its handle.
    private static async Task<Guid> ByPlacement(DocumentSession session, Guid body, BodyPlacement p)
    {
        Assert.Equal(body, Output(await Applied(session, Place(body, p))));
        return body;
    }

    private static PlaceBodyCommand Place(Guid body, BodyPlacement p)
        => new() { BodyId = body, Qw = p.Qw, Qx = p.Qx, Qy = p.Qy, Qz = p.Qz, Dx = p.Dx, Dy = p.Dy, Dz = p.Dz };

    // The scene with each form of the move, each in a session of its own.
    private static async Task<(T Moved, T Placed)> Both<T>(Func<DocumentSession, Move, Task<T>> scene)
    {
        using var first = new ManifoldGeometryBackend();
        using var second = new ManifoldGeometryBackend();
        return (await scene(NewSession(first), ByCommands), await scene(NewSession(second), ByPlacement));
    }

    private static DocumentSession NewSession(IGeometryBackend backend, IAssetSource? assets = null)
        => new(EngineHosting.CreateDefault(backend, assets: assets));

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name = "bodyId")
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static async Task<T> Read<T>(DocumentSession session, Query query)
    {
        var result = await session.Query<T>(query);
        Assert.True(result.Error is null, $"{query.GetType().Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static Task<Guid> Box(DocumentSession session, Guid id, double x = 40, double y = 30, double z = 10)
        => Applied(session, new CreateBoxCommand { CommandId = id, SizeX = x, SizeY = y, SizeZ = z }).ContinueWith(task => Output(task.Result), TaskScheduler.Default);

    private static void Near(double expected, double actual, double tolerance, string what)
        => Assert.True(Math.Abs(expected - actual) <= tolerance, $"{what}: {expected.ToString("R", CultureInfo.InvariantCulture)} and {actual.ToString("R", CultureInfo.InvariantCulture)}");

    private static void Near(DatumPoint expected, DatumPoint actual, double tolerance, string what)
    {
        Near(expected.X, actual.X, tolerance, what + ".X");
        Near(expected.Y, actual.Y, tolerance, what + ".Y");
        Near(expected.Z, actual.Z, tolerance, what + ".Z");
    }

    private static void Near(DatumAxis expected, DatumAxis actual, double tolerance, string what)
        => Near(new DatumPoint(expected.X, expected.Y, expected.Z), new DatumPoint(actual.X, actual.Y, actual.Z), tolerance, what);

    private static void Near(Aabb expected, Aabb actual, double tolerance, string what)
    {
        Near(new DatumPoint(expected.MinX, expected.MinY, expected.MinZ), new DatumPoint(actual.MinX, actual.MinY, actual.MinZ), tolerance, what + ".Min");
        Near(new DatumPoint(expected.MaxX, expected.MaxY, expected.MaxZ), new DatumPoint(actual.MaxX, actual.MaxY, actual.MaxZ), tolerance, what + ".Max");
    }

    // The box of 40 by 30 by 10, with a move, and its live identifier.
    private static async Task<Guid> MovedBox(DocumentSession session, Move move, BodyPlacement placement)
        => await move(session, await Box(session, BoxId), placement);

    // Item 7, GetTessellation: each vertex of the mesh of the placed box is a vertex of the mesh of the moved box.
    [NativeManifoldFact]
    public async Task The_Triangles_Of_A_Placed_Body_Are_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) => await Read<Tessellation>(session, new GetTessellationQuery { BodyId = await MovedBox(session, move, Where) }));

        Assert.Equal(moved.TriangleCount, placed.TriangleCount);
        for (var i = 0; i < placed.VertexCount; i++)
        {
            var nearest = Enumerable.Range(0, moved.VertexCount).Min(j =>
                Math.Abs(moved.Positions[3 * j] - placed.Positions[3 * i]) + Math.Abs(moved.Positions[(3 * j) + 1] - placed.Positions[(3 * i) + 1])
                + Math.Abs(moved.Positions[(3 * j) + 2] - placed.Positions[(3 * i) + 2]));
            Assert.True(nearest < 1e-9, $"vertex {i} is {nearest} from each vertex of the moved box");
        }
    }

    // Item 7, GetBoundingBox: the box along the axes of the Document.
    [NativeManifoldFact]
    public async Task The_Box_Of_A_Placed_Body_Is_The_Box_Of_The_Moved_Body()
    {
        var (moved, placed) = await Both(async (session, move) => await Read<Aabb>(session, new GetBoundingBoxQuery { BodyId = await MovedBox(session, move, Where) }));

        Near(moved, placed, 1e-9, "box");
        // The box of the shape has its centre at the origin, so the box of the body has its centre at the offset.
        Near(new DatumPoint(Where.Dx, Where.Dy, Where.Dz), new DatumPoint((placed.MinX + placed.MaxX) / 2, (placed.MinY + placed.MaxY) / 2, (placed.MinZ + placed.MaxZ) / 2), 1e-9, "centre");
    }

    // Item 7, GetFaces: each face and each edge in the Document. The exact solid is the same in the two scenes, so the
    // values are equal bit for bit.
    [NativeManifoldFact]
    public async Task The_Faces_Of_A_Placed_Body_Are_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) => await Read<FacesInfo>(session, new GetFacesQuery { BodyId = await MovedBox(session, move, Where) }));

        Assert.Equal(moved.Faces.Count, placed.Faces.Count);
        foreach (var face in moved.Faces)
        {
            var other = Assert.Single(placed.Faces, f => f.Reference == face.Reference);
            Assert.Equal(face.Area, other.Area);
            Assert.Equal(face.Point, other.Point);
            Assert.Equal(face.Normal, other.Normal);
        }

        Assert.Equal(moved.Edges.Sum(edge => edge.Length), placed.Edges.Sum(edge => edge.Length));
    }

    // Item 7, GetFaceAt: a point on the top face of the placed box, with the normal of that face, finds the top face.
    [NativeManifoldFact]
    public async Task A_Pick_On_A_Placed_Body_Finds_The_Face_In_Its_Place()
    {
        var (x, y, z) = Placements.Apply(Where, 3, 4, 5);
        var (nx, ny, nz) = Placements.Direction(Where, 0, 0, 1);
        var (moved, placed) = await Both(async (session, move) => await Read<FaceInfo>(session,
            new GetFaceAtQuery { BodyId = await MovedBox(session, move, Where), X = x, Y = y, Z = z, Nx = nx, Ny = ny, Nz = nz }));

        Assert.Equal("+z", moved.Reference.Element);
        Assert.Equal(moved.Reference, placed.Reference);
    }

    // Item 7, GetMassProperties: the volume and the centre of a box from the exact solid, and of a cylinder from its design
    // curve (MeasureCurve.DesignMass).
    [NativeManifoldFact]
    public async Task The_Mass_Of_A_Placed_Body_Has_Its_Centre_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var box = await Read<MassProperties>(session, new GetMassPropertiesQuery { BodyId = await MovedBox(session, move, Where) });
            var cylinder = Output(await Applied(session, new CreateCylinderCommand { CommandId = OtherId, Radius = 3, Height = 8 }));
            return (box, await Read<MassProperties>(session, new GetMassPropertiesQuery { BodyId = await move(session, cylinder, There) }));
        });

        Assert.Equal(moved.box, placed.box);
        Near(moved.Item2.Volume, placed.Item2.Volume, 1e-9, "the volume of the cylinder");
        Near(moved.Item2.Centre, placed.Item2.Centre, 1e-9, "the centre of the cylinder");
        Near(There.Dx, placed.Item2.Centre.X, 1e-9, "the centre is at the offset");
    }

    // Item 7, GetDistance: the distance and the two nearest points between a placed box and a box that does not move.
    [NativeManifoldFact]
    public async Task The_Distance_To_A_Placed_Body_Is_From_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var body = await MovedBox(session, move, Where);
            var far = Output(await Applied(session, new TranslateCommand { BodyId = await Box(session, FixedId, 4, 4, 4), Dx = 80, Dy = 0, Dz = 0 }));
            return await Read<DistanceInfo>(session, new GetDistanceQuery { BodyIdA = body, BodyIdB = far });
        });

        Near(moved.Distance, placed.Distance, 1e-9, "distance");
        Near(moved.PointA, placed.PointA, 1e-9, "point A");
        Near(moved.PointB, placed.PointB, 1e-9, "point B");
        Assert.True(placed.Distance > 0, "the placed box is apart from the box at the origin");
    }

    // Item 7, GetAngle: the angle between the top face of the placed box and the top face of a box that does not move.
    [NativeManifoldFact]
    public async Task The_Angle_Of_A_Face_Of_A_Placed_Body_Turns_With_It()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var body = await MovedBox(session, move, Where);
            var fixedBox = await Box(session, FixedId, 4, 4, 4);
            return await Read<AngleInfo>(session, new GetAngleQuery
            {
                BodyIdA = body, FeatureIdA = BoxId, ElementA = "+z", BodyIdB = fixedBox, FeatureIdB = FixedId, ElementB = "+z",
            });
        });

        Near(moved.Cosine, placed.Cosine, 1e-12, "cosine");
        Near(moved.Sine, placed.Sine, 1e-12, "sine");
        Assert.True(placed.Cosine < 0.999, "the face turned");
    }

    // Item 7, CheckInterference: a placed box and a box that does not move share a volume, as the moved box does.
    [NativeManifoldFact]
    public async Task The_Interference_Of_A_Placed_Body_Is_From_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var body = await MovedBox(session, move, Where);
            var fixedBox = await Box(session, FixedId, 20, 20, 20);
            await Applied(session, new TranslateCommand { BodyId = fixedBox, Dx = 12, Dy = -3, Dz = 7 });
            return Assert.Single((await Read<InterferenceInfo>(session, new CheckInterferenceQuery())).Pairs);
        });

        Assert.Equal(PairClass.Overlapping, placed.Class);
        Assert.Equal(moved.Class, placed.Class);
        Near(moved.Volume, placed.Volume, 1e-9, "shared volume");
        Near(moved.Box!.Value, placed.Box!.Value, 1e-9, "shared box");
    }

    // Items 4 and 7: a mesh body of an STL file takes a placement, and its check reads its triangles in its place. A Rotate
    // refuses a mesh body (ADR-0112 item 1), so the other scene imports the cube with its points already moved.
    [NativeManifoldFact]
    public async Task A_Placed_Mesh_Body_Is_Checked_In_Its_Place()
    {
        var offset = new BodyPlacement(1, 0, 0, 0, 2, 0, 0);
        async Task<PairCheck> Scene(bool place)
        {
            using var backend = new ManifoldGeometryBackend();
            var assets = new AssetBytes();
            var session = NewSession(backend, assets);
            var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl(Cube(place ? 0 : 2))) }));
            if (place)
                await Applied(session, Place(mesh, offset));
            await Box(session, FixedId, 10, 10, 10);
            return Assert.Single((await Read<InterferenceInfo>(session, new CheckInterferenceQuery())).Pairs);
        }

        var (moved, placed) = (await Scene(false), await Scene(true));
        Assert.Equal(PairClass.Overlapping, placed.Class);
        Near(moved.Volume, placed.Volume, 1e-9, "shared volume");
        Near(moved.Box!.Value, placed.Box!.Value, 1e-9, "shared box");
    }

    // Item 7, GetConnectors: the point and the direction of each face of a placed body.
    [NativeManifoldFact]
    public async Task The_Connectors_Of_A_Placed_Body_Are_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var cylinder = Output(await Applied(session, new CreateCylinderCommand { CommandId = OtherId, Radius = 3, Height = 8 }));
            return await Read<ConnectorsInfo>(session, new GetConnectorsQuery { BodyId = await move(session, cylinder, Where) });
        });

        Assert.Equal(moved.Faces.Count, placed.Faces.Count);
        foreach (var face in moved.Faces)
        {
            var other = Assert.Single(placed.Faces, f => f.Reference == face.Reference);
            Assert.Equal(face.Kind, other.Kind);
            if (face.Point is { } point)
                Near(point, other.Point!, 1e-9, "point of " + face.Reference.Element);
            if (face.Direction is { } direction)
                Near(direction, other.Direction!, 1e-12, "direction of " + face.Reference.Element);
            Near(face.Radius ?? 0, other.Radius ?? 0, 1e-12, "radius of " + face.Reference.Element);
        }
    }

    // Item 7, GetViewLines: the lines of a view of a placed body, which the views of a sheet and its section read too.
    [NativeManifoldFact]
    public async Task The_Lines_Of_A_View_Of_A_Placed_Body_Are_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            await MovedBox(session, move, Where);
            return await Read<ViewLinesInfo>(session, new GetViewLinesQuery { RightX = 1, RightY = 0, RightZ = 0, UpX = 0, UpY = 0, UpZ = 1, EyeX = 0, EyeY = -1, EyeZ = 0 });
        });

        static string Text(ViewLinesInfo info) => string.Join("\n", info.Lines
            .Select(line => FormattableString.Invariant($"{Math.Round(line.FromX, 6)} {Math.Round(line.FromY, 6)} {Math.Round(line.ToX, 6)} {Math.Round(line.ToY, 6)} {line.Visible} {line.Kind}"))
            .Order(StringComparer.Ordinal));
        Assert.NotEmpty(placed.Lines);
        Assert.Equal(Text(moved), Text(placed));
    }

    // Item 7, the export to STEP: the exact solid is the same, so the file is the same but for the identifier of the body.
    [NativeManifoldFact]
    public async Task The_Step_File_Of_A_Placed_Body_Is_The_File_Of_The_Moved_Body()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var body = await MovedBox(session, move, Where);
            var (content, error) = await session.Read((document, _) => Engine.Core.Export.StepExport.Write(document));
            Assert.True(error is null, error?.Message);
            return (Body: body, Text: Encoding.UTF8.GetString(content!));
        });

        Assert.Equal(moved.Text.Replace(moved.Body.ToString(), placed.Body.ToString(), StringComparison.OrdinalIgnoreCase), placed.Text);
    }

    // Item 7, the export to STL: each corner of each triangle in its place, within the rounding of a float.
    [NativeManifoldFact]
    public async Task The_Stl_File_Of_A_Placed_Body_Is_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            await MovedBox(session, move, Where);
            var (content, error) = await session.ReadGeometry(Engine.Core.Export.StlExport.Write);
            Assert.True(error is null, error?.Message);
            return content!;
        });

        Assert.Equal(moved.Length, placed.Length);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(placed.AsSpan(80));
        for (var t = 0; t < count; t++)
        {
            for (var i = 3; i < 12; i++)
            {
                var at = 84 + (50 * t) + (4 * i);
                Near(BinaryPrimitives.ReadSingleLittleEndian(moved.AsSpan(at)), BinaryPrimitives.ReadSingleLittleEndian(placed.AsSpan(at)), 1e-4, $"triangle {t} value {i}");
            }
        }
    }

    // Item 5, a datum on a face: the frame of the face in its place, and a pocket from that datum opens the face in the exact
    // solid, as after a Rotate and a Translate (finding E200 of the codebase review of 2026-11-10).
    [NativeManifoldFact]
    public async Task A_Datum_On_A_Face_Of_A_Placed_Body_Has_The_Frame_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var body = await MovedBox(session, move, Where);
            var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = BoxId, Element = "+z" }), "datumId");
            var frame = (await Read<DatumRecord>(session, new GetDatumQuery { DatumId = datum })).Frame;
            var tool = await Square(session, datum, 5, -2);
            var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = body, SubtrahendBodyId = tool }));
            var top = (await Read<FacesInfo>(session, new GetFacesQuery { BodyId = cut })).Faces.Single(face => face.Reference.Element == "+z");
            return (frame, top.Holes, top.Area);
        });

        Near(moved.frame.Origin, placed.frame.Origin, 1e-12, "origin");
        Near(moved.frame.U, placed.frame.U, 1e-12, "u");
        Near(moved.frame.V, placed.frame.V, 1e-12, "v");
        Near(moved.frame.Normal, placed.frame.Normal, 1e-12, "normal");
        Assert.Equal((1, 1100.0), (placed.Holes, Math.Round(placed.Area, 6)));
        Assert.Equal((moved.Holes, Math.Round(moved.Area, 6)), (placed.Holes, Math.Round(placed.Area, 6)));
    }

    // Item 6: a Subtract, a Union and an Intersect of two placed bodies give the result of the two moved bodies. The result is
    // in the frame of the first body and has its placement.
    [NativeManifoldTheory]
    [InlineData("Subtract")]
    [InlineData("Union")]
    [InlineData("Intersect")]
    public async Task A_Boolean_Of_Two_Placed_Bodies_Is_The_Boolean_Of_The_Moved_Bodies(string kind)
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var first = await MovedBox(session, move, Where);
            var second = await move(session, await Box(session, OtherId, 20, 20, 20), There);
            Command boolean = kind switch
            {
                "Subtract" => new SubtractCommand { MinuendBodyId = first, SubtrahendBodyId = second },
                "Union" => new UnionCommand { FirstBodyId = first, SecondBodyId = second },
                _ => new IntersectCommand { FirstBodyId = first, SecondBodyId = second },
            };
            var result = Output(await Applied(session, boolean));
            var record = await session.Read((document, _) => document.Bodies.Single(body => body.Handle.Id == result));
            return (Record: record,
                Mass: await Read<MassProperties>(session, new GetMassPropertiesQuery { BodyId = result }),
                Box: await Read<Aabb>(session, new GetBoundingBoxQuery { BodyId = result }),
                Faces: (await Read<FacesInfo>(session, new GetFacesQuery { BodyId = result })).Faces.Count);
        });

        Assert.Null(moved.Record.Placement);
        Assert.Equal(Where, placed.Record.Placement);
        Near(moved.Mass.Volume, placed.Mass.Volume, 1e-6, "volume");
        Near(moved.Mass.Centre, placed.Mass.Centre, 1e-6, "centre");
        Near(moved.Box, placed.Box, 1e-6, "box");
        Assert.Equal(moved.Faces, placed.Faces);
    }

    // Item 6: a Translate and a Rotate of a placed body move its shape in its place, and the new body is at the identity.
    [NativeManifoldFact]
    public async Task A_Move_Of_A_Placed_Body_Moves_It_From_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var body = await MovedBox(session, move, Where);
            var turned = Output(await Applied(session, new RotateCommand { BodyId = body, Qw = 1, Qx = 0, Qy = 1, Qz = 0 }));
            var last = Output(await Applied(session, new TranslateCommand { BodyId = turned, Dx = -4, Dy = 2, Dz = 1 }));
            var record = await session.Read((document, _) => document.Bodies.Single(b => b.Handle.Id == last));
            return (record.Placement, Box: await Read<Aabb>(session, new GetBoundingBoxQuery { BodyId = last }),
                Faces: await Read<FacesInfo>(session, new GetFacesQuery { BodyId = last }));
        });

        Assert.Null(placed.Placement);
        Near(moved.Box, placed.Box, 1e-9, "box");
        Assert.Equal(
            moved.Faces.Faces.Select(face => (face.Reference.Element, face.Point, face.Normal)).OrderBy(face => face.Element, StringComparer.Ordinal),
            placed.Faces.Faces.Select(face => (face.Reference.Element, face.Point, face.Normal)).OrderBy(face => face.Element, StringComparer.Ordinal));
    }

    // Items 5 and 6: an edit of an extrude that a placed body and a Translate consume runs the regeneration again from the
    // placement, so the result follows the new distance in its place.
    [NativeManifoldFact]
    public async Task An_Edit_Under_A_Placed_Body_Rebuilds_It_In_Its_Place()
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var square = await Square(session, Datums.XY.Id, 6, 5);
            var body = await move(session, square, Where);
            var last = Output(await Applied(session, new TranslateCommand { BodyId = body, Dx = 1, Dy = 2, Dz = 3 }));
            await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = square, Distance = 9 });
            return (Box: await Read<Aabb>(session, new GetBoundingBoxQuery { BodyId = last }),
                Mass: await Read<MassProperties>(session, new GetMassPropertiesQuery { BodyId = last }));
        });

        Near(moved.Box, placed.Box, 1e-9, "box");
        Near(moved.Mass.Volume, placed.Mass.Volume, 1e-9, "volume");
        Near(12 * 12 * 9, placed.Mass.Volume, 1e-9, "the volume of the new distance");
        Near(moved.Mass.Centre, placed.Mass.Centre, 1e-9, "centre");
    }

    // Finding E246 of the codebase review of 2026-11-28 (TASK-0476): the design mass of a cut by a cylinder (CutMass) of a body
    // that a step made and that has its own PlaceBody. The step is a Subtract by a box, which gives no curved tool, a Subtract by
    // a cylinder, a Union or an Intersect. Until TASK-0476 the case of a Subtract by a box moved the cut two times, and the probe
    // of the review gave the centre x = 203.0 mm in place of 99.6 mm.
    [NativeManifoldTheory]
    [InlineData("box")]
    [InlineData("cylinder")]
    [InlineData("union")]
    [InlineData("intersect")]
    public async Task The_Mass_Of_A_Cut_Of_A_Placed_Result_Of_A_Step_Has_Its_Centre_In_Its_Place(string step)
    {
        var (moved, placed) = await Both(async (session, move) =>
        {
            var first = await Box(session, BoxId, 20, 20, 10);
            var tool = step == "cylinder"
                ? Output(await Applied(session, new CreateCylinderCommand { CommandId = OtherId, Radius = 1.5, Height = 30 }))
                : await Box(session, OtherId, 5, 5, 20);
            var second = Output(await Applied(session, new TranslateCommand { BodyId = tool, Dx = 6, Dy = 6, Dz = step == "intersect" ? 2 : 0 }));
            Command made = step switch
            {
                "union" => new UnionCommand { FirstBodyId = first, SecondBodyId = second },
                "intersect" => new IntersectCommand { FirstBodyId = first, SecondBodyId = second },
                _ => new SubtractCommand { MinuendBodyId = first, SubtrahendBodyId = second },
            };
            var result = await move(session, Output(await Applied(session, made)), new BodyPlacement(1, 0, 0, 0, 100, 0, 0));
            var hole = Output(await Applied(session, new TranslateCommand
            {
                BodyId = Output(await Applied(session, new CreateCylinderCommand { CommandId = FixedId, Radius = 1, Height = 40 })),
                Dx = step == "intersect" ? 106 : 100, Dy = step == "intersect" ? 6 : -4, Dz = 0,
            }));
            var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = result, SubtrahendBodyId = hole }));
            return await Read<MassProperties>(session, new GetMassPropertiesQuery { BodyId = cut });
        });

        Near(moved.Volume, placed.Volume, 1e-6, "volume");
        Near(moved.Centre, placed.Centre, 1e-6, "centre");
    }

    // Item 3: the refusals. The bus refuses an unknown body, a quaternion that gives no turn and an offset that is not finite.
    [Fact]
    public async Task A_Place_With_No_Body_Or_With_A_Bad_Value_Is_Refused()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }));

        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await session.Apply(Place(Guid.NewGuid(), Where))).Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, (await session.Apply(Place(box, Where with { Qw = 0, Qx = 0, Qy = 0, Qz = 0 }))).Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomQuaternionTooSmall, (await session.Apply(Place(box, Where with { Qw = 1e-300, Qx = 0, Qy = 0, Qz = 0 }))).Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, (await session.Apply(Place(box, Where with { Dy = double.NaN }))).Error?.Code);
        Assert.Equal(DiagnosticCodes.GeomInvalidParam, (await session.Apply(Place(box, Where with { Dz = double.PositiveInfinity }))).Error?.Code);
        Assert.Null(await session.Read((document, _) => document.Bodies.Single().Placement));
    }

    // Items 3 and 8: the record keeps its handle, its key and its shape; a placement back to the identity stores null, so the
    // record is the record of a body that never moved. Undo and Redo move the placement as for a rename.
    [Fact]
    public async Task A_Place_Changes_Only_The_Record_And_Undo_And_Redo_Move_It()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }));
        var before = await session.Read((document, _) => document.Bodies.Single());

        await Applied(session, Place(box, Where));
        var after = await session.Read((document, _) => document.Bodies.Single());
        Assert.Equal(before with { Placement = Where }, after);

        await Applied(session, new UndoCommand());
        Assert.Equal(before, await session.Read((document, _) => document.Bodies.Single()));
        await Applied(session, new RedoCommand());
        Assert.Equal(after, await session.Read((document, _) => document.Bodies.Single()));

        await Applied(session, Place(box, BodyPlacement.Identity));
        Assert.Equal(before, await session.Read((document, _) => document.Bodies.Single()));
    }

    // Item 2: the log holds the seven numbers, so an open of the file gives the same records and the same box.
    [NativeManifoldFact]
    public async Task An_Open_Gives_The_Same_Placement()
    {
        var path = Path.Combine(_folder, "place" + DocumentFile.Extension);
        IReadOnlyList<BodyRecord> bodies;
        Aabb box;
        Guid body;
        using (var backend = new ManifoldGeometryBackend())
        {
            var live = NewSession(backend);
            DocumentFile.Create(live, path);
            body = await MovedBox(live, ByPlacement, Where);
            bodies = await live.Read((document, _) => document.Bodies.ToList());
            box = await Read<Aabb>(live, new GetBoundingBoxQuery { BodyId = body });
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var opened = NewSession(backend);
            await DocumentFile.Open(opened, path);
            Assert.Equal(bodies, await opened.Read((document, _) => document.Bodies.ToList()));
            Assert.Equal(box, await Read<Aabb>(opened, new GetBoundingBoxQuery { BodyId = body }));
            opened.Close();
        }
    }

    // Item 8: body.changed carries the seven numbers, and body.created of a boolean carries the placement of its first body;
    // a body at the identity carries null.
    [NativeManifoldFact]
    public async Task The_Events_Of_A_Body_Carry_Its_Placement()
    {
        using var host = new Engine.Api.Http.EngineHost(new ManifoldGeometryBackend());
        var session = host.Session;
        var records = new List<EventRecord>();
        using var subscription = host.Subscribe(records.Add);
        var first = await Box(session, BoxId);
        var second = await Box(session, OtherId, 5, 5, 40);
        await Applied(session, Place(first, Where));
        await Applied(session, new SubtractCommand { MinuendBodyId = first, SubtrahendBodyId = second });

        static IReadOnlyDictionary<string, object?> Payload(EventRecord record) => (IReadOnlyDictionary<string, object?>)record.Payload!;
        var created = records.Where(record => record.Kind == "body.created").Select(Payload).ToList();
        Assert.Null(created[0]["placement"]);
        var changed = Payload(Assert.Single(records, record => record.Kind == "body.changed"));
        var expected = new Dictionary<string, object?> { ["qw"] = Where.Qw, ["qx"] = Where.Qx, ["qy"] = Where.Qy, ["qz"] = Where.Qz, ["dx"] = Where.Dx, ["dy"] = Where.Dy, ["dz"] = Where.Dz };
        Assert.Equal(expected, (IReadOnlyDictionary<string, object?>)changed["placement"]!);
        Assert.Equal(expected, (IReadOnlyDictionary<string, object?>)created[^1]["placement"]!);
    }

    // Item 8: GetBodyInfo gives the placement, so a client reads where a body is before it sends an absolute placement.
    [Fact]
    public async Task The_Information_Of_A_Body_Gives_Its_Placement()
    {
        var session = NewSession(new InProcessMeshBackend());
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }));
        Assert.Null((await Read<BodyInfo>(session, new GetBodyInfoQuery { BodyId = box })).Placement);

        await Applied(session, Place(box, Where));
        Assert.Equal(Where, (await Read<BodyInfo>(session, new GetBodyInfoQuery { BodyId = box })).Placement);
        Assert.Equal(Where, Assert.Single((await Read<BodiesInfo>(session, new GetBodiesQuery())).Bodies).Placement);
    }

    // Item 5: the plan of a preview counts a new placement as a new solid, so the fork reads the measures and the pairs of the
    // body again; a rename keeps the solid.
    [Fact]
    public void A_Preview_Counts_A_New_Placement_As_A_New_Solid()
    {
        var (box, other) = (new BodyRecord(new BodyHandle(BoxId), "Solid"), new BodyRecord(new BodyHandle(OtherId), "Solid"));
        var placed = Engine.Core.Preview.PreviewPlan.Of([box, other], [box with { Placement = Where }, other], [], []);
        Assert.DoesNotContain(BoxId, placed.SameSolid);
        Assert.Contains(BoxId, placed.Measured);
        Assert.Contains(placed.Pairs, pair => pair.A == BoxId || pair.B == BoxId);

        var renamed = Engine.Core.Preview.PreviewPlan.Of([box, other], [box with { Name = "Shelf" }, other], [], []);
        Assert.Contains(BoxId, renamed.SameSolid);
        Assert.Empty(renamed.Pairs);
    }

    // A square of the half size at the origin of the datum, extruded by the distance.
    private static async Task<Guid> Square(DocumentSession session, Guid datum, double half, double distance)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = datum }), "sketchId");
        (double X, double Y)[] corners = [(-half, -half), (half, -half), (half, half), (-half, half)];
        var points = new Guid[4];
        for (var i = 0; i < 4; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < 4; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % 4] });
        return Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = distance }));
    }

    // The 12 triangles of the cube from x to x + 10, each counter-clockwise when seen from outside.
    private static float[][] Cube(float x) =>
    [
        [x, 0, 0, x, 10, 0, x + 10, 10, 0], [x, 0, 0, x + 10, 10, 0, x + 10, 0, 0],
        [x, 0, 10, x + 10, 0, 10, x + 10, 10, 10], [x, 0, 10, x + 10, 10, 10, x, 10, 10],
        [x, 0, 0, x + 10, 0, 0, x + 10, 0, 10], [x, 0, 0, x + 10, 0, 10, x, 0, 10],
        [x, 10, 0, x, 10, 10, x + 10, 10, 10], [x, 10, 0, x + 10, 10, 10, x + 10, 10, 0],
        [x, 0, 0, x, 0, 10, x, 10, 10], [x, 0, 0, x, 10, 10, x, 10, 0],
        [x + 10, 0, 0, x + 10, 10, 0, x + 10, 10, 10], [x + 10, 0, 0, x + 10, 10, 10, x + 10, 0, 10],
    ];

    // A binary STL file of the triangles, with a normal of zero for each.
    private static byte[] Stl(float[][] triangles)
    {
        var bytes = new byte[84 + (50 * triangles.Length)];
        Encoding.ASCII.GetBytes("a test file").CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), (uint)triangles.Length);
        for (var t = 0; t < triangles.Length; t++)
        {
            for (var i = 0; i < 9; i++)
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(84 + (50 * t) + 12 + (i * 4)), triangles[t][i]);
        }

        return bytes;
    }
}
