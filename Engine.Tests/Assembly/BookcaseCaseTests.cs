using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;
using Engine.Tests.Governance;
using Xunit.Abstractions;

namespace Engine.Tests.Assembly;

// Row K10a (TASK-0429): the bookcase of the owner as the first case of K10, and the measure of 150 turns for register
// entry R-0070. The builder is BookcaseCase. docs/proposals/2026-11-21-the-bookcase-case-of-k10.md gives the table of the
// bodies; this file holds the same table as the expected boxes, apart from the builder, so a wrong size or place in the
// builder fails a test.
//
// The tests use the native backend, as the file of the owner does ("manifold"), because only that backend turns a body
// (ADR-0112 item 4). In continuous integration they cannot skip (NativeManifoldFact).
public sealed class BookcaseCaseTests(ITestOutputHelper output) : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-bookcase-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    // The box of each body, (minimum x, y, z) and (maximum x, y, z) in mm, from the table of the proposal.
    private static readonly IReadOnlyDictionary<string, Aabb> Expected = new Dictionary<string, Aabb>(StringComparer.Ordinal)
    {
        ["Module 1 - volume indicatif"] = new(-1700, -200, 0, -900, 200, 1000),
        ["Module 2 - volume indicatif"] = new(900, -200, 0, 1700, 200, 1000),
        ["Module 3 - volume indicatif"] = new(-100, -200, 1400, 300, 200, 2350),
        ["Module 5 - volume indicatif"] = new(1100, -200, 1800, 1500, 200, 2350),
        ["Module 6 - volume indicatif"] = new(-100, -200, 0, 500, 200, 600),
        ["Module 7 - volume indicatif"] = new(-1700, -200, 1000, -900, 200, 1400),
        ["Module 8 - volume indicatif"] = new(-900, -200, 600, -300, 200, 1000),
        ["Module 9 - volume indicatif"] = new(-900, -200, 0, -100, 200, 600),
        ["Module 10 - volume indicatif"] = new(500, -200, 0, 900, 200, 600),
        ["Module 11 - volume indicatif"] = new(1100, -200, 1400, 1500, 200, 1800),
        ["Module 12 - volume indicatif"] = new(-900, -200, 1000, -300, 200, 1400),
        ["Module 14 - volume indicatif"] = new(300, -200, 1400, 1100, 200, 2200),
        ["Module 15 - volume indicatif"] = new(-1700, -200, 1400, -900, 200, 2350),
        ["Module 16 - volume indicatif"] = new(-900, -200, 1400, -100, 200, 2350),
        ["Cote gauche"] = new(-300, -200, 600, -282, 200, 1400),
        ["Cote droit"] = new(882, -200, 600, 900, 200, 1400),
        ["Bas"] = new(-282, -200, 600, 882, 194, 618),
        ["Haut"] = new(-282, -200, 1382, 882, 194, 1400),
        ["Fond"] = new(-282, 194, 600, 882, 200, 1400),
    };

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static async Task<Aabb> Box(DocumentSession session, Guid body)
    {
        var result = await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = body });
        Assert.True(result.Error is null, result.Error?.Message);
        return result.Result;
    }

    // The name and the box of each live body, by name. The names of the bookcase are unique.
    private static async Task<SortedDictionary<string, Aabb>> Boxes(DocumentSession session)
    {
        var bodies = await session.Read((document, _) => document.Bodies.Select(body => (body.Name ?? "", body.Handle.Id)).ToList());
        var boxes = new SortedDictionary<string, Aabb>(StringComparer.Ordinal);
        foreach (var (name, id) in bodies)
            boxes.Add(name, await Box(session, id));
        return boxes;
    }

    // Each of the 19 bodies exists with its name, and its box is the box of the table, exactly. No other body exists.
    [NativeManifoldFact]
    public async Task Each_Body_Of_The_Bookcase_Has_Its_Name_And_The_Box_Of_The_Table()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var built = await BookcaseCase.Build(session);

        var boxes = await Boxes(session);
        session.Close();

        Assert.Equal(19, built.Count);
        Assert.Equal(Expected.Keys.Order(StringComparer.Ordinal), boxes.Keys);
        foreach (var (name, box) in Expected)
            Assert.True(box == boxes[name], $"{name}: the box is {boxes[name]}, and the table gives {box}.");
    }

    // A save and an open give the same bodies: the same records (handle, kind, name, visibility) and the same boxes.
    [NativeManifoldFact]
    public async Task A_Save_And_An_Open_Give_The_Same_Bodies()
    {
        var path = Path.Combine(_folder, "bookcase" + DocumentFile.Extension);
        List<BodyRecord> records;
        SortedDictionary<string, Aabb> boxes;
        using (var backend = new ManifoldGeometryBackend())
        {
            var live = NewSession(backend);
            DocumentFile.Create(live, path);
            await BookcaseCase.Build(live);
            records = await live.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            boxes = await Boxes(live);
            live.Close();
        }

        using (var backend = new ManifoldGeometryBackend())
        {
            var opened = NewSession(backend);
            var warnings = await DocumentFile.Open(opened, path);
            var reopened = await opened.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            var reboxes = await Boxes(opened);
            var version = await opened.Read((document, _) => document.Version);
            opened.Close();

            Assert.Empty(warnings);
            Assert.Equal(BookcaseCase.CommandCount, version);
            Assert.Equal(19, reopened.Count);
            Assert.Equal(records, reopened);
            Assert.Equal(boxes, reboxes);
        }
    }

    // Finding T186 of the codebase review of 2026-11-21 (TASK-0431, Q7 A of the owner): the comparison of the builder with the
    // file of the owner is the program eng/bookcase-compare, which the owner runs where the file is (answer Q2 A of the plan
    // of K10: the file is not in the repository). No test reads the documents folder of the person who runs it, so each test
    // compares something in each run. The program compiles this builder. Until TASK-0431 a test of this class read the file,
    // and it passed with no comparison where the file was absent, as in each run of the pipeline.
    [Fact]
    public void The_Comparison_With_The_File_Of_The_Owner_Is_A_Program_And_No_Test()
    {
        var documents = "Special" + "Folder.MyDocuments";
        var tests = Directory.EnumerateFiles(RepositoryFiles.Path("Engine.Tests"), "*.cs", SearchOption.AllDirectories)
            .Select(RepositoryFiles.Relative)
            .Where(file => !file.Contains("/bin/", StringComparison.Ordinal) && !file.Contains("/obj/", StringComparison.Ordinal))
            .Where(file => RepositoryFiles.Read(file).Contains(documents, StringComparison.Ordinal))
            .ToList();
        Assert.True(tests.Count == 0, $"A test reads the documents folder of the person: {string.Join(", ", tests)}.");

        var program = RepositoryFiles.Read("eng", "bookcase-compare", "Program.cs");
        foreach (var call in new[] { documents, "DocumentFile.Read(", "BookcaseCase.Build(", "GetBoundingBoxQuery" })
            Assert.Contains(call, program, StringComparison.Ordinal);
        Assert.Contains(@"<Compile Include=""..\..\Engine.Tests\Assembly\BookcaseCase.cs""", RepositoryFiles.Read("eng", "bookcase-compare", "BookcaseCompare.csproj"), StringComparison.Ordinal);
    }

    // The measure of register entry R-0070 for the study of P11.0 (ADR-0126 item 3). Module 1 turns 150 times about the
    // vertical axis through its front left edge, the point (-1700, -200, 0), as a door turns on its hinge. Module 1 is at
    // the left end of the bookcase, so it turns out to the front and no other body is on its path; it is a box of three
    // different lengths, so each length shows a change. A turn about a point that is not the origin is a Translate, a
    // Rotate and a Translate (ADR-0112 item 9), so 150 turns are 450 commands.
    //
    // The turn is exact rational numbers, with no sine and no cosine function (CLAUDE.md, determinism rule 1). With
    // t = 1/115, cos = (1 - t^2) / (1 + t^2) and sin = 2t / (1 + t^2), about 0.9965 degree. The quaternion (115, 0, 0, -1)
    // gives that turn about -z: Rotation.FromQuaternion gives (w^2 - z^2) / s = 13224 / 13226 and 2wz / s = -230 / 13226,
    // the same two fractions, each from one division. 150 turns give about 149.5 degrees.
    //
    // The test asserts only what is stable: the count of bodies and of commands, the exact box of the turned body within a
    // bound, the 18 other boxes, and that a replay of the log gives the same bodies and boxes. It writes the times, the
    // change of each length and the size of the exact numbers of the owned kernel to the output; no time decides the
    // test (ADR-0173 item 4).
    [NativeManifoldFact]
    public async Task One_Hundred_And_Fifty_Turns_Of_A_Module_About_Its_Hinge_Keep_Its_Box_And_Replay_The_Same()
    {
        const int Turns = 150;
        const double W = 115, Z = -1;
        const string Name = "Module 1 - volume indicatif";
        (double X, double Y) hinge = (-1700, -200);

        var turn = Rotation.FromQuaternion(W, 0, 0, Z)!.Value;
        Assert.Equal((13224.0 / 13226, -230.0 / 13226), (turn.Xx, turn.Xy));
        Assert.Equal((230.0 / 13226, 13224.0 / 13226, 1.0), (turn.Yx, turn.Yy, turn.Zz));

        var path = Path.Combine(_folder, "turns" + DocumentFile.Extension);
        SortedDictionary<string, Aabb> boxes;
        List<BodyRecord> records;
        Guid body;
        long version;
        TimeSpan time;
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = NewSession(backend);
            DocumentFile.Create(session, path);
            body = (await BookcaseCase.Build(session))[Name];
            var before = await Boxes(session);

            var clock = Stopwatch.StartNew();
            for (var i = 0; i < Turns; i++)
            {
                var there = BookcaseCase.Output(await BookcaseCase.Applied(session, new TranslateCommand { BodyId = body, Dx = -hinge.X, Dy = -hinge.Y, Dz = 0 }));
                var turned = BookcaseCase.Output(await BookcaseCase.Applied(session, new RotateCommand { BodyId = there, Qw = W, Qx = 0, Qy = 0, Qz = Z }));
                body = BookcaseCase.Output(await BookcaseCase.Applied(session, new TranslateCommand { BodyId = turned, Dx = hinge.X, Dy = hinge.Y, Dz = 0 }));
            }

            time = clock.Elapsed;
            boxes = await Boxes(session);
            records = await session.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            version = await session.Read((document, _) => document.Version);
            clock.Restart();
            var (solid, reason) = await session.Read((document, _) => KernelProjection.Of(document, body));
            var kernelTime = clock.Elapsed;
            Assert.True(solid is not null, reason);
            var faces = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
            Assert.True(faces.Error is null, faces.Error?.Message);
            session.Close();

            foreach (var (name, box) in before.Where(pair => pair.Key != Name))
                Assert.Equal(box, boxes[name]);
            Report(solid!, faces.Result!, before[Name], boxes[Name], time, kernelTime);
        }

        // 19 bodies stay: each command consumes its source. The log holds 57 commands of the builder and 450 of the turns.
        Assert.Equal(19, records.Count);
        Assert.Equal(57 + 450, version);
        Assert.Contains(records, record => record.Handle.Id == body && record.Name == Name);

        // The exact box of the module after the turns, from the exact turn (115 - i)^2 / 13226 to the power 150, with
        // BigInteger and Rational only. The engine rounds each of the nine numbers of a turn to a double (relative error at
        // most 2^-53), and each Translate and Rotate rounds each coordinate, which is less than 4096 mm, to a double (at
        // most 2^-42 mm). Over 450 commands that is less than 450 * (2^-42 + 3 * 4096 * 2^-53) mm, below 1e-9 mm. The bound
        // 1e-6 mm is far above it and far below a size that a person can make or measure.
        var expected = ExactBox(hinge, 800, 400, 1000, Turns);
        var actual = boxes[Name];
        double[] got = [actual.MinX, actual.MinY, actual.MinZ, actual.MaxX, actual.MaxY, actual.MaxZ];
        double[] want = [expected.MinX, expected.MinY, expected.MinZ, expected.MaxX, expected.MaxY, expected.MaxZ];
        output.WriteLine($"The largest difference between a bound of the box and the exact turn: {Enumerable.Range(0, 6).Max(i => Math.Abs(got[i] - want[i])).ToString("R", CultureInfo.InvariantCulture)} mm.");
        for (var i = 0; i < 6; i++)
            Assert.True(Math.Abs(got[i] - want[i]) <= 1e-6, $"Bound {i} of the turned module is {got[i]:R}, and the exact turn gives {want[i]:R}.");
        Assert.Equal((0.0, 1000.0), (actual.MinZ, actual.MaxZ));

        // A replay of the log in a new session gives the same bodies and the same boxes, bit for bit.
        using (var backend = new ManifoldGeometryBackend())
        {
            var replay = NewSession(backend);
            var clock = Stopwatch.StartNew();
            var warnings = await DocumentFile.Open(replay, path);
            var replayTime = clock.Elapsed;
            var replayed = await replay.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            var reboxes = await Boxes(replay);
            replay.Close();

            output.WriteLine($"The replay of the {version} commands of the log: {replayTime.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)} ms.");
            Assert.Empty(warnings);
            Assert.Equal(records, replayed);
            Assert.Equal(boxes, reboxes);
        }
    }

    // ADR-0191 item 5 and the answer Q7 A of the owner (phase P11.0, TASK-0474): the measure above again with PlaceBody. Each
    // of the 150 turns is one absolute PlaceBody of the module, as a joint or an agent gives it: the client computes the
    // quaternion of n turns, (115 - i)^n with BigInteger divided by 115^n, and the offset h - R h of the turn about the hinge
    // h, in doubles. The body keeps its handle and its shape, so the owned kernel applies one placement to the box of the
    // builder, and the exact numbers grow by one product, not by one product for each turn. The test asserts the box within
    // the bound of the test above, the bodies, the commands and the replay, and that the longest numerator of a coordinate has
    // fewer than 200 digits (2576 after 150 Rotate steps). It writes the times and the sizes to the output.
    [NativeManifoldFact]
    public async Task One_Hundred_And_Fifty_Placements_Of_A_Module_About_Its_Hinge_Keep_Its_Box_And_Small_Numbers()
    {
        const int Turns = 150;
        const string Name = "Module 1 - volume indicatif";
        (double X, double Y) hinge = (-1700, -200);

        var path = Path.Combine(_folder, "placements" + DocumentFile.Extension);
        SortedDictionary<string, Aabb> boxes;
        List<BodyRecord> records;
        Guid body;
        long version;
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = NewSession(backend);
            DocumentFile.Create(session, path);
            body = (await BookcaseCase.Build(session))[Name];
            var before = await Boxes(session);

            var clock = Stopwatch.StartNew();
            BigInteger re = 1, im = 0;
            for (var n = 1; n <= Turns; n++)
            {
                (re, im) = ((re * 115) + im, (im * 115) - re);
                var scale = BigInteger.Pow(115, n);
                var (qw, qz) = (new Rational(re, scale).ToDouble(), new Rational(-im, scale).ToDouble());
                var (rx, ry, _) = Rotation.FromQuaternion(qw, 0, 0, -qz)!.Value.Apply(hinge.X, hinge.Y, 0);
                Assert.Equal(body, BookcaseCase.Output(await BookcaseCase.Applied(session, new PlaceBodyCommand
                {
                    BodyId = body, Qw = qw, Qx = 0, Qy = 0, Qz = -qz, Dx = hinge.X - rx, Dy = hinge.Y - ry, Dz = 0,
                })));
            }

            var time = clock.Elapsed;
            boxes = await Boxes(session);
            records = await session.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            version = await session.Read((document, _) => document.Version);
            clock.Restart();
            var (solid, reason) = await session.Read((document, _) => KernelProjection.Of(document, body));
            var kernelTime = clock.Elapsed;
            Assert.True(solid is not null, reason);
            var faces = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
            Assert.True(faces.Error is null, faces.Error?.Message);
            session.Close();

            foreach (var (name, box) in before.Where(pair => pair.Key != Name))
                Assert.Equal(box, boxes[name]);
            var digits = solid!.Polygons.SelectMany(polygon => polygon.Vertices).SelectMany(v => new[] { v.X, v.Y, v.Z })
                .Max(c => BigInteger.Abs(c.Numerator).ToString(CultureInfo.InvariantCulture).Length);
            Report(solid, faces.Result!, before[Name], boxes[Name], time, kernelTime, "the 150 placements (150 commands)");
            Assert.True(digits < 200, $"The longest numerator of a coordinate has {digits} digits.");
        }

        // 19 bodies stay, the module with its first handle. The log holds 57 commands of the builder and 150 placements.
        Assert.Equal(19, records.Count);
        Assert.Equal(57 + Turns, version);
        Assert.Contains(records, record => record.Handle.Id == body && record.Name == Name && record.Placement is not null);

        var expected = ExactBox(hinge, 800, 400, 1000, Turns);
        var actual = boxes[Name];
        double[] got = [actual.MinX, actual.MinY, actual.MinZ, actual.MaxX, actual.MaxY, actual.MaxZ];
        double[] want = [expected.MinX, expected.MinY, expected.MinZ, expected.MaxX, expected.MaxY, expected.MaxZ];
        output.WriteLine($"The largest difference between a bound of the box and the exact turn: {Enumerable.Range(0, 6).Max(i => Math.Abs(got[i] - want[i])).ToString("R", CultureInfo.InvariantCulture)} mm.");
        for (var i = 0; i < 6; i++)
            Assert.True(Math.Abs(got[i] - want[i]) <= 1e-6, $"Bound {i} of the placed module is {got[i]:R}, and the exact turn gives {want[i]:R}.");

        using (var backend = new ManifoldGeometryBackend())
        {
            var replay = NewSession(backend);
            var clock = Stopwatch.StartNew();
            var warnings = await DocumentFile.Open(replay, path);
            var replayTime = clock.Elapsed;
            var replayed = await replay.Read((document, _) => document.Bodies.OrderBy(b => b.Handle.Id).ToList());
            var reboxes = await Boxes(replay);
            replay.Close();

            output.WriteLine($"The replay of the {version} commands of the log: {replayTime.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)} ms.");
            Assert.Empty(warnings);
            Assert.Equal(records, replayed);
            Assert.Equal(boxes, reboxes);
        }
    }

    // The box of a box of the sizes x, y and z, with its front left edge on the hinge, after n turns of the exact turn
    // about -z. The complex number (115 - i)^2 / 13226 is the turn; its power n gives the cosine and the sine of n turns.
    private static Aabb ExactBox((double X, double Y) hinge, long x, long y, long z, int n)
    {
        BigInteger re = 1, im = 0;
        for (var i = 0; i < 2 * n; i++)
            (re, im) = ((re * 115) + im, (im * 115) - re);
        var scale = BigInteger.Pow(13226, n);
        var cos = new Rational(re, scale);
        var sin = new Rational(im, scale);
        var (hx, hy) = (Rational.FromDouble(hinge.X), Rational.FromDouble(hinge.Y));
        var corners = new[] { (0L, 0L), (x, 0L), (0L, y), (x, y) }
            .Select(c => ((hx + (cos * c.Item1) - (sin * c.Item2)).ToDouble(), (hy + (sin * c.Item1) + (cos * c.Item2)).ToDouble()))
            .ToList();
        return new Aabb(corners.Min(c => c.Item1), corners.Min(c => c.Item2), 0, corners.Max(c => c.Item1), corners.Max(c => c.Item2), z);
    }

    // The output of the measure: the time of the turns, the change of each length of the box, and the size of the exact
    // numbers of the owned kernel. No public query gives the exact numbers; the test reads the solid of the owned kernel
    // through the internal KernelProjection, as GetFacesTests does.
    private void Report(KernelSolid solid, FacesInfo faces, Aabb start, Aabb end, TimeSpan time, TimeSpan kernelTime, string turns = "the 150 turns (450 commands)")
    {
        var vertices = solid.Polygons.SelectMany(polygon => polygon.Vertices).ToList();
        var coordinates = vertices.SelectMany(v => new[] { v.X, v.Y, v.Z }).ToList();
        var numeratorDigits = coordinates.Max(c => BigInteger.Abs(c.Numerator).ToString(CultureInfo.InvariantCulture).Length);
        var denominatorDigits = coordinates.Max(c => c.Denominator.ToString(CultureInfo.InvariantCulture).Length);

        // The exact lengths of the edges of the kernel solid, squared, against 800, 400 and 1000.
        var worst = Rational.Zero;
        foreach (var polygon in solid.Polygons)
        {
            for (var i = 0; i < polygon.Vertices.Count; i++)
            {
                var d = polygon.Vertices[(i + 1) % polygon.Vertices.Count] - polygon.Vertices[i];
                var squared = (d.X * d.X) + (d.Y * d.Y) + (d.Z * d.Z);
                var nearest = new Rational[] { 800 * 800, 400 * 400, 1000 * 1000 }.MinBy(n => Abs(squared - n).ToDouble());
                var change = Abs(squared - nearest) / nearest;
                if (change.CompareTo(worst) > 0)
                    worst = change;
            }
        }

        var edgeChange = faces.Edges.Max(e => new[] { 800.0, 400.0, 1000.0 }.Min(n => Math.Abs(e.Length - n)));
        var f = CultureInfo.InvariantCulture;
        output.WriteLine($"The time of {turns}: {time.TotalMilliseconds.ToString("F0", f)} ms.");
        output.WriteLine($"The time of the first read of the solid of the owned kernel after the turns: {kernelTime.TotalMilliseconds.ToString("F0", f)} ms.");
        output.WriteLine($"The box at the start: {Text(start)}. The box after the turns: {Text(end)}.");
        output.WriteLine($"The size of the box on z: {(end.MaxZ - end.MinZ).ToString("R", f)} (1000 at the start).");
        output.WriteLine($"The largest change of the length of an edge, GetFaces: {edgeChange.ToString("R", f)} mm.");
        output.WriteLine($"The largest relative change of the square of the length of an edge, exact: {worst.ToDouble().ToString("R", f)}.");
        output.WriteLine($"The owned kernel: {vertices.Count} vertices; the longest numerator of a coordinate has {numeratorDigits} digits, the longest denominator {denominatorDigits} digits.");
    }

    private static string Text(Aabb box)
        => string.Join(", ", new[] { box.MinX, box.MinY, box.MinZ, box.MaxX, box.MaxY, box.MaxZ }.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));

    private static Rational Abs(Rational value) => value.Sign < 0 ? -value : value;
}
