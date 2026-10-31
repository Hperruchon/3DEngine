using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Export;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Export;

// ADR-0048 (phase P8g, TASK-0157): the live bodies as one binary STL file, from the engine, the route and the
// command line. ADR-0048 names this file in enforced-by.
public sealed class StlExportTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("3de-stl-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static DocumentSession Session(IGeometryBackend backend) => new(EngineHosting.CreateDefault(backend));

    // The triangles of a file: the normal and the three vertices of each one.
    private static List<(float[] Normal, float[][] Vertices)> Triangles(byte[] file)
    {
        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(80));
        Assert.Equal(84 + (50 * count), file.Length);
        float Read(int at) => BinaryPrimitives.ReadSingleLittleEndian(file.AsSpan(at));
        return Enumerable.Range(0, count)
            .Select(t => 84 + (50 * t))
            .Select(at => (
                new[] { Read(at), Read(at + 4), Read(at + 8) },
                Enumerable.Range(1, 3).Select(v => new[] { Read(at + (12 * v)), Read(at + (12 * v) + 4), Read(at + (12 * v) + 8) }).ToArray()))
            .ToList();
    }

    // ADR-0048 items 1 and 2: a box gives its twelve triangles, each normal is a unit vector out of the box, and
    // each vertex is the position of the tessellation as a float.
    [NativeManifoldFact]
    public async Task A_Box_Gives_Twelve_Triangles_With_Unit_Normals_Out_Of_It()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = Session(backend);
        var box = await session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 });
        Assert.Equal(CommandStatus.Applied, box.Status);

        var (content, error) = await session.ReadGeometry(StlExport.Write);

        Assert.Null(error);
        Assert.StartsWith(StlExport.Header, Encoding.ASCII.GetString(content!, 0, 80), StringComparison.Ordinal);
        var triangles = Triangles(content!);
        Assert.Equal(12, triangles.Count);
        var (centre, mesh) = await session.ReadGeometry((document, b) =>
        {
            var key = new BodyHandle(document.Bodies.Single().GeometryKey);
            var aabb = b.TryGet<IGeometryQuery>()!.GetBoundingBox(key);
            return (((aabb.MinX + aabb.MaxX) / 2, (aabb.MinY + aabb.MaxY) / 2, (aabb.MinZ + aabb.MaxZ) / 2), b.TryGet<ITessellationOps>()!.Tessellate(key));
        });
        var positions = mesh.Positions.Select(value => (float)value).ToArray();
        foreach (var (normal, vertices) in triangles)
        {
            Assert.Equal(1, Math.Sqrt((normal[0] * normal[0]) + (normal[1] * normal[1]) + (normal[2] * normal[2])), 5);
            var middle = Enumerable.Range(0, 3).Select(axis => vertices.Average(vertex => vertex[axis])).ToArray();
            var outward = ((middle[0] - centre.Item1) * normal[0]) + ((middle[1] - centre.Item2) * normal[1]) + ((middle[2] - centre.Item3) * normal[2]);
            Assert.True(outward > 0, "A normal points into the box.");
            Assert.All(vertices, vertex => Assert.Contains(Enumerable.Range(0, positions.Length / 3), i =>
                positions[i * 3] == vertex[0] && positions[(i * 3) + 1] == vertex[1] && positions[(i * 3) + 2] == vertex[2]));
        }
    }

    // ADR-0048 item 2: each live body, so two boxes give twice the triangles; a document with no body gives a
    // file with no triangle; the managed backend has no tessellation and gives its code.
    [NativeManifoldFact]
    public async Task Each_Live_Body_Enters_The_File_And_A_Backend_With_No_Triangles_Gives_Its_Code()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = Session(backend);
        var empty = (await session.ReadGeometry(StlExport.Write)).Content!;
        await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
        await session.Apply(new CreateBoxCommand { SizeX = 4, SizeY = 5, SizeZ = 6 });
        var two = (await session.ReadGeometry(StlExport.Write)).Content!;
        var managed = Session(new InProcessMeshBackend());
        await managed.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });

        var (content, error) = await managed.ReadGeometry(StlExport.Write);

        Assert.Empty(Triangles(empty));
        Assert.Equal(24, Triangles(two).Count);
        Assert.Null(content);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, error?.Code);
    }

    // ADR-0048 item 4: GET /export/stl gives the bytes of the writer, and a backend with no tessellation gives 501
    // with the envelope.
    [NativeManifoldFact]
    public async Task The_Route_Gives_The_File_And_501_For_A_Backend_With_No_Triangles()
    {
        var native = new EngineHost(new ManifoldGeometryBackend());
        await native.Session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 });
        var managed = new EngineHost(new InProcessMeshBackend());
        await using var nativeApp = await StartSurface(native);
        await using var managedApp = await StartSurface(managed);
        using var http = new HttpClient();

        using var file = await http.GetAsync(nativeApp.Urls.First() + "/export/stl");
        using var refused = await http.GetAsync(managedApp.Urls.First() + "/export/stl");

        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("model/stl", file.Content.Headers.ContentType?.MediaType);
        Assert.Equal((await native.Session.ReadGeometry(StlExport.Write)).Content, await file.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotImplemented, refused.StatusCode);
        Assert.Equal(DiagnosticCodes.GeomCapMissing, JsonDocument.Parse(await refused.Content.ReadAsStringAsync()).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    // ADR-0048 item 5: the command line opens a document file and writes its bodies; a file that does not open
    // gives its code and the exit code 1, and a wrong form gives the exit code 2.
    [NativeManifoldFact]
    public async Task The_Command_Line_Writes_The_Bodies_Of_A_Document_File()
    {
        var document = Path.Combine(_folder, "part" + DocumentFile.Extension);
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = Session(backend);
            DocumentFile.Create(session, document);
            await session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 });
            session.Close();
        }

        var stl = Path.Combine(_folder, "part.stl");
        var (exit, output) = Run("export", "stl", document, stl);
        var (absentExit, absentOutput) = Run("export", "stl", Path.Combine(_folder, "absent" + DocumentFile.Extension), stl);
        var (usageExit, _) = Run("export", "obj", document, stl);

        Assert.Equal(0, exit);
        Assert.Equal(12, JsonDocument.Parse(output).RootElement.GetProperty("triangles").GetInt32());
        Assert.Equal(12, Triangles(File.ReadAllBytes(stl)).Count);
        Assert.Equal(1, absentExit);
        Assert.StartsWith("E-IO-", JsonDocument.Parse(absentOutput).RootElement.GetProperty("error").GetProperty("code").GetString(), StringComparison.Ordinal);
        Assert.Equal(2, usageExit);
    }

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
