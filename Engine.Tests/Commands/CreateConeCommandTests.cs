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

// The cone is a closed mesh that the engine makes (ADR-0089, phase K5c, TASK-0288). ADR-0089 names this file in
// enforced-by. The refusals, the record of the inputs, the mesh from the circle of ADR-0040, the two face names, a datum
// on the base and a refusal on the side, and a cut of a plate by a cone.
public sealed class CreateConeCommandTests
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

    private static async Task<Guid> Cone(DocumentSession session, double radius, double height)
        => Output(await Applied(session, new CreateConeCommand { Radius = radius, Height = height }), "bodyId");

    private static async Task<IReadOnlyList<FaceEntry>> Faces(DocumentSession session, Guid body)
    {
        var result = await session.Query<FacesInfo>(new GetFacesQuery { BodyId = body });
        Assert.True(result.Error is null, $"{result.Error?.Code} {result.Error?.Message}");
        return result.Result!.Faces;
    }

    private static FaceEntry Named(IReadOnlyList<FaceEntry> faces, Guid feature, string element)
        => Assert.Single(faces, face => face.Reference == new DatumReference(feature, element, null));

    // Item 2: a radius or a height that is not finite and greater than zero gives E-GEOM-INVALID-PARAM, before the check
    // of the capability.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -2)]
    [InlineData(double.NaN, 1)]
    [InlineData(1, double.PositiveInfinity)]
    public async Task A_Size_That_Is_Not_Positive_And_Finite_Is_Refused(double radius, double height)
    {
        var session = NewSession(new InProcessMeshBackend());

        var result = await session.Apply(new CreateConeCommand { Radius = radius, Height = height });

        Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.GeomInvalidParam), (result.Status, result.Error?.Code));
    }

    // Item 1: a backend with no ISolidMeshOps gives E-GEOM-CAP-MISSING.
    [Fact]
    public async Task A_Backend_With_No_Solid_Mesh_Gives_The_Missing_Capability()
    {
        var session = NewSession(new InProcessMeshBackend());

        var result = await session.Apply(new CreateConeCommand { Radius = 2, Height = 3 });

        Assert.Equal((CommandStatus.Rejected, DiagnosticCodes.GeomCapMissing), (result.Status, result.Error?.Code));
    }

    // Item 2: the record holds the inputs only (ADR-0007), and the catalog gives the command to each host.
    [Fact]
    public void The_Record_Holds_The_Radius_And_The_Height_Only()
    {
        var handler = new CreateConeCommandHandler();
        var command = new CreateConeCommand { Radius = 2.5, Height = 7 };

        var encoded = handler.Encode(command);

        Assert.Equal(["radius", "height"], encoded.Keys);
        Assert.Equal((2.5, 7.0), ((double)encoded["radius"]!, (double)encoded["height"]!));
        Assert.Contains(HandlerCatalog.CommandHandlers(), item => item.CommandName == "CreateCone");
    }

    // Item 2: the base is the circle of a sketch at -height / 2, the apex is at +height / 2, and the mesh is closed and
    // oriented: each directed edge appears one time, and its reverse one time.
    [Fact]
    public void The_Mesh_Is_The_Circle_And_The_Apex_And_It_Is_Closed()
    {
        var (positions, triangles) = CreateConeCommandHandler.Mesh(5, 8);
        var circle = CreateCylinderCommandHandler.Circle(5);

        Assert.Equal((66 * 3, 128 * 3), (positions.Length, triangles.Length));
        for (var i = 0; i < 64; i++)
            Assert.Equal((circle[i].X, circle[i].Y, -4.0), (positions[3 * i], positions[(3 * i) + 1], positions[(3 * i) + 2]));
        Assert.Equal((0.0, 0.0, 4.0), (positions[3 * 64], positions[(3 * 64) + 1], positions[(3 * 64) + 2]));
        Assert.Equal((0.0, 0.0, -4.0), (positions[3 * 65], positions[(3 * 65) + 1], positions[(3 * 65) + 2]));

        var edges = new HashSet<(int, int)>();
        for (var t = 0; t < triangles.Length; t += 3)
        {
            foreach (var (a, b) in new[] { (triangles[t], triangles[t + 1]), (triangles[t + 1], triangles[t + 2]), (triangles[t + 2], triangles[t]) })
                Assert.True(edges.Add((a, b)), $"The edge {a}-{b} appears two times in one direction.");
        }

        Assert.All(edges, edge => Assert.Contains((edge.Item2, edge.Item1), edges));
    }

    // Items 1 and 3: the body has the two names -z and side with no item. The base is a plane at -height / 2 with the
    // area of the polygon, the side is one part with no normal, and the bounds of Manifold are the base and the apex.
    [NativeManifoldFact]
    public async Task A_Cone_Gives_Two_Named_Faces()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = await Cone(session, 5, 10);

        var faces = await Faces(session, body);

        Assert.Equal(["-z", "side"], faces.Select(face => face.Reference.Element).Order(StringComparer.Ordinal));
        var (bottom, side) = (Named(faces, body, "-z"), Named(faces, body, "side"));
        Assert.Equal((new DatumAxis(0, 0, -1), (DatumAxis?)null, -5.0), (bottom.Normal, side.Normal, bottom.Point.Z));
        var polygon = 32 * 25 * Math.Sin(Math.PI / 32);
        Assert.True(Math.Abs(bottom.Area - polygon) < 1e-9 * polygon, $"{bottom.Area} against {polygon}");

        var bounds = backend.TryGet<IGeometryQuery>()!.GetBoundingBox(new BodyHandle(body));
        Assert.Equal((5.0, -5.0, 5.0), (bounds.MaxX, bounds.MinZ, bounds.MaxZ));
    }

    // Item 3: a datum on -z of a moved cone has the frame of the base, moved; the side has no plane.
    [NativeManifoldFact]
    public async Task A_Datum_Takes_The_Base_And_Refuses_The_Side()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = await Cone(session, 2, 6);
        await Applied(session, new TranslateCommand { BodyId = body, Dx = 1, Dy = 2, Dz = 3 });

        var datum = Output(await Applied(session, new CreateDatumCommand { FeatureId = body, Element = "-z" }), "datumId");
        var frame = await session.Read((document, _) => document.Datums.Single(item => item.Id == datum).Frame);
        Assert.Equal(new DatumFrame(new DatumPoint(1, 2, 0), new DatumAxis(0, 1, 0), new DatumAxis(1, 0, 0), new DatumAxis(0, 0, -1)), frame);

        var side = await session.Apply(new CreateDatumCommand { FeatureId = body, Element = "side" });
        Assert.Equal(CommandStatus.Rejected, side.Status);
        Assert.Contains("curved", side.Error!.Message, StringComparison.Ordinal);
    }

    // Finding E186 of the codebase review of 2026-11-08 (TASK-0295): the volume of the Manifold solid of a cone is the volume
    // of the pyramid on the 64-gon, (1/3) x 32 r^2 sin(pi/32) x h, and positive. Until TASK-0295 a mesh with each triangle
    // reversed gave the same volume with the sign minus, and no test of the cone saw it.
    [NativeManifoldTheory]
    [InlineData(5.0, 10.0)]
    [InlineData(0.5, 3.0)]
    [InlineData(40.0, 2.0)]
    public async Task The_Volume_Of_A_Cone_Is_Its_Pyramid_And_Positive(double radius, double height)
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var body = await Cone(session, radius, height);

        var pyramid = 32 * radius * radius * Math.Sin(Math.PI / 32) * height / 3;
        var volume = backend.Volume(new BodyHandle(body));
        Assert.True(Math.Abs(volume - pyramid) < 1e-9 * pyramid, $"{volume} against {pyramid}");
    }

    // E186: a closed mesh with each triangle clockwise from outside is refused, and the backend holds nothing under the
    // handle. Until TASK-0295 Manifold took it with the volume -261.379 for the cone of radius 5 and height 10.
    [NativeManifoldFact]
    public void A_Mesh_With_Inward_Triangles_Is_Refused()
    {
        using var backend = new ManifoldGeometryBackend();
        var (positions, triangles) = CreateConeCommandHandler.Mesh(5, 10);
        var reversed = (int[])triangles.Clone();
        for (var t = 0; t < reversed.Length; t += 3)
            (reversed[t + 1], reversed[t + 2]) = (reversed[t + 2], reversed[t + 1]);
        var handle = new BodyHandle(Guid.NewGuid());

        var refusal = Assert.Throws<ManifoldNativeException>(() => backend.CreateSolid(handle, positions, reversed));

        Assert.Contains("counter-clockwise", refusal.Message, StringComparison.Ordinal);
        backend.CreateSolid(handle, positions, triangles);
        Assert.True(backend.Volume(handle) > 0);
    }

    // Finding E194 of the codebase review of 2026-11-08 (b) (TASK-0302): a cone so small that its volume underflows to zero is
    // made, as before TASK-0295, so a document with it opens. Until TASK-0302 the check of the volume refused it.
    [NativeManifoldFact]
    public void A_Cone_Whose_Volume_Underflows_Is_Made()
    {
        using var backend = new ManifoldGeometryBackend();
        var (positions, triangles) = CreateConeCommandHandler.Mesh(1e-108, 1e-108);
        var handle = new BodyHandle(Guid.NewGuid());

        backend.CreateSolid(handle, positions, triangles);

        Assert.Equal(0.0, backend.Volume(handle));
    }

    // Items 1 and 3 with ADR-0057: a cone through a plate cuts a hole with the name side into +z and -z of the plate.
    [NativeManifoldFact]
    public async Task A_Cone_Cuts_A_Hole_Of_One_Name()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = NewSession(backend);
        var plate = Output(await Applied(session, new CreateBoxCommand { SizeX = 20, SizeY = 20, SizeZ = 4 }), "bodyId");
        var tool = await Cone(session, 6, 10);
        var cut = Output(await Applied(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = tool }), "bodyId");

        var faces = await Faces(session, cut);

        Assert.Equal((1, 1), (Named(faces, plate, "+z").Holes, Named(faces, plate, "-z").Holes));
        Assert.Equal((1, (DatumAxis?)null), (Named(faces, tool, "side").Parts, Named(faces, tool, "side").Normal));
        Assert.DoesNotContain(faces, face => face.Reference.FeatureId == tool && face.Reference.Element != "side");
        Assert.True(Named(faces, plate, "+z").Area > Named(faces, plate, "-z").Area, "The hole at the top of the plate is not the smaller one.");
    }
}
