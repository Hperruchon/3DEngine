using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;

namespace Engine.Tests.Commands;

// The sphere and the torus are closed meshes that the engine makes (ADR-0089 item 5, phase K5d, TASK-0289). The refusals,
// the record of the inputs, the meshes from the unit 64-gon of ADR-0040, the face surface, and a cut of a plate.
// TASK-0457 (phase P0.186): the cut of a plate counts the booleans of the kernel, so the class runs in the collection "kept
// booleans", alone (Engine.Tests/Features/KeptBooleanCollection.cs).
[Collection("kept booleans")]
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

    // TASK-0452 (register entry R-0073): the work of the cut and of its faces, as two counts of the kernel on this thread. Each
    // bound is 1.5 times the count of today. TASK-0454 (phase P0.184): the command subtracts the sphere from the plate one
    // time, and the query of the faces takes the kept result, so the counts are half the counts of TASK-0452 (13,875,936 and
    // 189,876).
    private const long CutSplits = 6_937_968;
    private const long CutExactSigns = 94_938;

    // A sphere at the middle of a plate cuts a hole with the name surface into +z and -z of the plate.
    [NativeManifoldFact]
    public async Task A_Sphere_Cuts_A_Hole_Of_One_Name()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 }), "bodyId");
        var tool = Output(await Applied(session, new CreateSphereCommand { Radius = 5 }), "bodyId");
        var (splits, exactSigns, booleans) = (Bsp.SplitsOnThisThread, Bsp.ExactSignsOnThisThread, PlanarBoolean.BooleansOnThisThread);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        var faces = await Faces(session, cut);
        (splits, exactSigns, booleans) = (Bsp.SplitsOnThisThread - splits, Bsp.ExactSignsOnThisThread - exactSigns, PlanarBoolean.BooleansOnThisThread - booleans);

        // ADR-0089 item 6: the cut took about 3 minutes before the filter of doubles of TASK-0289. TASK-0435 (phase P0.173):
        // no time decides the test (ADR-0173 item 4), so the limit of 300 s of TASK-0300 is gone. TASK-0452: the counts of
        // the splits of a polygon by a plane and of the exact signs hold that work. The command and the query complete on
        // this thread, so a count of zero says that the work went to another thread, and the test fails then too.
        Assert.Equal((1, 1), (faces.Single(f => f.Reference == new DatumReference(plate, "+z", null)).Holes, faces.Single(f => f.Reference == new DatumReference(plate, "-z", null)).Holes));
        Assert.Contains(faces, f => f.Reference == new DatumReference(tool, "surface", null));
        Assert.Equal(1, booleans);
        Assert.True(splits > 0 && splits <= CutSplits * 3 / 2, $"The cut split {splits} polygons; the bound is 1.5 times {CutSplits}.");
        Assert.True(exactSigns > 0 && exactSigns <= CutExactSigns * 3 / 2, $"The cut computed {exactSigns} exact signs; the bound is 1.5 times {CutExactSigns}.");
    }
}
