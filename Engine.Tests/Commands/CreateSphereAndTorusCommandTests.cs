using System.Diagnostics;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Commands;

// The sphere and the torus are closed meshes that the engine makes (ADR-0089 item 5, phase K5d, TASK-0289). The refusals,
// the record of the inputs, the meshes from the unit 64-gon of ADR-0040, the face surface, and a cut of a plate.
public sealed class CreateSphereAndTorusCommandTests
{
    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static Guid Output(CommandResult result, string name)
        => result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");

    private static DocumentSession NewSession(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static async Task<IReadOnlyList<FaceEntry>> Faces(DocumentSession session, Guid body)
    {
        var result = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!.Faces;
    }

    // Each directed edge appears one time, and its reverse one time, so the mesh is closed and oriented.
    private static void ClosedAndOriented(int[] triangles)
    {
        var edges = new HashSet<(int, int)>();
        for (var t = 0; t < triangles.Length; t += 3)
        {
            foreach (var (a, b) in new[] { (triangles[t], triangles[t + 1]), (triangles[t + 1], triangles[t + 2]), (triangles[t + 2], triangles[t]) })
                Assert.True(edges.Add((a, b)), $"The edge {a}-{b} appears two times in one direction.");
        }

        Assert.All(edges, edge => Assert.Contains((edge.Item2, edge.Item1), edges));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public async Task A_Sphere_Radius_That_Is_Not_Positive_And_Finite_Is_Refused(double radius)
    {
        var result = await NewSession(new InProcessMeshBackend()).Apply(new CreateSphereCommand { Radius = radius });

        Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.GeomInvalidParam), (result.Status, result.Error?.Code));
    }

    // A tube radius that is not less than the radius gives a torus that cuts itself, so it is refused.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(double.NaN, 1)]
    [InlineData(2, double.PositiveInfinity)]
    public async Task A_Torus_Size_That_Is_Not_Valid_Is_Refused(double radius, double tubeRadius)
    {
        var result = await NewSession(new InProcessMeshBackend()).Apply(new CreateTorusCommand { Radius = radius, TubeRadius = tubeRadius });

        Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.GeomInvalidParam), (result.Status, result.Error?.Code));
    }

    [Fact]
    public async Task A_Backend_With_No_Solid_Mesh_Gives_The_Missing_Capability()
    {
        var session = NewSession(new InProcessMeshBackend());

        var sphere = await session.Apply(new CreateSphereCommand { Radius = 2 });
        var torus = await session.Apply(new CreateTorusCommand { Radius = 3, TubeRadius = 1 });

        Assert.Equal((DiagnosticCodes.GeomCapMissing, DiagnosticCodes.GeomCapMissing), (sphere.Error?.Code, torus.Error?.Code));
    }

    [Fact]
    public void The_Records_Hold_The_Sizes_Only()
    {
        var sphere = new CreateSphereCommandHandler().Encode(new CreateSphereCommand { Radius = 2.5 });
        var torus = new CreateTorusCommandHandler().Encode(new CreateTorusCommand { Radius = 4, TubeRadius = 1.5 });

        Assert.Equal(["radius"], sphere.Keys);
        Assert.Equal(["radius", "tubeRadius"], torus.Keys);
        Assert.Equal((2.5, 4.0, 1.5), ((double)sphere["radius"]!, (double)torus["radius"]!, (double)torus["tubeRadius"]!));
        Assert.Contains(HandlerCatalog.CommandHandlers(), item => item.CommandName == "CreateSphere");
        Assert.Contains(HandlerCatalog.CommandHandlers(), item => item.CommandName == "CreateTorus");
    }

    // The sphere has 31 rings of 64 points and two poles; the equator holds (radius, 0, 0) exactly, and each point of a
    // ring is the product of the radius and two values of the unit table.
    [Fact]
    public void The_Sphere_Mesh_Is_Closed_And_Comes_From_The_Table()
    {
        var (positions, triangles) = CreateSphereCommandHandler.Mesh(5);
        var unit = CreateCylinderCommandHandler.Circle(1);

        Assert.Equal(((64 * 31) + 2, 3968), (positions.Length / 3, triangles.Length / 3));
        Assert.Equal((5.0, 0.0, 0.0), (positions[3 * 64 * 15], positions[(3 * 64 * 15) + 1], positions[(3 * 64 * 15) + 2]));
        var at = 3 * ((64 * 16) + 3);
        Assert.Equal((5 * unit[1].X * unit[3].X, 5 * unit[1].X * unit[3].Y, 5 * unit[1].Y), (positions[at], positions[at + 1], positions[at + 2]));
        ClosedAndOriented(triangles);
    }

    // The torus has 64 positions of a tube of 32 points; the outer equator holds (radius + tube, 0, 0) exactly.
    [Fact]
    public void The_Torus_Mesh_Is_Closed_And_Comes_From_The_Table()
    {
        var (positions, triangles) = CreateTorusCommandHandler.Mesh(4, 1.5);

        Assert.Equal((64 * 32, 4096), (positions.Length / 3, triangles.Length / 3));
        Assert.Equal((5.5, 0.0, 0.0), (positions[0], positions[1], positions[2]));
        ClosedAndOriented(triangles);
    }

    // A sphere and a torus each have the one face surface with no normal and no plane, and the bounds of Manifold are the
    // bounds of the mesh. A datum on the face is refused.
    [NativeManifoldFact]
    public async Task A_Sphere_And_A_Torus_Have_One_Curved_Face()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var sphere = Output(await Applied(session, new CreateSphereCommand { Radius = 5 }), "bodyId");
        var torus = Output(await Applied(session, new CreateTorusCommand { Radius = 4, TubeRadius = 1.5 }), "bodyId");

        foreach (var body in new[] { sphere, torus })
        {
            var face = Assert.Single(await Faces(session, body));
            Assert.Equal((new DatumReference(body, "surface", null), (DatumAxis?)null), (face.Reference, face.Normal));
            var datum = await session.Apply(new CreateDatumCommand { FeatureId = body, Element = "surface" });
            Assert.Equal(CommandStatus.Rejected, datum.Status);
            Assert.Contains("curved", datum.Error!.Message, StringComparison.Ordinal);
        }

        var query = backend.TryGet<IGeometryQuery>()!;
        var (s, t) = (query.GetBoundingBox(new BodyHandle(sphere)), query.GetBoundingBox(new BodyHandle(torus)));
        Assert.Equal((5.0, -5.0, 5.0, 5.5), (s.MaxX, s.MinZ, s.MaxZ, t.MaxX));
        Assert.True(Math.Abs(t.MaxZ - 1.5) < 1e-12, $"{t.MaxZ}");
    }

    // A sphere at the middle of a plate cuts a hole with the name surface into +z and -z of the plate.
    [NativeManifoldFact]
    public async Task A_Sphere_Cuts_A_Hole_Of_One_Name()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 }), "bodyId");
        var tool = Output(await Applied(session, new CreateSphereCommand { Radius = 5 }), "bodyId");
        var clock = Stopwatch.StartNew();
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        var faces = await Faces(session, cut);
        clock.Stop();

        Assert.Equal((1, 1), (faces.Single(f => f.Reference == new DatumReference(plate, "+z", null)).Holes, faces.Single(f => f.Reference == new DatumReference(plate, "-z", null)).Holes));
        Assert.Contains(faces, f => f.Reference == new DatumReference(tool, "surface", null));
        // ADR-0089 item 6: about 10 s on the computer of the owner, and about 3 minutes before TASK-0289. The runner of macOS
        // took 64 s in the main run 42445348622, about six times as long, so the form before TASK-0289 would take about 18
        // minutes there. TASK-0300: the limit of 300 s keeps that difference and leaves room for a slow runner.
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(300), $"The cut and its faces took {clock.Elapsed}.");
    }
}
