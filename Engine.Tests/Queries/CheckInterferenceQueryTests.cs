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
using Engine.Tests.Assembly;
using Engine.Tests.Geometry;
using Xunit.Abstractions;

namespace Engine.Tests.Queries;

// Row K11 (TASK-0436): the pair check CheckInterference at one position. ADR-0182 names this file in enforced-by. Each
// expected value comes from a computation by hand or from the boxes of a table, not from the code under test. The tests use
// the native backend, because only that backend moves a body and gives the triangles of a mesh body.
public sealed class CheckInterferenceQueryTests(ITestOutputHelper output)
{
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
