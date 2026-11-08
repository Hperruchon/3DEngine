using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Export;
using Engine.Core.Hosting;

namespace Engine.Core.Determinism;

// ADR-0074 (phase R14f3, TASK-0233): the probe of anti-objective 16. One log with geometry and solved sketches, its replay,
// and the SHA-256 of the exact bits of each mesh, each bounding box, each solved point and each datum frame. The tests
// of the three desktop runners and the program of the phone run the same code, so the pipeline compares one hash of each
// platform. Until TASK-0233 it was a part of Engine.Tests/ReplayDeterminism/CrossPlatformHashTests.cs (TASK-0156), which
// the program of the phone cannot reference. It writes nothing to a log of a document: the replay has its own Document.
public static class CrossPlatformProbe
{
    // Fixed identifiers, so the handles of the bodies are the same on each runner (ADR-0012 §4).
    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    // A cut of a box, and an extrude of a sketch whose constraints the solver solves, with a move of each solid.
    public static IReadOnlyList<Command> Log()
    {
        var (sketch, a, b, c, d) = (Id(10), Id(11), Id(12), Id(13), Id(14));
        var (ab, bc, cd, da) = (Id(21), Id(22), Id(23), Id(24));
        return
        [
            new CreateBoxCommand { CommandId = Id(1), SizeX = 40, SizeY = 30, SizeZ = 10 },
            new CreateBoxCommand { CommandId = Id(2), SizeX = 15.5, SizeY = 15.25, SizeZ = 30 },
            new TranslateCommand { CommandId = Id(3), BodyId = Id(2), Dx = 10.125, Dy = 5.0625, Dz = -3 },
            new SubtractCommand { CommandId = Id(4), MinuendBodyId = Id(1), SubtrahendBodyId = Id(3) },
            new CreateSketchCommand { CommandId = sketch, DatumId = Datums.XZ.Id },
            new AddSketchPointCommand { CommandId = a, SketchId = sketch, X = 0.5, Y = 0.3 },
            new AddSketchPointCommand { CommandId = b, SketchId = sketch, X = 30.4, Y = 0.2 },
            new AddSketchPointCommand { CommandId = c, SketchId = sketch, X = 30.1, Y = 20.6 },
            new AddSketchPointCommand { CommandId = d, SketchId = sketch, X = 0.3, Y = 19.8 },
            new AddSketchLineCommand { CommandId = ab, SketchId = sketch, StartPointId = a, EndPointId = b },
            new AddSketchLineCommand { CommandId = bc, SketchId = sketch, StartPointId = b, EndPointId = c },
            new AddSketchLineCommand { CommandId = cd, SketchId = sketch, StartPointId = c, EndPointId = d },
            new AddSketchLineCommand { CommandId = da, SketchId = sketch, StartPointId = d, EndPointId = a },
            new AddSketchConstraintCommand { CommandId = Id(31), SketchId = sketch, Kind = "horizontal", First = ab },
            new AddSketchConstraintCommand { CommandId = Id(32), SketchId = sketch, Kind = "vertical", First = bc },
            new AddSketchConstraintCommand { CommandId = Id(33), SketchId = sketch, Kind = "horizontal", First = cd },
            new AddSketchConstraintCommand { CommandId = Id(34), SketchId = sketch, Kind = "vertical", First = da },
            new AddSketchConstraintCommand { CommandId = Id(35), SketchId = sketch, Kind = "distance", First = ab, Value = 30 },
            new AddSketchConstraintCommand { CommandId = Id(36), SketchId = sketch, Kind = "distance", First = bc, Value = 20.5 },
            new ExtrudeCommand { CommandId = Id(40), SketchId = sketch, Distance = 12.75 },
            new TranslateCommand { CommandId = Id(41), BodyId = Id(40), Dx = -7.5, Dy = 2.25, Dz = 1 },

            // ADR-0051 item 10 (TASK-0166): a sketch on the side of a slanted line, whose datum has general axes, so
            // manifold_transform multiplies general numbers in native code; then a move of a point of that line, so the
            // datum and the solid on it move.
            new CreateSketchCommand { CommandId = Id(50), DatumId = Datums.XY.Id },
            new AddSketchPointCommand { CommandId = Id(51), SketchId = Id(50), X = 50.25, Y = 0.5 },
            new AddSketchPointCommand { CommandId = Id(52), SketchId = Id(50), X = 71.5, Y = 3.75 },
            new AddSketchPointCommand { CommandId = Id(53), SketchId = Id(50), X = 64.125, Y = 17.5 },
            new AddSketchLineCommand { CommandId = Id(54), SketchId = Id(50), StartPointId = Id(51), EndPointId = Id(52) },
            new AddSketchLineCommand { CommandId = Id(55), SketchId = Id(50), StartPointId = Id(52), EndPointId = Id(53) },
            new AddSketchLineCommand { CommandId = Id(56), SketchId = Id(50), StartPointId = Id(53), EndPointId = Id(51) },
            new ExtrudeCommand { CommandId = Id(57), SketchId = Id(50), Distance = 9.5 },
            new CreateDatumCommand { CommandId = Id(58), FeatureId = Id(57), Element = "side", ItemId = Id(55) },
            new CreateSketchCommand { CommandId = Id(60), DatumId = Id(58) },
            new AddSketchPointCommand { CommandId = Id(61), SketchId = Id(60), X = 1.5, Y = 1.25 },
            new AddSketchPointCommand { CommandId = Id(62), SketchId = Id(60), X = 6.75, Y = 1.5 },
            new AddSketchPointCommand { CommandId = Id(63), SketchId = Id(60), X = 4.25, Y = 6.125 },
            new AddSketchLineCommand { CommandId = Id(64), SketchId = Id(60), StartPointId = Id(61), EndPointId = Id(62) },
            new AddSketchLineCommand { CommandId = Id(65), SketchId = Id(60), StartPointId = Id(62), EndPointId = Id(63) },
            new AddSketchLineCommand { CommandId = Id(66), SketchId = Id(60), StartPointId = Id(63), EndPointId = Id(61) },
            new ExtrudeCommand { CommandId = Id(67), SketchId = Id(60), Distance = 3.25 },

            // ADR-0052 item 10 (TASK-0167): a sketch on XZ with the projection of an edge of the end cap of the extrude on the
            // slanted side, and of a vertex of the base; the move below moves both.
            new CreateSketchCommand { CommandId = Id(70), DatumId = Datums.XZ.Id },
            new AddSketchReferenceCommand { CommandId = Id(71), SketchId = Id(70), FeatureId = Id(67), Element = "endEdge", ItemId = Id(64) },
            new AddSketchReferenceCommand { CommandId = Id(72), SketchId = Id(70), FeatureId = Id(57), Element = "endVertex", ItemId = Id(53) },
            new MoveSketchPointCommand { CommandId = Id(68), SketchId = Id(50), PointId = Id(53), X = 63.5, Y = 18.25 },

            // Finding E161 of the codebase review of 2026-11-06 (d) (TASK-0242): the polygon of a circle of ADR-0040 item 2
            // and the cylinder of ADR-0076, with + - * / sqrt only, and a cut of the one by the other after a move, so the
            // four platforms compare the bits of the 64 vertices.
            new CreateSketchCommand { CommandId = Id(80), DatumId = Datums.XY.Id },
            new AddSketchPointCommand { CommandId = Id(81), SketchId = Id(80), X = 90.25, Y = 10.5 },
            new AddSketchCircleCommand { CommandId = Id(82), SketchId = Id(80), CenterPointId = Id(81), Radius = 6.125 },
            new ExtrudeCommand { CommandId = Id(83), SketchId = Id(80), Distance = 7.5 },
            new CreateCylinderCommand { CommandId = Id(84), Radius = 4.75, Height = 20 },
            new TranslateCommand { CommandId = Id(85), BodyId = Id(84), Dx = 90.25, Dy = 10.5, Dz = 1 },
            new SubtractCommand { CommandId = Id(86), MinuendBodyId = Id(83), SubtrahendBodyId = Id(85) },

            // ADR-0089 item 5 (phase K5f, TASK-0299): the closed meshes of the cone, the sphere and the torus, from the table of
            // the circle with + - * / sqrt only, a cut of the cone by a moved cylinder, and a move of each body, so the four
            // platforms compare the bits of each vertex of the new primitives. A cut by the sphere would run the exact kernel
            // on 3968 triangles, which took more than two minutes on the emulator (run 42444929625).
            new CreateConeCommand { CommandId = Id(90), Radius = 3.25, Height = 6.5 },
            new CreateCylinderCommand { CommandId = Id(91), Radius = 1.25, Height = 10 },
            new TranslateCommand { CommandId = Id(92), BodyId = Id(91), Dx = 0.5, Dy = 0.25, Dz = 2.125 },
            new SubtractCommand { CommandId = Id(93), MinuendBodyId = Id(90), SubtrahendBodyId = Id(92) },
            new TranslateCommand { CommandId = Id(94), BodyId = Id(93), Dx = 120.5, Dy = 4.25, Dz = 3.25 },
            new CreateTorusCommand { CommandId = Id(95), Radius = 5.5, TubeRadius = 1.25 },
            new TranslateCommand { CommandId = Id(96), BodyId = Id(95), Dx = 140.75, Dy = -6.5, Dz = 1.25 },
            new CreateSphereCommand { CommandId = Id(97), Radius = 2.375 },
            new TranslateCommand { CommandId = Id(98), BodyId = Id(97), Dx = 160.5, Dy = 8.25, Dz = 2.375 },
        ];
    }

    // Each line of the text ends with a line feed on each platform: AppendLine writes Environment.NewLine,
    // and the first runs of the pipeline gave Windows its own hash with equal numbers (TASK-0156).
    //
    // The text of the state: each live body in the order of its handle, with the bits of its bounding box and of
    // each position and its indices, then each sketch with the bits of each point. The first line of the file is
    // the SHA-256 of this text; the lines of the bounding boxes and of the points follow, so a person can read a
    // difference.
    public static (string Hash, IReadOnlyList<string> Lines) State(Document document, IGeometryBackend backend)
    {
        static string Short(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
        static string Bits(double value) => BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
        var query = backend.TryGet<IGeometryQuery>()!;
        var tessellation = backend.TryGet<ITessellationOps>()!;
        var text = new StringBuilder();
        var lines = new List<string>();
        foreach (var body in document.Bodies.OrderBy(body => body.Handle.Id))
        {
            var box = query.GetBoundingBox(new BodyHandle(body.GeometryKey));
            var line = $"body {body.Handle.Id} {body.Kind} box {string.Join(" ", new[] { box.MinX, box.MinY, box.MinZ, box.MaxX, box.MaxY, box.MaxZ }.Select(Bits))}";
            var mesh = tessellation.Tessellate(new BodyHandle(body.GeometryKey));
            var positions = string.Join(" ", mesh.Positions.Select(Bits));
            var indices = string.Join(" ", mesh.Indices);
            // A short hash of the positions and of the indices, so a difference between two runners names its part.
            lines.Add($"{line} positions {mesh.Positions.Length} {Short(positions)} indices {mesh.Indices.Length} {Short(indices)}");
            text.Append(line).Append('\n');
            text.Append(positions).Append('\n');
            text.Append(indices).Append('\n');
        }

        foreach (var sketch in document.Sketches.OrderBy(sketch => sketch.Id))
        {
            foreach (var point in sketch.Points.OrderBy(point => point.Id))
            {
                var line = $"point {sketch.Id} {point.Id} {Bits(point.X)} {Bits(point.Y)}";
                lines.Add(line);
                text.Append(line).Append('\n');
            }
        }

        // ADR-0051 item 10 (TASK-0166): the bits of the frame of each datum.
        foreach (var datum in document.Datums.OrderBy(datum => datum.Id))
        {
            var (o, u, v, n) = (datum.Frame.Origin, datum.Frame.U, datum.Frame.V, datum.Frame.Normal);
            var line = $"datum {datum.Id} {string.Join(" ", new[] { o.X, o.Y, o.Z, u.X, u.Y, u.Z, v.X, v.Y, v.Z, n.X, n.Y, n.Z }.Select(Bits))}";
            lines.Add(line);
            text.Append(line).Append('\n');
        }

        return (Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))), lines);
    }

    // ADR-0083 (phase P9b, TASK-0258): the bracket of the demonstration "Open the same STL on desktop and Android", made
    // with the commands of the engine: an L profile on the plane YZ (U along +y, V along +z, the normal along +x), extruded
    // by 60, so the base is 60 by 40 by 4 and the wall is 4 thick and 30 high, and two holes of 6 mm through the base at
    // x = 15 and x = 45, y = 24. Its STL export is the file testdata/stl/bracket.stl.
    public static IReadOnlyList<Command> BracketLog()
    {
        var sketch = Id(100);
        (double U, double V)[] profile = [(0, 0), (40, 0), (40, 4), (4, 4), (4, 30), (0, 30)];
        var log = new List<Command> { new CreateSketchCommand { CommandId = sketch, DatumId = Datums.YZ.Id } };
        for (var i = 0; i < profile.Length; i++)
            log.Add(new AddSketchPointCommand { CommandId = Id(101 + i), SketchId = sketch, X = profile[i].U, Y = profile[i].V });
        for (var i = 0; i < profile.Length; i++)
            log.Add(new AddSketchLineCommand { CommandId = Id(111 + i), SketchId = sketch, StartPointId = Id(101 + i), EndPointId = Id(101 + ((i + 1) % profile.Length)) });
        log.Add(new ExtrudeCommand { CommandId = Id(120), SketchId = sketch, Distance = 60 });
        var body = Id(120);
        foreach (var (hole, x) in new[] { (130, 15.0), (140, 45.0) })
        {
            log.Add(new CreateCylinderCommand { CommandId = Id(hole), Radius = 3, Height = 10 });
            log.Add(new TranslateCommand { CommandId = Id(hole + 1), BodyId = Id(hole), Dx = x, Dy = 24, Dz = 2 });
            log.Add(new SubtractCommand { CommandId = Id(hole + 2), MinuendBodyId = body, SubtrahendBodyId = Id(hole + 1) });
            body = Id(hole + 2);
        }

        return log;
    }

    // The STL export of the bracket, the bytes of testdata/stl/bracket.stl.
    public static async Task<byte[]> BracketStl(IGeometryBackend backend, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        var registry = new CommandRegistry();
        HandlerCatalog.RegisterAll(registry, new QueryRegistry());
        var replay = await Replay.ReplayLog(BracketLog(), registry, backend, ct).ConfigureAwait(false);
        var (content, error) = StlExport.Write(replay.Document, backend);
        return content ?? throw new InvalidOperationException($"The export of the bracket failed: {error?.Code} {error?.Message}");
    }

    // ADR-0083: the import of the bracket and of its first half, the file testdata/stl/bracket-damaged.stl, through a bus
    // as a host applies them; the first gives a mesh body and the second a refusal. The lines name the triangles and the
    // code, so the four platforms compare them.
    public static async Task<(string Hash, IReadOnlyList<string> Lines)> Import(IGeometryBackend backend, byte[] stl, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(stl);
        var assets = new AssetBytes();
        var (good, cut) = (assets.Add(stl), assets.Add(stl[..(stl.Length / 2)]));
        var kit = EngineHosting.CreateDefault(backend, assets: assets);
        var bus = kit.CreateCommandBus();
        var imported = await bus.Apply(new ImportStlCommand { CommandId = Id(200), Asset = good, BodyName = "bracket.stl" }, ct).ConfigureAwait(false);
        var damaged = await bus.Apply(new ImportStlCommand { CommandId = Id(201), Asset = cut, BodyName = "bracket-damaged.stl" }, ct).ConfigureAwait(false);
        if (imported.Status != CommandStatus.Applied || damaged.Status != CommandStatus.Rejected)
            throw new InvalidOperationException($"The import of the bracket gave {imported.Status} {imported.Error?.Code}, and of its half {damaged.Status}.");

        var (hash, lines) = State(kit.Document, backend);
        var triangles = backend.TryGet<ITessellationOps>()!.Tessellate(new BodyHandle(Id(200))).Indices.Length / 3;
        var summary = $"stl bracket.stl triangles {triangles} bracket-damaged.stl {damaged.Error?.Code}";
        return (Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{hash}\n{summary}\n"))), [.. lines, summary]);
    }

    // The replay of the log on the backend, and the hash and the lines of its state. Since TASK-0258 the hash holds the
    // export and the import of the bracket too (ADR-0083), so the pipeline compares the STL reader of each platform.
    public static async Task<(string Hash, IReadOnlyList<string> Lines, ReplayResult Replay)> Run(IGeometryBackend backend, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(backend);
        var registry = new CommandRegistry();
        HandlerCatalog.RegisterAll(registry, new QueryRegistry());
        var replay = await Replay.ReplayLog(Log(), registry, backend, ct).ConfigureAwait(false);
        var (hash, lines) = await WithStl(backend, State(replay.Document, backend), ct).ConfigureAwait(false);
        return (hash, lines, replay);
    }

    // The hash and the lines of a state, with the export and the import of the bracket (ADR-0083): the desktop test and
    // the program of the phone both take their hash from here, so the four platforms compare the same text.
    public static async Task<(string Hash, IReadOnlyList<string> Lines)> WithStl(IGeometryBackend backend, (string Hash, IReadOnlyList<string> Lines) state, CancellationToken ct = default)
    {
        var (stlHash, stlLines) = await Import(backend, await BracketStl(backend, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
        var combined = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{state.Hash}\n{stlHash}\n")));
        return (combined, [.. state.Lines, .. stlLines]);
    }
}
