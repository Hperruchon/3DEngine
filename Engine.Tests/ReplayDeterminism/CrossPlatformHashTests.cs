using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Hosting;
using Engine.Core.Sketch;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.ReplayDeterminism;

// Phase P0.67 (TASK-0156, anti-objective 16): the replay of one log with geometry and solved sketches gives the
// same bits on each platform. Each runner replays the log on the native backend and writes the SHA-256 of the
// exact bits of each mesh, each bounding box and each solved point to the file that DETERMINISM_HASH_FILE names;
// the job "Compare the determinism hashes" of the pipeline compares the files of the three runners. Register
// entry R-0023 recorded that each runner compared with its own constants only.
public class CrossPlatformHashTests
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
        ];
    }

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
            text.AppendLine(line);
            text.AppendLine(positions);
            text.AppendLine(indices);
        }

        foreach (var sketch in document.Sketches.OrderBy(sketch => sketch.Id))
        {
            foreach (var point in sketch.Points.OrderBy(point => point.Id))
            {
                var line = $"point {sketch.Id} {point.Id} {Bits(point.X)} {Bits(point.Y)}";
                lines.Add(line);
                text.AppendLine(line);
            }
        }

        return (Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))), lines);
    }

    private static CommandRegistry Registry()
    {
        var registry = new CommandRegistry();
        HandlerCatalog.RegisterAll(registry, new QueryRegistry());
        return registry;
    }

    [NativeManifoldFact]
    public async Task A_Native_Replay_With_Geometry_And_Solved_Sketches_Gives_One_Hash()
    {
        using var first = new ManifoldGeometryBackend();
        using var second = new ManifoldGeometryBackend();
        var one = await Replay.ReplayLog(Log(), Registry(), first);
        var two = await Replay.ReplayLog(Log(), Registry(), second);

        var (hash, lines) = State(one.Document, first);
        Assert.Equal(Log().Count, one.Document.Log.Count);
        Assert.Equal(2, one.Document.Bodies.Count);
        Assert.True(SketchSolver.Status(Assert.Single(one.Document.Sketches)).Solved, "The sketch of the log is not solved.");
        Assert.Equal(hash, State(two.Document, second).Hash);

        if (Environment.GetEnvironmentVariable("DETERMINISM_HASH_FILE") is { Length: > 0 } file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
            File.WriteAllLines(file, [hash, .. lines]);
        }
    }
}
