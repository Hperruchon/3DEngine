using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Determinism;
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
    private static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    // ADR-0074 (TASK-0233): the log and the state are in Engine.Core, so that the program of the phone runs them too.
    public static IReadOnlyList<Command> Log() => CrossPlatformProbe.Log();

    public static (string Hash, IReadOnlyList<string> Lines) State(Document document, IGeometryBackend backend) => CrossPlatformProbe.State(document, backend);

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
        // Finding E161 (TASK-0242): the ring of a circle extrude cut by a moved cylinder is the fifth body and its sketch the fifth.
        Assert.Equal(5, one.Document.Bodies.Count);
        Assert.Equal(5, one.Document.Sketches.Count);
        Assert.All(one.Document.Sketches.SelectMany(sketch => sketch.References), reference => Assert.Null(reference.Fault));
        Assert.Equal(2, one.Document.Sketches.Single(sketch => sketch.Id == Id(70)).References.Count);
        Assert.All(one.Document.Sketches, sketch => Assert.True(SketchSolver.Status(sketch).Solved, $"The sketch {sketch.Id} of the log is not solved."));
        // The datum on the side of the slanted line has general axes, and each body has no fault.
        var datum = Assert.Single(one.Document.Datums);
        Assert.Null(datum.Fault);
        Assert.NotEqual(0, datum.Frame.U.X * datum.Frame.U.Y);
        Assert.All(one.Document.Bodies, body => Assert.Null(body.Fault));
        Assert.Equal(hash, State(two.Document, second).Hash);

        // ADR-0083 (TASK-0258): the hash of the file holds the export and the import of the bracket too, as the phone gives.
        var (complete, all) = await CrossPlatformProbe.WithStl(first, (hash, lines));
        Assert.Equal(complete, (await CrossPlatformProbe.WithStl(second, State(two.Document, second))).Hash);
        if (Environment.GetEnvironmentVariable("DETERMINISM_HASH_FILE") is { Length: > 0 } file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
            File.WriteAllLines(file, [complete, .. all]);
        }
    }
}
