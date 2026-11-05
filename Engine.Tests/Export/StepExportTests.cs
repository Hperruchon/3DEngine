using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Export;
using Engine.Core.Features;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;
using Engine.Kernel;
using Engine.Tests.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Export;

// ADR-0070 (phase K6a, TASK-0200): the live bodies as one STEP file of AP214, from the B-Rep of the owned kernel, from
// the engine, the route and the command line. ADR-0070 names this file in enforced-by. Each test reads the file back
// with StepReader and compares the counts of its faces, edges and vertices with Brep.Of.
public sealed class StepExportTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-step-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static DocumentSession Session(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    private static KernelSolid Box(Rational x, Rational y, Rational z) => KernelSolid.Box(Guid.NewGuid(), x, y, z);

    private static byte[] Written(params (Guid Body, Brep Brep)[] solids)
    {
        var (content, error) = StepExport.Write(solids);
        Assert.True(error is null, $"{error?.Code} {error?.Message}");
        return content!;
    }

    // The checks of each solid of the file against the B-Reps: the solids of each body in order, and for each body the
    // counts of its faces, edges and vertices, and the point of each vertex.
    private static StepReader Matches(byte[] file, params (Guid Body, Brep Brep)[] solids)
    {
        var step = StepReader.Parse(file);
        var written = step.Of("MANIFOLD_SOLID_BREP").ToList();
        Assert.Equal(solids.Sum(solid => solid.Brep.Shells.Count), written.Count);
        var at = 0;
        foreach (var (body, brep) in solids)
        {
            var (faces, edges, vertices) = (0, 0, 0);
            foreach (var _ in brep.Shells)
            {
                var solid = written[at++];
                Assert.Equal($"'{body:D}'", step.Instances[solid].Attributes[0]);
                var counts = step.Solid(solid);
                (faces, edges, vertices) = (faces + counts.Faces, edges + counts.Edges, vertices + counts.Vertices);
            }

            Assert.Equal((brep.Faces.Count, brep.Edges.Count, brep.Vertices.Count), (faces, edges, vertices));
        }

        Assert.Equal(solids.Sum(solid => solid.Brep.Faces.Count), step.Count("ADVANCED_FACE"));
        Assert.Equal(solids.Sum(solid => solid.Brep.Edges.Count), step.Count("EDGE_CURVE"));
        Assert.Equal(solids.Sum(solid => solid.Brep.Vertices.Count), step.Count("VERTEX_POINT"));
        Assert.Equal(solids.Sum(solid => solid.Brep.Holes), step.Count("FACE_BOUND"));
        Assert.Equal(
            solids.SelectMany(solid => solid.Brep.Vertices).Select(vertex => vertex.ToDoubles()),
            step.Of("VERTEX_POINT").Select(step.Vertex));

        // Each vertex of each face lies on the plane of the face, and the normal of the plane points out of the solid as
        // the normal of the kernel does.
        var faceIds = step.Of("ADVANCED_FACE").ToList();
        var kernelFaces = solids.SelectMany(solid => solid.Brep.Faces).ToList();
        for (var f = 0; f < faceIds.Count; f++)
        {
            var (origin, normal) = step.Plane(faceIds[f]);
            var (nx, ny, nz) = kernelFaces[f].Plane.Normal.ToDoubles();
            Assert.True((normal.X * nx) + (normal.Y * ny) + (normal.Z * nz) > 0, $"The plane of the face {f} faces the wrong side.");
            Assert.Equal(1, Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z)), 12);
            foreach (var (_, uses) in step.Loops(faceIds[f]))
            {
                foreach (var use in uses)
                {
                    var p = step.Vertex(step.Ends(use).Start);
                    var distance = ((p.X - origin.X) * normal.X) + ((p.Y - origin.Y) * normal.Y) + ((p.Z - origin.Z) * normal.Z);
                    Assert.True(Math.Abs(distance) < 1e-9, $"A vertex of the face {f} is {distance} mm from its plane.");
                }
            }
        }

        return step;
    }

    // Items 1 and 2: a box reads back as one solid with 6 faces, 12 edges and 8 vertices, each one at its point, with the
    // header of AP214 and the units in millimetres.
    [Fact]
    public void A_Box_Reads_Back_With_The_Counts_Of_Its_Brep()
    {
        var body = Guid.NewGuid();
        var brep = Brep.Of(Box(10, 20, 30));

        var step = Matches(Written((body, brep)), (body, brep));

        Assert.Equal((6, 12, 8), (brep.Faces.Count, brep.Edges.Count, brep.Vertices.Count));
        Assert.Contains("FILE_SCHEMA(('AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }'));", step.Header, StringComparison.Ordinal);
        Assert.Contains(step.Of("CARTESIAN_POINT"), id => step.Point(id) == (5, 10, 15));
        Assert.Equal(1, step.Count("ADVANCED_BREP_SHAPE_REPRESENTATION"));
        Assert.Equal(6, step.Count("PLANE"));
    }

    // Item 2: a cut gives a face with a hole, as a FACE_BOUND after its FACE_OUTER_BOUND; a cut across a body gives two
    // solids of one body; an extrude of a concave profile gives its faces; two bodies give their solids in their order.
    [Fact]
    public void A_Hole_A_Split_And_A_Concave_Extrude_Read_Back()
    {
        var plate = Brep.Of(PlanarBoolean.Subtract(Box(10, 10, 2), Box(4, 4, 6))!);
        var split = Brep.Of(PlanarBoolean.Subtract(Box(10, 10, 10), Box(2, 20, 20))!);
        var ell = new[] { (0, 0), (6, 0), (6, 2), (2, 2), (2, 5), (0, 5) }
            .Select(p => new ProfileVertex(p.Item1, p.Item2, Guid.NewGuid())).ToList();
        var extrude = Brep.Of(KernelSolid.Extrude(Guid.NewGuid(), ell, new Vector3R(0, 0, 0), new Vector3R(1, 0, 0), new Vector3R(0, 1, 0), new Vector3R(0, 0, 1), 3));
        var bodies = new[] { (Guid.NewGuid(), plate), (Guid.NewGuid(), split), (Guid.NewGuid(), extrude) };

        Matches(Written(bodies), bodies);

        Assert.Equal(2, plate.Holes);
        Assert.Equal(2, split.Shells.Count);
        Assert.Equal(8, extrude.Faces.Count);
    }

    // Item 3: a void inside a body, a B-Rep that is not closed, and a coordinate that a double does not hold each give
    // E-EXPORT-BODY-FAILED with the body, and no file.
    [Fact]
    public void A_Void_Is_Refused_With_Its_Code()
    {
        var body = Guid.NewGuid();
        var hollow = Brep.Of(PlanarBoolean.Subtract(Box(10, 10, 10), Box(2, 2, 2))!);
        var huge = new Rational(new System.Numerics.BigInteger(double.MaxValue) * 4, System.Numerics.BigInteger.One);
        var far = Brep.Of(Box(1, 1, 1).Translate(new Vector3R(huge, 0, 0)));

        var (content, error) = StepExport.Write([(body, hollow)]);
        var (farContent, farError) = StepExport.Write([(body, far)]);

        Assert.Equal(2, hollow.Shells.Count);
        Assert.Null(content);
        Assert.Equal(DiagnosticCodes.ExportBodyFailed, error?.Code);
        Assert.Contains(body.ToString(), error!.Message, StringComparison.Ordinal);
        Assert.Contains("void", error.Message, StringComparison.Ordinal);
        Assert.Null(farContent);
        Assert.Equal(DiagnosticCodes.ExportBodyFailed, farError?.Code);
    }

    // Item 1: a real has a point and the shortest form that reads back to the same double; minus zero is zero.
    [Theory]
    [InlineData(0.0, "0.")]
    [InlineData(1.0, "1.")]
    [InlineData(-2.5, "-2.5")]
    [InlineData(100.0, "100.")]
    [InlineData(0.1, "0.1")]
    [InlineData(1e-5, "1.E-05")]
    [InlineData(1e16, "10000000000000000.")]
    [InlineData(1.5e300, "1.5E+300")]
    [InlineData(0.30000000000000004, "0.30000000000000004")]
    public void A_Real_Has_A_Point_And_Reads_Back(double value, string text)
    {
        Assert.Equal(text, StepExport.Real(value));
        Assert.Equal(value, double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));
        Assert.Equal("0.", StepExport.Real(-0.0));
    }

    // Item 1: the bytes of a fixed body are the same in each culture and on each platform. The pipeline runs this test on
    // Windows, Linux and macOS, so the hash is the proof of the platforms; a culture with a decimal comma gives the same
    // bytes. The hash changes only with a change of ADR-0070.
    [Fact]
    public void The_Bytes_Are_The_Same_In_Each_Culture_And_On_Each_Platform()
    {
        var body = new Guid("6f1c2a4e-0b7d-4e59-9a3c-1d2e3f405162");
        var feature = new Guid("0a1b2c3d-4e5f-4a6b-8c7d-9e0f1a2b3c4d");
        var solid = PlanarBoolean.Subtract(
            KernelSolid.Box(feature, Rational.FromDouble(10.1), 20, 3),
            KernelSolid.Box(Guid.Empty, Rational.FromDouble(0.7), 2, 10).Translate(Vector3R.FromDoubles(1.0 / 3, 0.25, 0)))!;
        var invariant = Written((body, Brep.Of(solid)));

        var culture = CultureInfo.CurrentCulture;
        byte[] french;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            french = Written((body, Brep.Of(solid)));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        Assert.Equal(invariant, french);
        Assert.Equal(ExpectedHash, Convert.ToHexString(SHA256.HashData(invariant)));
    }

    // The SHA-256 of the file of The_Bytes_Are_The_Same_In_Each_Culture_And_On_Each_Platform.
    private const string ExpectedHash = "635139E40F93DE453BDC9FFAEC70321E67026287694DCCC5BC1075453161FB15";

    // Item 2: the engine writes each live body from the owned kernel: a box, an extrude of a sketch, a translate and a cut,
    // with the counts of the B-Rep of each body. The export changes neither the version nor the log.
    [NativeManifoldFact]
    public async Task The_Engine_Writes_Each_Live_Body_From_The_Owned_Kernel()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = Session(backend);
        var plate = await Body(session, new CreateBoxCommand { SizeX = 20, SizeY = 10, SizeZ = 2 });
        var tool = await Body(session, new CreateBoxCommand { SizeX = 4, SizeY = 4, SizeZ = 10 });
        var moved = await Body(session, new TranslateCommand { BodyId = tool, Dx = 3, Dy = 0, Dz = 0 });
        await Body(session, new SubtractCommand { MinuendBodyId = plate, SubtrahendBodyId = moved });
        var sketch = await Output(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var corners = new[] { (30.0, 0.0), (36.0, 0.0), (36.0, 4.0), (30.0, 4.0) };
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(await Output(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        for (var i = 0; i < points.Count; i++)
            await Output(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }, "lineId");
        await Body(session, new ExtrudeCommand { SketchId = sketch, Distance = 5 });

        var before = await session.Read((document, _) => document.Version);
        var (content, error) = await session.ReadGeometry((document, _) => StepExport.Write(document));
        var solids = await session.ReadGeometry((document, _) => document.Bodies
            .OrderBy(body => body.Handle.Id)
            .Select(body => (body.Handle.Id, Brep.Of(KernelProjection.Of(document, body.Handle.Id).Solid!)))
            .ToArray());

        Assert.True(error is null, $"{error?.Code} {error?.Message}");
        Assert.Equal(2, solids.Length);
        Matches(content!, solids);
        Assert.Equal(before, await session.Read((document, _) => document.Version));
    }

    // Items 2 and 3: a body with a fault gives E-EXPORT-BODY-FAILED with its reason, and a document with no body gives a
    // file with no solid.
    [NativeManifoldFact]
    public async Task A_Body_With_A_Fault_Is_Refused_And_No_Body_Gives_No_Solid()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = Session(backend);
        var (empty, emptyError) = await session.ReadGeometry((document, _) => StepExport.Write(document));
        var sketch = await Output(session, new CreateSketchCommand { DatumId = Datums.XY.Id }, "sketchId");
        var corners = new[] { (1.0, 2.0), (11.0, 2.0), (11.0, 8.0), (1.0, 8.0) };
        var points = new List<Guid>();
        foreach (var (x, y) in corners)
            points.Add(await Output(session, new AddSketchPointCommand { SketchId = sketch, X = x, Y = y }, "pointId"));
        for (var i = 0; i < points.Count; i++)
            await Output(session, new AddSketchLineCommand { SketchId = sketch, StartPointId = points[i], EndPointId = points[(i + 1) % points.Count] }, "lineId");
        var body = await Body(session, new ExtrudeCommand { SketchId = sketch, Distance = 3 });

        // The second corner moves above the third, so two lines cross and the rebuild gives the body a fault.
        await session.Apply(new MoveSketchPointCommand { SketchId = sketch, PointId = points[1], X = 5, Y = 12 });
        var (content, error) = await session.ReadGeometry((document, _) => StepExport.Write(document));

        Assert.Null(emptyError);
        Assert.Equal(0, StepReader.Parse(empty!).Count("MANIFOLD_SOLID_BREP"));
        Assert.Null(content);
        Assert.Equal(DiagnosticCodes.ExportBodyFailed, error?.Code);
        Assert.Contains(body.ToString(), error!.Message, StringComparison.Ordinal);
    }

    // Item 4: GET /export/step gives the bytes of the writer as model/step, and a body that the export does not write
    // gives 422 with the envelope.
    [NativeManifoldFact]
    public async Task The_Route_Gives_The_File_And_422_For_A_Body_It_Does_Not_Write()
    {
        var host = new EngineHost(new ManifoldGeometryBackend());
        await Body(host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 });
        await using var app = await StartSurface(host);
        using var http = new HttpClient();

        using var file = await http.GetAsync(app.Urls.First() + "/export/step");
        var expected = (await host.Session.ReadGeometry((document, _) => StepExport.Write(document))).Content;

        var outer = await Body(host.Session, new CreateBoxCommand { SizeX = 10, SizeY = 10, SizeZ = 10 });
        var inner = await Body(host.Session, new CreateBoxCommand { SizeX = 2, SizeY = 2, SizeZ = 2 });
        await Body(host.Session, new SubtractCommand { MinuendBodyId = outer, SubtrahendBodyId = inner });
        using var refused = await http.GetAsync(app.Urls.First() + "/export/step");

        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("model/step", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expected, await file.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal(DiagnosticCodes.ExportBodyFailed, JsonDocument.Parse(await refused.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // Item 5: the command line reads a document file and writes its STEP file, with { file, bodies, warnings }; the
    // document file does not change, and a file that cannot be written gives E-EXPORT-WRITE-FAILED.
    [NativeManifoldFact]
    public async Task The_Command_Line_Writes_The_Step_File_Of_A_Document_File()
    {
        var document = Path.Combine(_folder, "part" + DocumentFile.Extension);
        byte[] expected;
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = Session(backend);
            DocumentFile.Create(session, document);
            await Body(session, new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 });
            expected = (await session.ReadGeometry((read, _) => StepExport.Write(read))).Content!;
            session.Close();
        }

        var before = File.ReadAllBytes(document);
        var step = Path.Combine(_folder, "part.step");
        var (exit, output) = Run("export", "step", document, step);
        var (writeExit, writeOutput) = Run("export", "step", document, Path.Combine(_folder, "absent", "part.step"));
        var (usageExit, _) = Run("export", "iges", document, step);

        Assert.Equal(0, exit);
        var result = JsonDocument.Parse(output).RootElement;
        Assert.Equal((step, 1, 0), (result.GetProperty("file").GetString(), result.GetProperty("bodies").GetInt32(), result.GetProperty("warnings").GetArrayLength()));
        Assert.Equal(before, File.ReadAllBytes(document));

        Assert.Equal(expected, File.ReadAllBytes(step));
        Assert.Equal((1, DiagnosticCodes.ExportWriteFailed), (writeExit, JsonDocument.Parse(writeOutput).RootElement.GetProperty("error").GetProperty("code").GetString()));
        Assert.Equal(2, usageExit);
    }

    private static async Task<Guid> Output(DocumentSession session, Command command, string name)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(name, out var id) ? id : throw new InvalidOperationException($"No output {name}.");
    }

    private static Task<Guid> Body(DocumentSession session, Command command) => Output(session, command, "bodyId");

    private static (int Exit, string Output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var exit = global::Engine.Cli.Cli.Run(args, stdout, new StringWriter());
        return (exit, stdout.ToString());
    }

    private static async Task<WebApplication> StartSurface(EngineHost host)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddEngineSurface(_ => host);
        var app = builder.Build();
        app.MapEngineSurface();
        await app.StartAsync();
        return app;
    }
}
