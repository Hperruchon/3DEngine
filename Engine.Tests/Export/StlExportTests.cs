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

    // Finding E171 of the codebase review of 2026-11-07 (TASK-0263): apply, query and export read the asset of an import
    // from the default store only, so a document whose asset is in the folder of "asset put --folder" did not export. Now
    // each takes --folder. The triangle has a new coordinate at each run, so the default store does not hold its asset.
    [NativeManifoldFact]
    public async Task The_Command_Line_Reads_The_Asset_Of_An_Import_From_The_Folder_That_It_Names()
    {
        var store = Path.Combine(_folder, "store");
        var source = Path.Combine(_folder, "unique.stl");
        var stl = new byte[84 + 50];
        BinaryPrimitives.WriteUInt32LittleEndian(stl.AsSpan(80), 1);
        float[] triangle = [Random.Shared.Next(1, 1_000_000), 0, 0, 0, 10, 0, 0, 0, 10];
        for (var i = 0; i < 9; i++)
            BinaryPrimitives.WriteSingleLittleEndian(stl.AsSpan(84 + 12 + (i * 4)), triangle[i]);
        File.WriteAllBytes(source, stl);

        var (putExit, putOutput) = Run("asset", "put", source, "--folder", store);
        var asset = JsonDocument.Parse(putOutput).RootElement.GetProperty("asset").GetString()!;
        var (applyExit, _) = Run("apply", "ImportStl", "--folder", store, "--param", $"asset={asset}");
        var (defaultExit, defaultOutput) = Run("apply", "ImportStl", "--param", $"asset={asset}");
        var (queryExit, _) = Run("query", "GetBodyInfo", "--kind", "part", "--folder", store, "--param", $"bodyId={Guid.NewGuid()}");
        var document = Path.Combine(_folder, "imported" + DocumentFile.Extension);
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = new DocumentSession(EngineHosting.CreateDefault(backend, assets: new Engine.Core.Assets.AssetStore(store)));
            DocumentFile.Create(session, document);
            Assert.Equal(CommandStatus.Applied, (await session.Apply(new ImportStlCommand { Asset = asset })).Status);
            session.Close();
        }

        var (exportExit, exportOutput) = Run("export", "stl", document, Path.Combine(_folder, "out.stl"), "--folder", store);

        Assert.Equal(0, putExit);
        Assert.Equal(0, applyExit);
        Assert.Equal(1, defaultExit);
        Assert.Contains(DiagnosticCodes.StlAsset, defaultOutput, StringComparison.Ordinal);
        // A body that does not exist: the query reads the options and refuses the body (1), not the usage (2).
        Assert.Equal(1, queryExit);
        Assert.True(exportExit == 0, exportOutput);
        Assert.Equal(1, JsonDocument.Parse(exportOutput).RootElement.GetProperty("triangles").GetInt32());
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

    // Finding E119 of the codebase review of 2026-11-01 (TASK-0160): the export reads the document file and changes
    // nothing. An incomplete last line stays in the file and gives W-IO-TRUNCATED in the output, and a file that a
    // live session holds, as the desktop does, gives its bodies.
    [NativeManifoldFact]
    public async Task The_Command_Line_Reads_The_Document_File_And_Changes_Nothing()
    {
        var document = Path.Combine(_folder, "torn" + DocumentFile.Extension);
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = Session(backend);
            DocumentFile.Create(session, document);
            await session.Apply(new CreateBoxCommand { SizeX = 10, SizeY = 20, SizeZ = 30 });
            session.Close();
        }

        File.AppendAllText(document, "{\"version\":2,\"comm");
        var before = File.ReadAllBytes(document);
        var stl = Path.Combine(_folder, "torn.stl");

        var (exit, output) = Run("export", "stl", document, stl);

        Assert.Equal(0, exit);
        Assert.Equal(before, File.ReadAllBytes(document));
        var warning = Assert.Single(JsonDocument.Parse(output).RootElement.GetProperty("warnings").EnumerateArray());
        Assert.Equal(DiagnosticCodes.IoTruncated, warning.GetProperty("code").GetString());

        using var live = new ManifoldGeometryBackend();
        var holder = Session(live);
        File.WriteAllBytes(document, before[..^"{\"version\":2,\"comm".Length]);
        await DocumentFile.Open(holder, document);
        var (heldExit, heldOutput) = Run("export", "stl", document, stl);
        holder.Close();

        Assert.Equal(0, heldExit);
        Assert.Equal(12, JsonDocument.Parse(heldOutput).RootElement.GetProperty("triangles").GetInt32());
    }

    // Finding E122: a file that cannot be written gives E-EXPORT-WRITE-FAILED, and an empty path of the document
    // gives E-IO-LOAD-FAILED with its own message.
    [NativeManifoldFact]
    public async Task The_Command_Line_Gives_The_Code_Of_The_Export_And_Of_An_Empty_Path()
    {
        var document = Path.Combine(_folder, "part" + DocumentFile.Extension);
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = Session(backend);
            DocumentFile.Create(session, document);
            await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
            session.Close();
        }

        var (writeExit, writeOutput) = Run("export", "stl", document, Path.Combine(_folder, "absent", "part.stl"));
        var (emptyExit, emptyOutput) = Run("export", "stl", "", Path.Combine(_folder, "part.stl"));

        var write = JsonDocument.Parse(writeOutput).RootElement.GetProperty("error");
        var empty = JsonDocument.Parse(emptyOutput).RootElement.GetProperty("error");
        Assert.Equal((1, DiagnosticCodes.ExportWriteFailed), (writeExit, write.GetProperty("code").GetString()));
        Assert.Equal((1, DiagnosticCodes.IoLoadFailed, "The path of the document file is empty."), (emptyExit, empty.GetProperty("code").GetString(), empty.GetProperty("message").GetString()));
    }

    // The review of Codex of v0.198, finding 1 (TASK-0220): `engine export step part.3de part.3de` wrote the STEP text over
    // the document file, and the document no longer opened. An export onto its own document file, also by another spelling
    // of the path, gives E-EXPORT-WRITE-FAILED and leaves each byte of the document.
    [NativeManifoldTheory]
    [InlineData("stl")]
    [InlineData("step")]
    public async Task An_Export_Onto_Its_Own_Document_File_Is_Refused_And_The_Document_Stays(string format)
    {
        var document = Path.Combine(_folder, "part" + DocumentFile.Extension);
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = Session(backend);
            DocumentFile.Create(session, document);
            await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
            session.Close();
        }
        var before = File.ReadAllBytes(document);

        var other = Path.Combine(_folder, ".", "part" + DocumentFile.Extension);
        foreach (var target in new[] { document, other })
        {
            var (exit, output) = Run("export", format, document, target);
            var error = JsonDocument.Parse(output).RootElement.GetProperty("error");
            Assert.Equal((1, DiagnosticCodes.ExportWriteFailed), (exit, error.GetProperty("code").GetString()));
            Assert.Equal(before, File.ReadAllBytes(document));
        }
    }

    private static async Task<(string Path, byte[] Bytes)> Part(string folder)
    {
        var document = Path.Combine(folder, "part" + DocumentFile.Extension);
        using (var backend = new ManifoldGeometryBackend())
        {
            var session = Session(backend);
            DocumentFile.Create(session, document);
            await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 2, SizeZ = 3 });
            session.Close();
        }

        return (document, File.ReadAllBytes(document));
    }

    // A link of a folder: a junction on Windows, which needs no right of an administrator, and a symbolic link on Linux and
    // macOS, as WriteSetJudgeTests makes it.
    private static void FolderLink(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            using var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            })!;
            mklink.StandardOutput.ReadToEnd();
            mklink.WaitForExit();
            Assert.True(mklink.ExitCode == 0, "mklink /J failed");
        }
        else
        {
            Directory.CreateSymbolicLink(link, target);
        }
    }

    // The review of Codex of v0.200, finding 1 (TASK-0221): the folder alias is a link to the folder models, so
    // alias/part.3de is the document models/part.3de. The compare of P0.87 resolved a link of the last part of a path only,
    // and the export wrote over the document.
    [NativeManifoldTheory]
    [InlineData("stl")]
    [InlineData("step")]
    public async Task An_Export_Onto_Its_Document_Through_A_Link_Of_A_Folder_Is_Refused(string format)
    {
        var (models, alias) = (Path.Combine(_folder, "models"), Path.Combine(_folder, "alias"));
        Directory.CreateDirectory(models);
        var (document, before) = await Part(models);
        FolderLink(alias, models);
        try
        {
            var (exit, output) = Run("export", format, document, Path.Combine(alias, "part" + DocumentFile.Extension));

            var error = JsonDocument.Parse(output).RootElement.GetProperty("error");
            Assert.Equal((1, DiagnosticCodes.ExportWriteFailed), (exit, error.GetProperty("code").GetString()));
            Assert.Equal(before, File.ReadAllBytes(document));
        }
        finally
        {
            Directory.Delete(alias);
        }
    }

    // The review of Codex of v0.200, finding 2 (TASK-0221): the compare of P0.87 called Path.GetFullPath outside the
    // handler of the export, so a path with a NUL character threw out of Cli.Run. Before P0.87 the write gave
    // E-EXPORT-WRITE-FAILED, and now the compare gives it.
    [NativeManifoldTheory]
    [InlineData("stl")]
    [InlineData("step")]
    public async Task A_Path_Of_The_Export_That_Is_Not_Valid_Gives_Its_Code(string format)
    {
        var (document, before) = await Part(_folder);

        var (exit, output) = Run("export", format, document, Path.Combine(_folder, "a\0b." + format));

        var error = JsonDocument.Parse(output).RootElement.GetProperty("error");
        Assert.Equal((1, DiagnosticCodes.ExportWriteFailed), (exit, error.GetProperty("code").GetString()));
        Assert.Equal(before, File.ReadAllBytes(document));
    }

    // Finding E121: a vertex that is not finite as a float gives E-EXPORT-RANGE and no file, and a control box at the
    // edge of the range of a float gives its file.
    [NativeManifoldFact]
    public async Task A_Vertex_Out_Of_The_Range_Of_A_Float_Is_Refused()
    {
        using var backend = new ManifoldGeometryBackend();
        var session = Session(backend);
        var box = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        Assert.True(box.Outputs.TryGet<Guid>("bodyId", out var body));
        var near = await session.Apply(new TranslateCommand { BodyId = body, Dx = 1e38, Dy = 0, Dz = 0 });
        Assert.True(near.Outputs.TryGet<Guid>("bodyId", out var moved));

        var (control, controlError) = await session.ReadGeometry(StlExport.Write);
        await session.Apply(new TranslateCommand { BodyId = moved, Dx = 1e39, Dy = 0, Dz = 0 });
        var (content, error) = await session.ReadGeometry(StlExport.Write);

        Assert.Null(controlError);
        Assert.All(Triangles(control!).SelectMany(t => t.Vertices.Append(t.Normal)).SelectMany(v => v), value => Assert.True(float.IsFinite(value)));
        Assert.Null(content);
        Assert.Equal(DiagnosticCodes.ExportRange, error!.Code);
    }

    // Finding E121: more triangles than one file holds gives E-EXPORT-TOO-LARGE, and no OverflowException. The
    // backend gives one mesh of a million triangles for each of 44 bodies.
    [Fact]
    public void More_Triangles_Than_A_File_Holds_Are_Refused()
    {
        var document = new Document();
        for (var i = 0; i < 44; i++)
            document.AddBody(new BodyRecord(new BodyHandle(Guid.NewGuid()), "Box"));

        var (content, error) = StlExport.Write(document, new LargeMeshBackend());

        Assert.Null(content);
        Assert.Equal(DiagnosticCodes.ExportTooLarge, error!.Code);
    }

    // A backend whose each body has one shared mesh of a million triangles with no area.
    private sealed class LargeMeshBackend : IGeometryBackend, ITessellationOps
    {
        private static readonly Tessellation Mesh = new([0, 0, 0], new uint[3_000_000]);

        public BackendCapabilities Capabilities => BackendCapabilities.None;

        public BackendIdentity Identity => new("large", "1");

        public T? TryGet<T>() where T : class => this as T;

        public Tessellation Tessellate(BodyHandle handle) => Mesh;
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
