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
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry;

namespace Engine.Tests.Commands;

// Row K16 (TASK-0433): the commands Union and Intersect, in the form of Subtract (ADR-0181). ADR-0181 names this file in
// enforced-by. Each expected volume, centre and box comes from a computation by hand: a cube of 10 is [-5, 5] on each axis,
// and the measure is GetMassProperties of K15a (ADR-0180), the exact solid of the owned kernel; the box is GetBoundingBox,
// the solid of Manifold.
public sealed class UnionIntersectCommandTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-tests-" + Guid.NewGuid().ToString("N"));

    public UnionIntersectCommandTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

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

    private static async Task<Guid> Cube(DocumentSession session, double dx = 0)
    {
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 }));
        return dx == 0 ? box : Output(await Applied(session, new TranslateCommand { BodyId = box, Dx = dx, Dy = 0, Dz = 0 }));
    }

    private static async Task<T> Answer<T>(DocumentSession session, Query query)
    {
        var result = await session.Query<T>(query);
        Assert.True(result.Error is null, $"{query.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static Task<MassProperties> Mass(DocumentSession session, Guid body)
        => Answer<MassProperties>(session, new GetMassPropertiesQuery { BodyId = body });

    private static Task<Aabb> Box(DocumentSession session, Guid body)
        => Answer<Aabb>(session, new GetBoundingBoxQuery { BodyId = body });

    private static Task<IReadOnlyList<Guid>> Live(DocumentSession session)
        => session.Read<IReadOnlyList<Guid>>((document, _) => [.. document.Bodies.Select(body => body.Handle.Id).Order()]);

    // The live bodies in the order of their identifiers, as Live gives them.
    private static Guid[] Sorted(params IEnumerable<Guid> ids) => [.. ids.Order()];

    private static UnionCommand Union(Guid first, Guid second) => new() { FirstBodyId = first, SecondBodyId = second };

    private static IntersectCommand Intersect(Guid first, Guid second) => new() { FirstBodyId = first, SecondBodyId = second };

    // Items 1, 2 and 4: two cubes that overlap by half. The union is 1500 with the centre (2.5, 0, 0) and the box [-5, 10]
    // on x; the intersection is 500 with the same centre and the box [0, 5] on x. Each command consumes both operands, and
    // the result takes the name and the visibility of the first body.
    [NativeManifoldFact]
    public async Task Two_Cubes_That_Overlap_Give_The_Exact_Union_And_Intersection()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var a = await Cube(session);
        var b = await Cube(session, 5);
        await Applied(session, new RenameBodyCommand { BodyId = a, NewName = "Knob" });
        await Applied(session, new SetBodyVisibilityCommand { BodyId = a, Visible = false });
        var union = Output(await Applied(session, Union(a, b)));

        Assert.Equal(new MassProperties(1500, new DatumPoint(2.5, 0, 0), false), await Mass(session, union));
        Assert.Equal(new Aabb(-5, -5, -5, 10, 5, 5), await Box(session, union));
        Assert.Equal(Sorted(union), await Live(session));
        var record = await session.Read((document, _) => document.Bodies.Single());
        Assert.Equal(("Solid", "Knob", false), (record.Kind, record.Name, record.Visible));

        var c = await Cube(session);
        var d = await Cube(session, 5);
        var common = Output(await Applied(session, Intersect(c, d)));

        Assert.Equal(new MassProperties(500, new DatumPoint(2.5, 0, 0), false), await Mass(session, common));
        Assert.Equal(new Aabb(0, -5, -5, 5, 5, 5), await Box(session, common));
        Assert.Equal(Sorted(union, common), await Live(session));
    }

    // Items 3 and 4: two cubes that touch join into one body of 2000, and two cubes apart join into one body of two parts.
    // Neither pair shares a volume, so each intersection is refused with E-GEOM-NATIVE-OP, as a cut that leaves nothing,
    // and both bodies stay live. The same holds for a box that touches a part of a face.
    [NativeManifoldFact]
    public async Task Touching_And_Apart_Cubes_Join_And_Share_No_Volume()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        foreach (var (dx, centre) in new[] { (10.0, 5.0), (20.0, 10.0) })
        {
            var a = await Cube(session);
            var b = await Cube(session, dx);
            var refused = await session.Apply(Intersect(a, b));
            Assert.Equal(CommandStatus.Rejected, refused.Status);
            Assert.Equal(DiagnosticCodes.GeomNativeOp, refused.Error!.Code);
            Assert.Contains(a, await Live(session));
            Assert.Contains(b, await Live(session));

            var union = Output(await Applied(session, Union(a, b)));
            Assert.Equal(new MassProperties(2000, new DatumPoint(centre, 0, 0), false), await Mass(session, union));
            Assert.Equal(new Aabb(-5, -5, -5, dx + 5, 5, 5), await Box(session, union));
        }

        // Item 3: a box of 2 on the face +y of a cube, at [2, 4] x [5, 7] x [-1, 1], touches a part of the face. Manifold gives
        // a flat solid of no volume there, which it does not call empty, so the exact kernel refuses the intersection first.
        var cube = await Cube(session);
        var small = Output(await Applied(session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 }));
        var placed = Output(await Applied(session, new TranslateCommand { BodyId = small, Dx = 3, Dy = 6, Dz = 0 }));
        var flat = await session.Apply(Intersect(cube, placed));
        Assert.Equal(DiagnosticCodes.GeomNativeOp, flat.Error?.Code);
        Assert.Contains("owned kernel", flat.Error!.Message, StringComparison.Ordinal);
        Assert.Contains(cube, await Live(session));
    }

    // Item 2: each refusal of Subtract, with its code: a body that is not live (first or second), one body twice, a mesh
    // body of an STL file (first or second), and a backend with no booleans.
    [NativeManifoldFact]
    public async Task Each_Refusal_Of_Subtract_Holds_For_Union_And_Intersect()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = NewSession(backend, assets);
        var a = await Cube(session);
        var b = await Cube(session, 5);
        var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Triangle()) }));
        var managed = NewSession(new InProcessMeshBackend());
        var (p, q) = (await Cube(managed), await Cube(managed));

        foreach (var make in new Func<Guid, Guid, Command>[] { Union, Intersect })
        {
            Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await session.Apply(make(Guid.NewGuid(), b))).Error?.Code);
            Assert.Equal(DiagnosticCodes.GeomBodyNotFound, (await session.Apply(make(a, Guid.NewGuid()))).Error?.Code);
            Assert.Equal(DiagnosticCodes.GeomInvalidParam, (await session.Apply(make(a, a))).Error?.Code);
            Assert.Equal(DiagnosticCodes.MeshUnsupported, (await session.Apply(make(mesh, a))).Error?.Code);
            Assert.Equal(DiagnosticCodes.MeshUnsupported, (await session.Apply(make(a, mesh))).Error?.Code);
            Assert.Equal(DiagnosticCodes.GeomCapMissing, (await managed.Apply(make(p, q))).Error?.Code);
        }

        Assert.Equal(Sorted(a, b, mesh), await Live(session));
        Assert.Equal(Sorted(p, q), await Live(managed));
    }

    // A binary STL file of one triangle, which makes a mesh body (ADR-0083).
    private static byte[] Triangle()
    {
        float[] corners = [0f, 0f, 0f, 10f, 0f, 0f, 0f, 10f, 0f];
        var bytes = new byte[84 + 50];
        Encoding.ASCII.GetBytes("a test file").CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), 1);
        for (var i = 0; i < 9; i++)
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(84 + 12 + (i * 4)), corners[i]);
        return bytes;
    }

    // Item 6: the faces of the result keep the names of the faces of the two bodies (ADR-0058, ADR-0059). A datum on the
    // face +x of the first cube, which lies inside the second cube, gets W-DATUM-REFERENCE-FAILED after the union, and a datum
    // on the face -x keeps no fault. After the intersection the opposite holds. An undo removes the fault, and a redo gives
    // it again.
    [NativeManifoldFact]
    public async Task The_Faces_Of_The_Result_Keep_Their_Names_And_A_Datum_On_A_Gone_Face_Gets_A_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        foreach (var unite in new[] { true, false })
        {
            var a = await Cube(session);
            var b = await Cube(session, 5);
            var feature = (await session.Read((document, _) => document.Log[^1].Command) as TranslateCommand)!.BodyId;
            var plus = Output(await Applied(session, new CreateDatumCommand { FeatureId = a, Element = "+x" }), "datumId");
            var minus = Output(await Applied(session, new CreateDatumCommand { FeatureId = a, Element = "-x" }), "datumId");
            var result = await Applied(session, unite ? Union(a, b) : Intersect(a, b));
            var body = Output(result);

            var names = (await Answer<FacesInfo>(session, new GetFacesQuery { BodyId = body })).Faces
                .Select(face => (face.Reference.FeatureId == a ? "A" : face.Reference.FeatureId == feature ? "B" : "?") + face.Reference.Element)
                .Order(StringComparer.Ordinal).ToArray();
            string[] expected = unite
                ? ["A+y", "A+z", "A-x", "A-y", "A-z", "B+x", "B+y", "B+z", "B-y", "B-z"]
                : ["A+x", "B-x"];
            Assert.True(unite ? names.SequenceEqual(expected) : expected.All(names.Contains) && names.Length == 6 && !names.Contains("A-x") && !names.Contains("B+x"), string.Join(" ", names));

            var (gone, kept) = unite ? (plus, minus) : (minus, plus);
            Assert.Equal(DiagnosticCodes.DatumReferenceFailed, Assert.Single(result.Diagnostics).Code);
            Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Datum(session, gone)).Fault?.Code);
            Assert.Null((await Datum(session, kept)).Fault);

            await Applied(session, new UndoCommand());
            Assert.Null((await Datum(session, gone)).Fault);
            await Applied(session, new RedoCommand());
            Assert.Equal(DiagnosticCodes.DatumReferenceFailed, (await Datum(session, gone)).Fault?.Code);

            // A new datum on the gone face is refused, and a new datum on the kept face has its outward normal: a union and an
            // intersection turn no face, as the minuend of a cut keeps its normals.
            Assert.Equal(DiagnosticCodes.DatumReferenceInvalid, (await session.Apply(new CreateDatumCommand { FeatureId = a, Element = unite ? "+x" : "-x" })).Error?.Code);
            var again = Output(await Applied(session, new CreateDatumCommand { FeatureId = a, Element = unite ? "-x" : "+x" }), "datumId");
            Assert.Equal(unite ? -1 : 1, (await Datum(session, again)).Frame.Normal.X);
        }
    }

    // ADR-0181 item 6: the result keeps the curve of each curved side of either body, as a cut does (ADR-0143 item 1).
    [NativeManifoldFact]
    public async Task The_Result_Keeps_The_Curve_Of_A_Cylinder()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        foreach (var unite in new[] { true, false })
        {
            var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 2, Height = 20 }));
            var cube = await Cube(session);
            var body = Output(await Applied(session, unite ? Union(cube, cylinder) : Intersect(cube, cylinder)));
            var curves = await session.Read((document, _) => Engine.Core.Features.KernelProjection.Curves(document, body));
            Assert.Contains(new Engine.Kernel.FaceTag(cylinder, "side"), curves.Keys);
        }
    }

    private static Task<DatumRecord> Datum(DocumentSession session, Guid id)
        => session.Read((document, _) => document.Datums.Single(datum => datum.Id == id));

    // ADR-0021 and ADR-0031: an undo of a union or an intersection gives back both operands and removes the result; a redo
    // gives the result again with the same volume.
    [NativeManifoldFact]
    public async Task An_Undo_Gives_Back_Both_Operands_And_A_Redo_Gives_The_Result_Again()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        foreach (var (unite, volume) in new[] { (true, 1500.0), (false, 500.0) })
        {
            var a = await Cube(session);
            var b = await Cube(session, 5);
            var before = await Live(session);
            var result = Output(await Applied(session, unite ? Union(a, b) : Intersect(a, b)));

            await Applied(session, new UndoCommand());
            Assert.Equal(before, await Live(session));
            await Applied(session, new RedoCommand());
            Assert.Equal(Sorted(before.Except([a, b]).Append(result)), await Live(session));
            Assert.Equal(volume, (await Mass(session, result)).Volume);
        }
    }

    // ADR-0055 and ADR-0181 item 7: a new distance of an extrude that a union and an intersection read rebuilds each result.
    // The extrude of the square (0, 0) to (10, 10) by 10 and the cube [-5, 5] share the cube [0, 5]: the union is
    // 1000 + 1000 - 125 = 1875 with the centre (5000 - 312.5) / 1875 = 2.5 on each axis. By 20 the union is 2875 and the box
    // ends at z = 20. The intersection stays 125 at (2.5, 2.5, 2.5).
    [NativeManifoldFact]
    public async Task A_New_Distance_Of_An_Extrude_Rebuilds_The_Union_And_The_Intersection()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var (union, unionExtrude) = await ExtrudeWithCube(session, unite: true);
        var (common, commonExtrude) = await ExtrudeWithCube(session, unite: false);
        Assert.Equal(new MassProperties(1875, new DatumPoint(2.5, 2.5, 2.5), false), await Mass(session, union));
        Assert.Equal(new MassProperties(125, new DatumPoint(2.5, 2.5, 2.5), false), await Mass(session, common));

        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = unionExtrude, Distance = 20 });
        await Applied(session, new SetExtrudeDistanceCommand { ExtrudeId = commonExtrude, Distance = 20 });

        Assert.Equal(2875, (await Mass(session, union)).Volume);
        Assert.Equal(new Aabb(-5, -5, -5, 10, 10, 20), await Box(session, union));
        Assert.Equal(new MassProperties(125, new DatumPoint(2.5, 2.5, 2.5), false), await Mass(session, common));
        Assert.Equal(new Aabb(0, 0, 0, 5, 5, 5), await Box(session, common));
        Assert.All(await session.Read((document, _) => document.Bodies.ToList()), body => Assert.Null(body.Fault));
    }

    private static async Task<(Guid Result, Guid Extrude)> ExtrudeWithCube(DocumentSession session, bool unite)
    {
        var sketch = Output(await Applied(session, new CreateSketchCommand { DatumId = Datums.XY.Id }), "sketchId");
        (double X, double Y)[] corners = [(0, 0), (10, 0), (10, 10), (0, 10)];
        var points = new Guid[corners.Length];
        for (var i = 0; i < points.Length; i++)
            points[i] = Output(await Applied(session, new AddSketchPointCommand { SketchId = sketch, X = corners[i].X, Y = corners[i].Y }), "pointId");
        for (var i = 0; i < points.Length; i++)
            await Applied(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Length] });
        var extrude = Output(await Applied(session, new ExtrudeCommand { SketchId = sketch, Distance = 10 }));
        var cube = await Cube(session);
        return (Output(await Applied(session, unite ? Union(extrude, cube) : Intersect(cube, extrude))), extrude);
    }

    // ADR-0012 §4 and ADR-0014: a replay of the log on two new native backends gives the same bodies and the same boxes as
    // the session, and a save and an open give the same Document with the same measures.
    [NativeManifoldFact]
    public async Task A_Replay_And_An_Open_Give_The_Same_Bodies()
    {
        var path = Path.Combine(_folder, "booleans" + DocumentFile.Extension);
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        DocumentFile.Create(session, path);
        var union = Output(await Applied(session, Union(await Cube(session), await Cube(session, 5))));
        var common = Output(await Applied(session, Intersect(await Cube(session), await Cube(session, 5))));
        var bodies = await session.Read((document, _) => document.Bodies.ToList());
        var log = await session.Read((document, _) => document.Log.Select(entry => entry.Command).ToList());
        var boxes = new[] { await Box(session, union), await Box(session, common) };
        session.Close();

        foreach (var _ in new[] { 1, 2 })
        {
            using var replayBackend = new ManifoldGeometryBackend();
            var replay = await Replay.ReplayLog(log, EngineHosting.CreateDefault(replayBackend).CommandRegistry, replayBackend);
            Assert.Equal(bodies, replay.Document.Bodies);
            var query = replayBackend.TryGet<IGeometryQuery>()!;
            Assert.Equal(boxes, new[] { union, common }.Select(id => query.GetBoundingBox(new BodyHandle(bodies.Single(body => body.Handle.Id == id).GeometryKey))));
        }

        using var openBackend = new ManifoldGeometryBackend();
        var opened = NewSession(openBackend);
        Assert.Empty(await DocumentFile.Open(opened, path));
        Assert.Equal(bodies, await opened.Read((document, _) => document.Bodies.ToList()));
        Assert.Equal(new MassProperties(1500, new DatumPoint(2.5, 0, 0), false), await Mass(opened, union));
        Assert.Equal(new MassProperties(500, new DatumPoint(2.5, 0, 0), false), await Mass(opened, common));
        Assert.Equal(boxes, new[] { await Box(opened, union), await Box(opened, common) });
        opened.Close();
    }

    // Row K16 and the bookcase of K10a: the union of the five panels of the open case is one body of
    // 2 (18 × 400 × 800) + 2 (1164 × 394 × 18) + 1164 × 6 × 800 = 33,617,376 mm³ in the box [-300, 900] × [-200, 200] ×
    // [600, 1400], with the name of the first panel. The panels touch and do not overlap, so the volume is the sum. Module 1
    // and module 7 touch on the plane z = 1000, so they share no volume and their intersection is refused.
    [NativeManifoldFact]
    public async Task The_Panels_Of_The_Bookcase_Join_Into_One_Case_And_Two_Modules_Share_No_Volume()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var bodies = await BookcaseCase.Build(session);

        var panels = new[] { "Cote gauche", "Cote droit", "Bas", "Haut", "Fond" }.Select(name => bodies[name]).ToList();
        var case_ = panels[0];
        foreach (var panel in panels.Skip(1))
            case_ = Output(await Applied(session, Union(case_, panel)));

        var mass = await Mass(session, case_);
        Assert.Equal(33_617_376, mass.Volume);
        Assert.False(mass.Facets);
        Assert.Equal(new Aabb(-300, -200, 600, 900, 200, 1400), await Box(session, case_));
        Assert.Equal("Cote gauche", await session.Read((document, _) => document.Bodies.Single(body => body.Handle.Id == case_).Name));
        Assert.Equal(BookcaseCase.Parts.Count - 4, (await Live(session)).Count);

        var refused = await session.Apply(Intersect(bodies["Module 1 - volume indicatif"], bodies["Module 7 - volume indicatif"]));
        Assert.Equal(DiagnosticCodes.GeomNativeOp, refused.Error?.Code);
    }
}
