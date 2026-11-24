using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Assembly;
using Engine.Tests.Geometry;
using Xunit.Abstractions;

namespace Engine.Tests.Queries;

// Row K11 (TASK-0436): the pair check CheckInterference at one position. ADR-0182 names this file in enforced-by. Each
// expected value comes from a computation by hand or from the boxes of a table, not from the code under test. The tests use
// the native backend, because only that backend moves a body and gives the triangles of a mesh body.
public sealed class CheckInterferenceQueryTests(ITestOutputHelper output)
{
    // TASK-0461 (finding E241 of the codebase review of 2026-11-23, ADR-0182 item 8): each boolean of the kernel in the check
    // gives the token of the query, so a cancel and the stop of a host that waits for a fork do not wait for a walk with no
    // token. A test cannot time a cancel inside one walk, so this test reads the calls in the source of the handler. Until
    // TASK-0461 the subtract of a void gave no token.
    [Fact]
    public void Each_Boolean_Of_The_Check_Gives_The_Token_Of_The_Query()
    {
        var source = Engine.Tests.Governance.RepositoryFiles.Read("Engine.Core", "Queries", "CheckInterferenceQueryHandler.cs");
        var calls = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .Where(call => call.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax
            {
                Expression: Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax { Identifier.ValueText: "PlanarBoolean" },
            })
            .ToList();

        Assert.True(calls.Count >= 2, $"The handler has {calls.Count} calls of PlanarBoolean; the scan found too few.");
        Assert.All(calls, call => Assert.True(
            call.ArgumentList.Arguments.Count == 3 && call.ArgumentList.Arguments[2].Expression.ToString() == "ct",
            $"The call {call} gives no token of the query."));
    }

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result)
        => result.Outputs.TryGet<Guid>("bodyId", out var id) ? id : throw new InvalidOperationException("No output bodyId.");

    private static async Task<Guid> Box(DocumentSession session, double size, double dx = 0, double dy = 0, double dz = 0)
    {
        var box = Output(await Applied(session, new CreateBoxCommand { SizeX = size, SizeY = size, SizeZ = size }));
        return dx == 0 && dy == 0 && dz == 0 ? box : Output(await Applied(session, new TranslateCommand { BodyId = box, Dx = dx, Dy = dy, Dz = dz }));
    }

    private static string Ids(params Guid[] bodies) => string.Join(",", bodies);

    private static async Task<InterferenceInfo> Check(DocumentSession session, string? bodies = null, string? intended = null)
    {
        var result = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = bodies, IntendedPairs = intended });
        Assert.True(result.Error is null, $"CheckInterference: {result.Error?.Code} {result.Error?.Message}");
        return result.Result!;
    }

    private static async Task<PairCheck> Pair(DocumentSession session, Guid a, Guid b, string? intended = null)
        => Assert.Single((await Check(session, Ids(a, b), intended)).Pairs);

    private static async Task<string?> Refusal(DocumentSession session, string? bodies, string? intended = null)
        => (await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = bodies, IntendedPairs = intended })).Error?.Code;

    // Item 3: two boxes of 10 at the origin and at (5, 3, 0) share the box from (0, -2, -5) to (5, 5, 5), 5 by 7 by 10 = 350
    // mm³. A box of 2 inside a box of 10 shares its whole volume, 8 mm³. Neither pair is intended, so each is a fault.
    [NativeManifoldFact]
    public async Task Two_Boxes_That_Overlap_Give_The_Exact_Volume_And_Box_Of_The_Overlap()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var a = await Box(session, 10);
        var b = await Box(session, 10, 5, 3, 0);
        var inner = await Box(session, 2);

        Assert.Equal(new PairCheck(a, b, PairClass.Overlapping, 350, new Aabb(0, -2, -5, 5, 5, 5), 0, null, null, false, true, false, CheckBasis.Guaranteed),
            await Pair(session, a, b));
        Assert.Equal(new PairCheck(inner, a, PairClass.Overlapping, 8, new Aabb(-1, -1, -1, 1, 1, 1), 0, null, null, false, true, false, CheckBasis.Guaranteed),
            await Pair(session, inner, a));
    }

    // Item 3: a box of 10 at the origin touches a box of 10 at (10, 0, 0) on a face, at (10, 5, 0) on a part of a face, at
    // (10, 10, 0) on an edge and at (10, 10, 10) on a point: no volume, no box, a gap of 0 and one common point. At (30, 40, 0)
    // it is apart: the gap is √(20² + 30²) = √1300, from (5, 5, z) to (25, 35, z).
    [NativeManifoldFact]
    public async Task Two_Boxes_That_Touch_On_A_Face_An_Edge_Or_A_Point_Or_Are_Apart()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var a = await Box(session, 10);

        foreach (var (dx, dy, dz) in new[] { (10.0, 0.0, 0.0), (10.0, 5.0, 0.0), (10.0, 10.0, 0.0), (10.0, 10.0, 10.0) })
        {
            var touch = await Pair(session, a, await Box(session, 10, dx, dy, dz));
            Assert.Equal((PairClass.Touching, 0.0, (Aabb?)null, 0.0, false, false), (touch.Class, touch.Volume, touch.Box, touch.Gap, touch.Fault, touch.MeshOnly));
            Assert.Equal(touch.PointA, touch.PointB);
            Assert.Equal(5, touch.PointA!.X);
        }

        var apart = await Pair(session, a, await Box(session, 10, 30, 40, 0));
        Assert.Equal((PairClass.Apart, 0.0, (Aabb?)null, Math.Sqrt(1300), false), (apart.Class, apart.Volume, apart.Box, apart.Gap, apart.Fault));
        Assert.Equal((5.0, 5.0, 25.0, 35.0), (apart.PointA!.X, apart.PointA.Y, apart.PointB!.X, apart.PointB.Y));
        Assert.Equal(apart.PointA.Z, apart.PointB.Z);
    }

    // Items 2 and 5: with no list, each live body is checked, each pair once in the order of the Document. An intended pair
    // is marked in either order: an intended contact that touches, an intended contact that is apart, and an intended
    // overlap are not faults; an overlap that is not intended is the one fault. The check writes nothing: the version, the
    // log and the bodies stay.
    [NativeManifoldFact]
    public async Task Each_Pair_Of_The_Live_Bodies_Is_Checked_Once_And_An_Intended_Pair_Is_Not_A_Fault()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var a = await Box(session, 10);
        var touching = await Box(session, 10, 10, 0, 0);
        var apart = await Box(session, 10, 0, 50, 0);
        var overlapping = await Box(session, 10, 0, 0, 5);
        var before = await session.Read((document, _) => (document.Version, document.Log.Count, document.Bodies.Count));

        var all = await Check(session, intended: $"{touching}:{a},{a}:{apart}");
        var order = new[] { a, touching, apart, overlapping };
        Assert.Equal(
            [.. from i in Enumerable.Range(0, 4) from j in Enumerable.Range(i + 1, 3 - i) select (order[i], order[j])],
            all.Pairs.Select(pair => (pair.BodyIdA, pair.BodyIdB)).ToArray());
        PairCheck Of(Guid x, Guid y) => all.Pairs.Single(pair => pair.BodyIdA == x && pair.BodyIdB == y);
        Assert.Equal((PairClass.Touching, true, false), (Of(a, touching).Class, Of(a, touching).Intended, Of(a, touching).Fault));
        Assert.Equal((PairClass.Apart, true, false, 40.0), (Of(a, apart).Class, Of(a, apart).Intended, Of(a, apart).Fault, Of(a, apart).Gap));
        Assert.Equal((PairClass.Overlapping, false, true, 500.0), (Of(a, overlapping).Class, Of(a, overlapping).Intended, Of(a, overlapping).Fault, Of(a, overlapping).Volume));
        Assert.Equal([Of(a, overlapping)], all.Pairs.Where(pair => pair.Fault));
        Assert.Equal(before, await session.Read((document, _) => (document.Version, document.Log.Count, document.Bodies.Count)));

        var declared = Assert.Single((await Check(session, Ids(overlapping, a), $"{a}:{overlapping}")).Pairs);
        Assert.Equal((overlapping, a, PairClass.Overlapping, true, false), (declared.BodyIdA, declared.BodyIdB, declared.Class, declared.Intended, declared.Fault));
    }

    // Item 4: a body with a curved face is checked on the facets of its solid, with the mark MeshOnly. A cylinder of radius 5
    // and height 10 at the origin and a plate of 20 by 20 by 2 at the origin share the prism of the facets from z -1 to 1,
    // which is 2 / 10 of the volume of the facets of the cylinder, a little less than the volume 50 π of the curve.
    [NativeManifoldFact]
    public async Task A_Curved_Body_Is_Checked_On_Its_Facets_With_The_Mark_Of_A_Mesh()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var cylinder = Output(await Applied(session, new CreateCylinderCommand { Radius = 5, Height = 10 }));
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 2 }));
        var far = await Box(session, 2, 0, 0, 20);
        var solid = await session.Read((document, _) => KernelProjection.OfRecord(document, document.Bodies.Single(body => body.Handle.Id == cylinder)).Solid!);
        var facets = solid.Volume().ToDouble();
        var xs = solid.Polygons.SelectMany(polygon => polygon.Vertices).Select(v => v.X.ToDouble()).ToList();
        var ys = solid.Polygons.SelectMany(polygon => polygon.Vertices).Select(v => v.Y.ToDouble()).ToList();

        var overlap = await Pair(session, cylinder, plate);
        Assert.Equal((PairClass.Overlapping, true, CheckBasis.Guaranteed), (overlap.Class, overlap.MeshOnly, overlap.Basis));
        Assert.True(Math.Abs(overlap.Volume - (facets / 5)) <= 1e-12 * facets, $"{overlap.Volume} against {facets / 5}");
        Assert.True(overlap.Volume < 50 * Math.PI && overlap.Volume > 49 * Math.PI, $"{overlap.Volume}");
        Assert.Equal(new Aabb(xs.Min(), ys.Min(), -1, xs.Max(), ys.Max(), 1), overlap.Box);

        var gap = await Pair(session, cylinder, far);
        Assert.Equal((PairClass.Apart, 14.0, true), (gap.Class, gap.Gap, gap.MeshOnly));
        Assert.False((await Pair(session, plate, far)).MeshOnly);
    }

    // Item 4: a closed mesh body of an STL file is checked on its triangles, with the mark MeshOnly. The cube of the file is
    // from 0 to 10 on each axis: a box of 10 at (15, 5, 5) touches it, and a box of 4 at (5, 5, 5) lies inside it, so they share
    // 64 mm³ in the box from 3 to 7. The two boxes are apart by 3, with no mark. A mesh of one triangle and a cube with a
    // missing triangle close no solid and are refused.
    [NativeManifoldFact]
    public async Task A_Closed_Mesh_Body_Is_Checked_On_Its_Triangles_With_The_Mark_Of_A_Mesh()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets));
        var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl(Cube)) }));
        var beside = await Box(session, 10, 15, 5, 5);
        var inside = await Box(session, 4, 5, 5, 5);

        var pairs = (await Check(session)).Pairs;
        Assert.Equal(3, pairs.Count);
        var touch = pairs.Single(pair => pair.BodyIdA == mesh && pair.BodyIdB == beside);
        Assert.Equal((PairClass.Touching, 0.0, true), (touch.Class, touch.Gap, touch.MeshOnly));
        var overlap = pairs.Single(pair => pair.BodyIdA == mesh && pair.BodyIdB == inside);
        Assert.Equal((PairClass.Overlapping, 64.0, (Aabb?)new Aabb(3, 3, 3, 7, 7, 7), true), (overlap.Class, overlap.Volume, overlap.Box, overlap.MeshOnly));
        var boxes = pairs.Single(pair => pair.BodyIdA == beside && pair.BodyIdB == inside);
        Assert.Equal((PairClass.Apart, 3.0, false), (boxes.Class, boxes.Gap, boxes.MeshOnly));

        var triangle = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl([[0, 0, 0, 10, 0, 0, 0, 10, 0]])) }));
        Assert.Equal(DiagnosticCodes.MeshUnsupported, await Refusal(session, Ids(triangle, beside)));

        // The cube with no first triangle: the two triangles of z = 0 hold the origin, so the sum of the volume stays 1000,
        // and only the test of the edges finds that the solid is not closed.
        var open = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl(Cube[1..])) }));
        Assert.Equal(DiagnosticCodes.MeshUnsupported, await Refusal(session, Ids(open, beside)));
    }

    // Item 7: a body that is not live gives E-GEOM-BODY-NOT-FOUND; a list that is not valid gives E-CHECK-PAIR-INVALID.
    [NativeManifoldFact]
    public async Task A_Body_That_Is_Not_Live_Or_A_List_That_Is_Not_Valid_Is_Refused()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var (a, b, c) = (await Box(session, 1), await Box(session, 1, 5, 0, 0), await Box(session, 1, 10, 0, 0));
        var invalid = DiagnosticCodes.CheckPairInvalid;

        Assert.Equal(DiagnosticCodes.GeomBodyNotFound, await Refusal(session, Ids(a, Guid.NewGuid())));
        foreach (var bodies in new[] { "", " ", "a body", Ids(a), Ids(a, a), $"{a},,{b}", $"{a};{b}" })
            Assert.Equal(invalid, await Refusal(session, bodies));
        foreach (var pairs in new[] { "", $"{a}", $"{a}:{a}", $"{a}:{c}", $"{a}:{b}:{c}", $"{a}:{b},{b}:{a}", $"{a}:x" })
            Assert.Equal(invalid, await Refusal(session, Ids(a, b), pairs));
        Assert.Equal(invalid, await Refusal(session, null, $"{a}:{Guid.NewGuid()}"));
        Assert.Equal(3, (await Check(session, $" {a} , {b},{c} ", $" {a} : {c} ")).Pairs.Count);
    }

    // Row K11 on the bookcase of K10a: 19 bodies give 171 pairs. Each body is a box of the table of BookcaseCase, so each
    // class and each gap follows from the intervals of two boxes: a shared length on each axis is an overlap, a shared length
    // of zero on one axis or more is a contact, and the lengths between the boxes give the gap. No pair overlaps.
    [NativeManifoldFact]
    public async Task The_Bookcase_Has_No_Overlap_And_Each_Contact_And_Gap_Of_Its_Boxes()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var bodies = await BookcaseCase.Build(session);
        var names = bodies.ToDictionary(pair => pair.Value, pair => pair.Key);
        var parts = BookcaseCase.Parts.ToDictionary(part => part.Name);

        var watch = Stopwatch.StartNew();
        var pairs = (await Check(session)).Pairs;
        output.WriteLine($"171 pairs of the bookcase: {watch.Elapsed.TotalSeconds:F2} s.");

        Assert.Equal(171, pairs.Count);
        foreach (var pair in pairs)
        {
            var (expected, gap) = Expected(parts[names[pair.BodyIdA]], parts[names[pair.BodyIdB]]);
            Assert.True(expected == pair.Class && gap == pair.Gap && !pair.MeshOnly && !pair.Fault,
                $"{names[pair.BodyIdA]} and {names[pair.BodyIdB]}: {pair.Class} {pair.Gap} against {expected} {gap}");
        }

        var touching = pairs.Where(pair => pair.Class == PairClass.Touching).Select(pair => $"{names[pair.BodyIdA]} | {names[pair.BodyIdB]}").ToList();
        output.WriteLine($"{touching.Count} pairs touch:\n{string.Join("\n", touching)}");
        Assert.DoesNotContain(pairs, pair => pair.Class == PairClass.Overlapping);
        Assert.Contains("Module 3 - volume indicatif | Haut", touching);
        Assert.Contains("Module 6 - volume indicatif | Bas", touching);
        Assert.Contains("Bas | Fond", touching);
        PairCheck Of(string x, string y) => pairs.Single(pair => (names[pair.BodyIdA], names[pair.BodyIdB]) == (x, y));
        Assert.Equal((PairClass.Apart, 764.0), (Of("Bas", "Haut").Class, Of("Bas", "Haut").Gap));
        Assert.Equal(1800, Of("Module 1 - volume indicatif", "Module 2 - volume indicatif").Gap);
    }

    // TASK-0438 (finding T188 of the second codebase review of 2026-11-21): the boxes of two bodies share a volume while the
    // solids only touch or are apart, so the intersection of the kernel gives no solid and the nearest pair decides. The plate
    // of 40 by 40 by 10 at the origin has a pocket of 10 by 10 by 5 from z 0 to 5. A box of the size of the pocket at its place
    // touches the floor and the four walls. A box of 4 by 4 by 2 at (0, 0, 2) is from z 1 to 3, so it is 1 above the floor
    // and 3 from each wall: the gap is 1, from a point of the floor at z 0 to the point above it at z 1. The box of the plate
    // holds both boxes.
    [NativeManifoldFact]
    public async Task A_Box_In_A_Pocket_Touches_Or_Is_Apart_While_The_Boxes_Of_The_Bodies_Share_A_Volume()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 40, SizeZ = 10 }));
        var pocket = Output(await Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 5 }));
        pocket = Output(await Applied(session, new TranslateCommand { BodyId = pocket, Dx = 0, Dy = 0, Dz = 2.5 }));
        plate = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = pocket }));
        var fit = Output(await Applied(session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 5 }));
        fit = Output(await Applied(session, new TranslateCommand { BodyId = fit, Dx = 0, Dy = 0, Dz = 2.5 }));
        var small = Output(await Applied(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 2 }));
        small = Output(await Applied(session, new TranslateCommand { BodyId = small, Dx = 0, Dy = 0, Dz = 2 }));

        var touch = await Pair(session, plate, fit);
        Assert.Equal((PairClass.Touching, 0.0, 0.0, (Aabb?)null, false), (touch.Class, touch.Gap, touch.Volume, touch.Box, touch.Fault));
        Assert.Equal(touch.PointA, touch.PointB);

        var apart = await Pair(session, plate, small);
        Assert.Equal((PairClass.Apart, 1.0, 0.0, (Aabb?)null, false), (apart.Class, apart.Gap, apart.Volume, apart.Box, apart.Fault));
        Assert.Equal((0.0, 1.0), (apart.PointA!.Z, apart.PointB!.Z));
        Assert.Equal((apart.PointA.X, apart.PointA.Y), (apart.PointB.X, apart.PointB.Y));
    }

    // TASK-0438 (finding E233; Q5 A of the second codebase review of 2026-11-21): ADR-0182 item 8. A sphere has 3968
    // triangles, so two spheres need 3968² = 15,745,024 products of pieces, above the limit of 100,000: the check is refused
    // before its first pair, and the message gives the work. Before TASK-0438 it took 101 s on the computer of the owner. A
    // sphere and a plate need 3968 × 6 = 23,808, under the limit, and the check gives the overlap.
    [NativeManifoldFact]
    public async Task A_Check_Whose_Work_Is_Above_The_Limit_Is_Refused_Before_Its_First_Pair()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 10 }));
        var far = Output(await Applied(session, new CreateSphereCommand { Radius = 10 }));
        far = Output(await Applied(session, new TranslateCommand { BodyId = far, Dx = 50, Dy = 0, Dz = 0 }));
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 40, SizeZ = 2 }));
        var pieces = await session.Read((document, _) => KernelProjection.OfRecord(document, document.Bodies.Single(body => body.Handle.Id == sphere)).Solid!.Polygons.Count);
        Assert.Equal(3968, pieces);

        var refused = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = Ids(sphere, far) });
        Assert.Equal(DiagnosticCodes.CheckWorkLimit, refused.Error?.Code);
        Assert.Contains("15745024 products", refused.Error!.Message, StringComparison.Ordinal);
        Assert.Equal(DiagnosticCodes.CheckWorkLimit, await Refusal(session, null));

        var overlap = await Pair(session, sphere, plate);
        Assert.Equal((PairClass.Overlapping, true), (overlap.Class, overlap.MeshOnly));
    }

    // TASK-0438 (finding E233; Q5 A): a cancel acts inside a pair. The nearest pair reads the token for each piece of its
    // first side, and the intersection of the kernel at each node of each walk that splits, so a cancelled token stops each
    // of them at its first step. The query reads it before each body.
    [NativeManifoldFact]
    public async Task A_Cancel_Stops_The_Gap_And_The_Intersection_Inside_One_Pair()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var a = KernelSolid.Box(Guid.NewGuid(), 10, 10, 10);
        var b = KernelSolid.Box(Guid.NewGuid(), 10, 10, 10).Translate(Vector3R.FromDoubles(5, 0, 0));
        var pieces = a.Polygons.Select(polygon => new NearestPoints.Piece(polygon.Vertices)).ToList();

        Assert.NotNull(PlanarBoolean.Intersect(a, b, CancellationToken.None));
        Assert.NotNull(NearestPoints.Nearest(pieces, pieces, CancellationToken.None));
        Assert.Throws<OperationCanceledException>(() => PlanarBoolean.Intersect(a, b, cancelled.Token));
        Assert.Throws<OperationCanceledException>(() => NearestPoints.Nearest(pieces, pieces, cancelled.Token));

        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var (x, y) = (await Box(session, 10), await Box(session, 10, 5, 0, 0));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = Ids(x, y) }, cancelled.Token));
    }

    // TASK-0438 (finding E234; ADR-0182 item 7): a closed mesh whose two shells share a volume is refused. The cubes [0, 10]³
    // and [5, 15]³ in one file: before TASK-0438 a box of 2 at (2, 2, 2), inside the first cube, was "apart" with a gap of 1.
    // Two cubes apart in one file are checked: a box of 2 at (22, 22, 22) shares 8 mm³ with the second cube. A cube with a
    // void, [0, 10]³ less [3, 7]³ as an inner shell turned inward, is checked: a box of 2 at (5, 5, 5) is in the void, 1 from
    // each wall, and a box of 1 at (1.5, 1.5, 1.5) shares its 1 mm³ with the wall.
    [NativeManifoldFact]
    public async Task A_Mesh_Whose_Shells_Share_A_Volume_Is_Refused_And_Shells_Apart_Or_A_Void_Are_Checked()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets));
        static float[][] Moved(float[][] triangles, float by) => [.. triangles.Select(t => t.Select(v => v + by).ToArray())];
        static float[][] Scaled(float[][] triangles, float scale, float by) => [.. triangles.Select(t => t.Select(v => (v * scale) + by).ToArray())];
        static float[][] Turned(float[][] triangles) => [.. triangles.Select(t => (float[])[t[0], t[1], t[2], t[6], t[7], t[8], t[3], t[4], t[5]])];

        var crossing = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl([.. Cube, .. Moved(Cube, 5)])) }));
        var inFirst = await Box(session, 2, 2, 2, 2);
        Assert.Equal(DiagnosticCodes.MeshUnsupported, await Refusal(session, Ids(crossing, inFirst)));

        var apart = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl([.. Cube, .. Moved(Cube, 20)])) }));
        var inSecond = await Box(session, 2, 22, 22, 22);
        var second = await Pair(session, apart, inSecond);
        Assert.Equal((PairClass.Overlapping, 8.0, true), (second.Class, second.Volume, second.MeshOnly));

        var hollow = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl([.. Cube, .. Turned(Scaled(Cube, 0.4f, 3))])) }));
        var inVoid = await Box(session, 2, 5, 5, 5);
        var inWall = await Box(session, 1, 1.5, 1.5, 1.5);
        var empty = await Pair(session, hollow, inVoid);
        Assert.Equal((PairClass.Apart, 1.0), (empty.Class, empty.Gap));
        var wall = await Pair(session, hollow, inWall);
        Assert.Equal((PairClass.Overlapping, 1.0), (wall.Class, wall.Volume));
    }

    // TASK-0449 (finding E236 of the third codebase review of 2026-11-21; ADR-0182 item 7): a hollow cube with a cube in its
    // void does not cross itself. One file holds [0, 20]³ outward, [4, 16]³ inward and [8, 12]³ outward, so the island is an
    // outer shell inside the box of the first one. Before TASK-0449 the check refused the mesh, because the two outer shells
    // share the volume of the island. The voids now leave the shared volume first. A box of 1 at (1, 1, 1) shares its 1 mm³
    // with the wall, a box of 1 at (6, 6, 6) in the void is 1.5 from the inner face of the wall, and a box of 2 at
    // (10, 10, 10) shares its 8 mm³ with the island.
    [NativeManifoldFact]
    public async Task A_Mesh_With_An_Island_In_A_Void_Is_Checked()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets));
        static float[][] Scaled(float[][] triangles, float scale, float by) => [.. triangles.Select(t => t.Select(v => (v * scale) + by).ToArray())];
        static float[][] Turned(float[][] triangles) => [.. triangles.Select(t => (float[])[t[0], t[1], t[2], t[6], t[7], t[8], t[3], t[4], t[5]])];

        var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl([.. Scaled(Cube, 2, 0), .. Turned(Scaled(Cube, 1.2f, 4)), .. Scaled(Cube, 0.4f, 8)])) }));
        var (inWall, inVoid, inIsland) = (await Box(session, 1, 1, 1, 1), await Box(session, 1, 6, 6, 6), await Box(session, 2, 10, 10, 10));

        var wall = await Pair(session, mesh, inWall);
        Assert.Equal((PairClass.Overlapping, 1.0, true), (wall.Class, wall.Volume, wall.MeshOnly));
        var empty = await Pair(session, mesh, inVoid);
        Assert.Equal((PairClass.Apart, 1.5), (empty.Class, empty.Gap));
        var island = await Pair(session, mesh, inIsland);
        Assert.Equal((PairClass.Overlapping, 8.0), (island.Class, island.Volume));
    }

    // TASK-0449 (finding E237 of the third codebase review of 2026-11-21; ADR-0182 item 8): the bound of the work comes before
    // the solid of a mesh body. Two files of 400 triangles each, one triangle at each height, close no solid, and their pair
    // needs 400 × 400 = 160,000 products. Before TASK-0449 the check built the solid of the first file and refused it as open
    // with E-MESH-UNSUPPORTED; the count of the triangles now refuses the check by the bound before that work.
    [NativeManifoldFact]
    public async Task The_Bound_Of_The_Work_Comes_Before_The_Solid_Of_A_Mesh_Body()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets));
        float[][] layers = [.. Enumerable.Range(0, 400).Select(k => (float[])[0, 0, k, 10, 0, k, 0, 10, k])];
        var first = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl(layers)) }));
        var second = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl(layers)) }));

        var refused = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = Ids(first, second) });
        Assert.Equal(DiagnosticCodes.CheckWorkLimit, refused.Error?.Code);
        Assert.Contains("160000 products", refused.Error!.Message, StringComparison.Ordinal);
    }

    // TASK-0438 (finding E233; ADR-0182 item 8): the test of the shells of a mesh counts in the work. Forty cubes of 12
    // triangles, each moved by 0.1 from the last, in one file: each pair of the 780 pairs of shells shares a volume, so the
    // test of the shells needs 780 × 12 × 12 = 112,320 products, and the pair with a box 480 × 6 = 2,880. The sum, 115,200, is
    // above the limit, so the check is refused before it tests a shell.
    [NativeManifoldFact]
    public async Task The_Test_Of_The_Shells_Of_A_Mesh_Counts_In_The_Work_Of_The_Check()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets));
        float[][] cubes = [.. Enumerable.Range(0, 40).SelectMany(k => Cube.Select(t => t.Select(v => v + (0.1f * k)).ToArray()))];
        var mesh = Output(await Applied(session, new ImportStlCommand { Asset = assets.Add(Stl(cubes)) }));
        var box = await Box(session, 2, 30, 0, 0);

        var refused = await session.Query<InterferenceInfo>(new CheckInterferenceQuery { BodyIds = Ids(mesh, box) });
        Assert.Equal(DiagnosticCodes.CheckWorkLimit, refused.Error?.Code);
        Assert.Contains("115200 products", refused.Error!.Message, StringComparison.Ordinal);
    }

    // TASK-0438 (finding E230; Q6 A; ADR-0181, "Consequences"): a union of two boxes of 10 that touch on an edge, at the origin
    // and at (10, 10, 0), keeps its body, with a B-Rep whose shared edge has four faces. The pair check reads the solid of the
    // kernel and needs no valid B-Rep: a box of 2 at (0, 0, 20) is apart by 14, and a box of 2 at (5, 5, 0) on the shared edge
    // shares 4 mm³ with it, 2 with each box. The STEP export refuses the body with E-EXPORT-BODY-FAILED.
    [NativeManifoldFact]
    public async Task A_Union_Of_Two_Boxes_That_Touch_On_An_Edge_Is_Checked_And_Not_Written_To_Step()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = new DocumentSession(EngineHosting.CreateDefault(backend));
        var (a, b) = (await Box(session, 10), await Box(session, 10, 10, 10, 0));
        var union = Output(await Applied(session, new UnionCommand { FirstBodyId = a, SecondBodyId = b }));
        var (above, on) = (await Box(session, 2, 0, 0, 20), await Box(session, 2, 5, 5, 0));

        var apart = await Pair(session, union, above);
        Assert.Equal((PairClass.Apart, 14.0), (apart.Class, apart.Gap));
        var overlap = await Pair(session, union, on);
        Assert.Equal((PairClass.Overlapping, 4.0), (overlap.Class, overlap.Volume));
        var (_, error) = await session.Read((document, _) => Engine.Core.Export.StepExport.Write(document));
        Assert.Equal(DiagnosticCodes.ExportBodyFailed, error?.Code);
        Assert.Contains("more than two faces", error!.Message, StringComparison.Ordinal);
    }

    // The class and the gap of two boxes of the table, from their intervals on each axis.
    private static (PairClass Class, double Gap) Expected(BookcaseCase.Part a, BookcaseCase.Part b)
    {
        static double Shared(double centreA, double sizeA, double centreB, double sizeB)
            => Math.Min(centreA + (sizeA / 2), centreB + (sizeB / 2)) - Math.Max(centreA - (sizeA / 2), centreB - (sizeB / 2));

        double[] shared = [Shared(a.X, a.SizeX, b.X, b.SizeX), Shared(a.Y, a.SizeY, b.Y, b.SizeY), Shared(a.Z, a.SizeZ, b.Z, b.SizeZ)];
        if (shared.All(length => length > 0))
            return (PairClass.Overlapping, 0);
        if (shared.All(length => length >= 0))
            return (PairClass.Touching, 0);
        return (PairClass.Apart, Math.Sqrt(shared.Where(length => length < 0).Sum(length => length * length)));
    }

    // The 12 triangles of the cube from 0 to 10, each counter-clockwise when seen from outside.
    private static readonly float[][] Cube =
    [
        [0, 0, 0, 0, 10, 0, 10, 10, 0], [0, 0, 0, 10, 10, 0, 10, 0, 0],
        [0, 0, 10, 10, 0, 10, 10, 10, 10], [0, 0, 10, 10, 10, 10, 0, 10, 10],
        [0, 0, 0, 10, 0, 0, 10, 0, 10], [0, 0, 0, 10, 0, 10, 0, 0, 10],
        [0, 10, 0, 0, 10, 10, 10, 10, 10], [0, 10, 0, 10, 10, 10, 10, 10, 0],
        [0, 0, 0, 0, 0, 10, 0, 10, 10], [0, 0, 0, 0, 10, 10, 0, 10, 0],
        [10, 0, 0, 10, 10, 0, 10, 10, 10], [10, 0, 0, 10, 10, 10, 10, 0, 10],
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
