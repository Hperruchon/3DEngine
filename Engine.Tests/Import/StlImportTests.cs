using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Sketch;
using Engine.Core;
using Engine.Core.Assets;
using Engine.Core.Commands;
using Engine.Core.Determinism;
using Engine.Core.Export;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Import;
using Engine.Core.Queries;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;
using Engine.Tests.Governance;

namespace Engine.Tests.Import;

// ADR-0083 (phase P9b, TASK-0258): the reader of STL, the command ImportStl, the mesh body and the operations that it
// refuses, and the two files of the demonstration "Open the same STL on desktop and Android". ADR-0083 names this file
// in its enforced-by field.
public sealed class StlImportTests
{
    // A binary STL file of the triangles, with a normal of zeros, as nine floats for each triangle.
    private static byte[] Binary(params float[] triangles)
    {
        var count = triangles.Length / 9;
        var bytes = new byte[84 + (50 * count)];
        Encoding.ASCII.GetBytes("a test file").CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(80), (uint)count);
        for (var t = 0; t < count; t++)
            for (var i = 0; i < 9; i++)
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(84 + (t * 50) + 12 + (i * 4)), triangles[(t * 9) + i]);
        return bytes;
    }

    private static readonly float[] Triangle = [0f, 0f, 0f, 10f, 0f, 0f, 0f, 10f, 0f];

    private static string Text(string body) => $"solid test\n{body}endsolid test\n";

    private const string Facet = "facet normal 0 0 1\n outer loop\n  vertex 0 0 0\n  vertex 10 0 0\n  vertex 0 10 0\n endloop\nendfacet\n";

    // Item 3: a binary file gives the bits of each float, with no arithmetic, also for a value with no short decimal form
    // and for a subnormal value; the normal is not read.
    [Fact]
    public void A_Binary_File_Gives_The_Bits_Of_Each_Float()
    {
        float[] triangles = [0.1f, -2.5f, 1e-40f, 1234.5678f, 0f, -0f, 3f, float.Epsilon, 7.25f];

        var (read, error) = StlReader.Read(Binary(triangles));

        Assert.Null(error);
        Assert.Equal(triangles.Select(BitConverter.SingleToInt32Bits), read!.Select(BitConverter.SingleToInt32Bits));
    }

    // Item 3: a text file gives each number as the nearest 32-bit float, and a header of a binary file that starts with
    // "solid" is still binary when the length agrees with its count.
    [Fact]
    public void A_Text_File_And_A_Binary_File_That_Starts_With_Solid_Read()
    {
        var (text, textError) = StlReader.Read(Encoding.ASCII.GetBytes(Text(Facet.Replace("vertex 10 0 0", "vertex 0.1 1e-3 -2.5E2", StringComparison.Ordinal))));
        var binary = Binary(Triangle);
        Encoding.ASCII.GetBytes("solid but binary").CopyTo(binary, 0);
        var (fromBinary, binaryError) = StlReader.Read(binary);

        Assert.Null(textError);
        Assert.Equal([0f, 0f, 0f, float.Parse("0.1", CultureInfo.InvariantCulture), 0.001f, -250f, 0f, 10f, 0f], text!);
        Assert.Null(binaryError);
        Assert.Equal(Triangle, fromBinary);
    }

    // Item 3: each fault has its own code.
    [Fact]
    public void Each_Fault_Has_Its_Code()
    {
        var full = Binary([.. Triangle, .. Triangle]);
        string? Code(byte[] bytes) => StlReader.Read(bytes).Error?.Code;
        var notFinite = Binary(0f, 0f, 0f, float.NaN, 0f, 0f, 0f, 1f, 0f);
        var tooLarge = new byte[84 + (50L * (StlReader.MaximumTriangles + 1))];
        BinaryPrimitives.WriteUInt32LittleEndian(tooLarge.AsSpan(80), StlReader.MaximumTriangles + 1);

        Assert.Equal(DiagnosticCodes.StlTruncated, Code(full[..(full.Length / 2)]));
        Assert.Equal(DiagnosticCodes.StlTruncated, Code([1, 2, 3]));
        Assert.Equal(DiagnosticCodes.StlEmpty, Code(Binary()));
        Assert.Equal(DiagnosticCodes.StlEmpty, Code(Encoding.ASCII.GetBytes(Text(string.Empty))));
        Assert.Equal(DiagnosticCodes.StlNotFinite, Code(notFinite));
        Assert.Equal(DiagnosticCodes.StlTooLarge, Code(tooLarge));
        Assert.Equal(DiagnosticCodes.StlSyntax, Code(Encoding.ASCII.GetBytes(Text(Facet.Replace("vertex 10 0 0", "vertex ten 0 0", StringComparison.Ordinal)))));
        Assert.Equal(DiagnosticCodes.StlSyntax, Code(Encoding.ASCII.GetBytes($"solid test\n{Facet}")));
        Assert.Equal(DiagnosticCodes.StlSyntax, Code([.. "solid "u8, 0xFF, 0xFE]));
    }

    // Finding E170 of the codebase review of 2026-11-07 (TASK-0263): three forms of a file gave a code that item 3 does not
    // give. A cut binary file whose header starts with "solid" gave E-STL-SYNTAX; a name of the solid with the word
    // "endsolid" or "facet" gave E-STL-EMPTY or E-STL-SYNTAX; a text file with a byte order mark gave E-STL-TRUNCATED.
    [Fact]
    public void A_Cut_Binary_File_With_Solid_A_Name_With_A_Keyword_And_A_Byte_Order_Mark_Have_The_Codes_Of_Item_3()
    {
        var cut = Binary([.. Triangle, .. Triangle, .. Triangle, .. Triangle]);
        new byte[80].CopyTo(cut, 0);
        Encoding.ASCII.GetBytes("solid exported by a program").CopyTo(cut, 0);
        string? Code(byte[] bytes) => StlReader.Read(bytes).Error?.Code;

        Assert.Equal(DiagnosticCodes.StlTruncated, Code(cut[..150]));
        foreach (var name in new[] { "old endsolid copy", "facet", "facet normal" })
        {
            var (named, error) = StlReader.Read(Encoding.ASCII.GetBytes($"solid {name}\n{Facet}endsolid {name}\n"));
            Assert.True(error is null, $"{name}: {error?.Code} {error?.Message}");
            Assert.Equal(Triangle, named);
        }

        var (marked, markError) = StlReader.Read([0xEF, 0xBB, 0xBF, .. Encoding.ASCII.GetBytes(Text(Facet))]);
        Assert.Null(markError);
        Assert.Equal(Triangle, marked);
    }

    // Finding E173 of the codebase review of 2026-11-07 (b) (TASK-0269): the fix of E170 changed the result of the reader for
    // files that v0.243 read. A recorded import reads its asset again at each replay, so each file that v0.243 read gives
    // the result of v0.243 (ADR-0083 item 3). Each expected result below is the result of the reader of v0.243.
    [Fact]
    public void Each_Text_File_That_The_Reader_Of_V0243_Read_Gives_Its_Result()
    {
        var one = Facet.Replace("\n", " ", StringComparison.Ordinal);
        var second = Facet.Replace("vertex 10 0 0", "vertex 20 0 0", StringComparison.Ordinal);
        float[] secondTriangle = [0f, 0f, 0f, 20f, 0f, 0f, 0f, 10f, 0f];
        (string Form, string Text, float[] Triangles)[] forms =
        [
            ("a facet on the line of solid, then a facet on its own lines", $"solid part {one}\n{second}endsolid part\n", [.. Triangle, .. secondTriangle]),
            ("one line with a final LF", $"solid part {one} endsolid part\n", Triangle),
            ("one line with a final CRLF", $"solid part {one} endsolid part\r\n", Triangle),
            ("one line with no line end", $"solid part {one} endsolid part", Triangle),
            ("one line with no name and a final LF", $"solid {one} endsolid\n", Triangle),
            ("a name and the facets on lines of their own", Text(Facet), Triangle),
            ("facet and normal on two lines", $"solid part facet\nnormal 0 0 1\n outer loop\n  vertex 0 0 0\n  vertex 10 0 0\n  vertex 0 10 0\n endloop\nendfacet\nendsolid part\n", Triangle),
        ];

        foreach (var (form, text, triangles) in forms)
        {
            var (read, error) = StlReader.Read(Encoding.ASCII.GetBytes(text));
            Assert.True(error is null, $"{form}: {error?.Code} {error?.Message}");
            Assert.Equal(triangles, read);
        }

        Assert.Equal(DiagnosticCodes.StlEmpty, StlReader.Read(Encoding.ASCII.GetBytes("solid part endsolid part")).Error?.Code);
    }

    // The review of Codex of v0.254 (TASK-0274): the reader of v0.250 to v0.253 took the whole line of "solid" as the name, so
    // it read a name with "facet normal" and fewer than three numbers, which the rule of TASK-0269 refused. Each expected result
    // below is the result of the reader of v0.250; the rule keeps the results of v0.243 too (the test above).
    [Fact]
    public void Each_Text_File_That_The_Reader_Of_V0250_Read_Gives_Its_Result()
    {
        (string Form, string Text)[] forms =
        [
            ("a name of facet normal and one number", $"solid facet normal 0\n{Facet}endsolid\n"),
            ("a name of facet normal and two numbers", $"solid facet normal 0 0\n{Facet}endsolid\n"),
            ("a name of facet normal and three numbers", $"solid facet normal 0 0 1\n{Facet}endsolid\n"),
            ("a name of facet normal", $"solid facet normal\n{Facet}endsolid\n"),
        ];

        foreach (var (form, text) in forms)
        {
            var (read, error) = StlReader.Read(Encoding.ASCII.GetBytes(text));
            Assert.True(error is null, $"{form}: {error?.Code} {error?.Message}");
            Assert.Equal(Triangle, read);
        }
    }

    // Finding E178 of the codebase review of 2026-11-07 (b) (TASK-0272): a text file whose name is not UTF-8 gives the code of
    // a text file, and a cut binary file of fewer than 84 bytes whose header starts with "solid" gives the code of a cut file.
    // Until TASK-0272 they gave the opposite codes.
    [Fact]
    public void A_Name_That_Is_Not_Utf8_And_A_Short_Cut_Binary_File_Have_The_Codes_Of_Item_3()
    {
        byte[] latin = [.. Encoding.ASCII.GetBytes("solid caf"), 0xE9, .. Encoding.ASCII.GetBytes($"\n{Facet}endsolid\n")];
        var binary = Binary([.. Triangle, .. Triangle]);
        new byte[80].CopyTo(binary, 0);
        Encoding.ASCII.GetBytes("solid exported by a program").CopyTo(binary, 0);

        Assert.True(latin.Length >= 84);
        Assert.Equal(DiagnosticCodes.StlSyntax, StlReader.Read(latin).Error?.Code);
        Assert.Equal(DiagnosticCodes.StlTruncated, StlReader.Read(binary[..83]).Error?.Code);
    }

    private static (DocumentSession Session, AssetBytes Assets) Session(IGeometryBackend backend)
    {
        var assets = new AssetBytes();
        return (new DocumentSession(EngineHosting.CreateDefault(backend, assets: assets)), assets);
    }

    // Items 1 and 8: the import gives a mesh body with the name and the floats of the file, on the managed backend too.
    [Fact]
    public async Task An_Import_Gives_A_Mesh_Body_With_The_Floats_Of_The_File()
    {
        var (session, assets) = Session(new InProcessMeshBackend());
        var asset = assets.Add(Binary(Triangle));

        var result = await session.Apply(new ImportStlCommand { Asset = asset, BodyName = "part.stl" });

        Assert.Equal(CommandStatus.Applied, result.Status);
        Assert.True(result.Outputs.TryGet<Guid>("bodyId", out var body));
        var record = await session.Read((document, _) => document.Bodies.Single());
        Assert.Equal((body, ImportStlCommandHandler.MeshKind, "part.stl"), (record.Handle.Id, record.Kind, record.Name));
        session.Close();
    }

    // Item 2: a host with no store, a name that the store does not hold, and a text that is not a name refuse the import.
    [Fact]
    public async Task The_Asset_And_The_Unit_Are_Checked()
    {
        var noStore = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var (session, _) = Session(new InProcessMeshBackend());
        var name = new string('a', 64);

        Assert.Equal(DiagnosticCodes.StlAsset, (await noStore.Apply(new ImportStlCommand { Asset = name })).Error?.Code);
        Assert.Equal(DiagnosticCodes.StlAsset, (await session.Apply(new ImportStlCommand { Asset = name })).Error?.Code);
        Assert.Equal(DiagnosticCodes.StlAsset, (await session.Apply(new ImportStlCommand { Asset = "not a name" })).Error?.Code);
        foreach (var each in new[] { noStore, session })
            each.Close();
    }

    // Item 4: a mesh body is viewed, framed and exported as STL; each other operation refuses it with E-MESH-UNSUPPORTED,
    // and the name and the visibility stay a part of the view.
    [NativeManifoldFact]
    public async Task A_Mesh_Body_Is_Viewed_And_Exported_As_STL_And_Each_Other_Operation_Refuses_It()
    {
        using var backend = new ManifoldGeometryBackend();
        var (session, assets) = Session(backend);
        var stl = Binary(Triangle);
        Assert.True((await session.Apply(new ImportStlCommand { Asset = assets.Add(stl) })).Outputs.TryGet<Guid>("bodyId", out var mesh));
        Assert.True((await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 })).Outputs.TryGet<Guid>("bodyId", out var box));

        Command[] refused =
        [
            new TranslateCommand { BodyId = mesh, Dx = 1, Dy = 0, Dz = 0 },
            new SubtractCommand { MinuendBodyId = mesh, SubtrahendBodyId = box },
            new SubtractCommand { MinuendBodyId = box, SubtrahendBodyId = mesh },
            new CreateDatumCommand { FeatureId = mesh, Element = "+z" },
        ];
        foreach (var command in refused)
            Assert.Equal(DiagnosticCodes.MeshUnsupported, (await session.Apply(command)).Error?.Code);
        var sketch = await session.Apply(new CreateSketchCommand { DatumId = Datums.XY.Id });
        Assert.True(sketch.Outputs.TryGet<Guid>("sketchId", out var sketchId));
        Assert.Equal(DiagnosticCodes.MeshUnsupported, (await session.Apply(new AddSketchReferenceCommand { SketchId = sketchId, FeatureId = mesh, Element = "endEdge" })).Error?.Code);
        Assert.Equal(DiagnosticCodes.MeshUnsupported, (await session.Query<FacesInfo>(new GetFacesQuery { BodyId = mesh })).Error?.Code);
        Assert.Equal(DiagnosticCodes.MeshUnsupported, (await session.Query<FaceInfo>(new GetFaceAtQuery { BodyId = mesh, X = 1, Y = 1, Z = 0, Nx = 0, Ny = 0, Nz = 1 })).Error?.Code);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(new RenameBodyCommand { BodyId = mesh, NewName = "bracket" })).Status);
        Assert.Equal(CommandStatus.Applied, (await session.Apply(new SetBodyVisibilityCommand { BodyId = mesh, Visible = false })).Status);

        var tessellation = (await session.Query<Tessellation>(new GetTessellationQuery { BodyId = mesh })).Result!;
        Assert.Equal(Triangle.Select(value => (double)value), tessellation.Positions);
        var box3 = (await session.Query<Aabb>(new GetBoundingBoxQuery { BodyId = mesh })).Result!;
        Assert.Equal((0.0, 0.0, 0.0, 10.0, 10.0, 0.0), (box3.MinX, box3.MinY, box3.MinZ, box3.MaxX, box3.MaxY, box3.MaxZ));
        var (step, stepError) = await session.Read((document, _) => StepExport.Write(document));
        Assert.Null(step);
        Assert.Equal(DiagnosticCodes.MeshUnsupported, stepError?.Code);
        session.Close();
    }

    // Item 2: a replay of the log with the same store gives the same mesh body.
    [NativeManifoldFact]
    public async Task A_Replay_With_The_Store_Gives_The_Same_Mesh()
    {
        using var backend = new ManifoldGeometryBackend();
        var assets = new AssetBytes();
        var asset = assets.Add(Binary([.. Triangle, .. Triangle.Select(value => value + 1f)]));
        var registry = new CommandRegistry();
        HandlerCatalog.RegisterAll(registry, new QueryRegistry(), assets);
        var id = Guid.NewGuid();

        var replay = await Replay.ReplayLog([new ImportStlCommand { CommandId = id, Asset = asset }], registry, backend);

        Assert.Equal(ImportStlCommandHandler.MeshKind, replay.Document.Bodies.Single().Kind);
        Assert.Equal(18, backend.Tessellate(new BodyHandle(id)).Positions.Length);
    }

    // Criterion 10 of the demonstration: testdata/stl/bracket.stl is the STL export of the bracket of the probe, and
    // bracket-damaged.stl its first half; the files give the triangles and the code that the probe compares on Windows,
    // Linux, macOS and the emulator. With STL_WRITE_TESTDATA=1 the test writes the two files.
    [NativeManifoldFact]
    public async Task The_Files_Of_The_Demonstration_Are_The_Bracket_Of_The_Probe()
    {
        using var backend = new ManifoldGeometryBackend();
        var stl = await CrossPlatformProbe.BracketStl(backend);
        var (good, damaged) = (RepositoryFiles.Path("testdata", "stl", "bracket.stl"), RepositoryFiles.Path("testdata", "stl", "bracket-damaged.stl"));
        if (Environment.GetEnvironmentVariable("STL_WRITE_TESTDATA") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(good)!);
            await File.WriteAllBytesAsync(good, stl);
            await File.WriteAllBytesAsync(damaged, stl[..(stl.Length / 2)]);
        }

        Assert.Equal(stl, await File.ReadAllBytesAsync(good));
        Assert.Equal(stl[..(stl.Length / 2)], await File.ReadAllBytesAsync(damaged));
        var (triangles, error) = StlReader.Read(await File.ReadAllBytesAsync(good));
        Assert.Null(error);
        Assert.Equal(DiagnosticCodes.StlTruncated, StlReader.Read(await File.ReadAllBytesAsync(damaged)).Error?.Code);
        var (_, lines) = await CrossPlatformProbe.Import(new ManifoldGeometryBackend(), stl);
        Assert.Equal($"stl bracket.stl triangles {triangles!.Length / 9} bracket-damaged.stl {DiagnosticCodes.StlTruncated}", lines[^1]);
    }
}
