using System.Net.Http.Json;
using System.Text.Json;
using Engine.Api.Http;
using Engine.Api.Http.WebSockets;
using Engine.Contracts;
using Engine.Contracts.Assembly;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Engine.Tests.Assembly;

// The container model (ADR-0071, phase P8b1, TASK-0202). ADR-0071 names this file in enforced-by. A document is a part
// or an assembly; a part file of an earlier version opens as a part with the same bytes; an assembly holds references
// to parts at a fixed version, and refuses each command of a part; the file, the events, undo and redo, the query, the
// snapshot, the route and the command line.
public sealed class ContainerModelTests : IDisposable
{
    // The header and one entry of a part file as each version of the engine before TASK-0202 wrote it (ADR-0015
    // items 2 and 3), with the managed backend.
    private const string OldHeader = "{\"format\":\"3dengine-document\",\"formatVersion\":1,\"lengthUnit\":\"mm\"}";

    // Finding E154 of the codebase review of 2026-11-06 (b) (TASK-0224, Q1 A): the header of an assembly since TASK-0224.
    private const string AssemblyHeader = "{\"format\":\"3dengine-document\",\"formatVersion\":2,\"lengthUnit\":\"mm\",\"kind\":\"assembly\"}";

    private const string OldEntry = "{\"version\":1,\"backend\":{\"name\":\"managed\",\"version\":\"1\"},\"command\":{\"name\":\"CreateBox\",\"schemaVersion\":1,"
        + "\"commandId\":\"5b0c1f4e-2a6d-4c1b-9e3f-7a8b9c0d1e2f\",\"expectedDocumentVersion\":null,\"parameters\":{\"sizeX\":10,\"sizeY\":20,\"sizeZ\":30}}}";

    private static readonly string Part = new('d', 64);

    private readonly string _folder = Directory.CreateTempSubdirectory("3de-assembly-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession(string kind)
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend(), kind: kind);
        return (new DocumentSession(kit), kit.Events);
    }

    private static AddPartReferenceCommand Reference(long version = 3) => new() { Part = Part, Version = version, X = 10, Y = -2.5, Z = 0.125 };

    private static async Task<CommandResult> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static async Task<PartReferencesInfo> References(DocumentSession session)
    {
        var result = await session.Query<PartReferencesInfo>(new GetPartReferencesQuery());
        Assert.Null(result.Error);
        return result.Result!;
    }

    // ADR-0071 item 4: a part file of an earlier version opens as a part with its bodies, and the open leaves its bytes as
    // they were. A new part file has the same header as before, so its bytes do not change either.
    [Fact]
    public async Task An_Older_Part_File_Opens_As_A_Part_And_A_New_Part_File_Has_The_Same_Header()
    {
        var old = Path.Combine(_folder, "old" + DocumentFile.Extension);
        File.WriteAllText(old, OldHeader + "\n" + OldEntry + "\n");
        var before = File.ReadAllBytes(old);

        Assert.Equal(Document.Part, DocumentFile.ReadKind(old));
        var (session, _) = NewSession(DocumentFile.ReadKind(old));
        var warnings = await DocumentFile.Open(session, old);
        var (kind, bodies, version) = await session.Read((document, _) => (document.Kind, document.Bodies.Count, document.Version));
        var references = await References(session);
        session.Close();

        Assert.Empty(warnings);
        Assert.Equal((Document.Part, 1, 1L), (kind, bodies, version));
        Assert.Equal(before, File.ReadAllBytes(old));
        Assert.Equal((Document.Part, 0), (references.Kind, references.References.Count));

        var made = Path.Combine(_folder, "new" + DocumentFile.Extension);
        var (fresh, _) = NewSession(Document.Part);
        DocumentFile.Create(fresh, made);
        await Applied(fresh, new CreateBoxCommand { CommandId = new Guid("5b0c1f4e-2a6d-4c1b-9e3f-7a8b9c0d1e2f"), SizeX = 10, SizeY = 20, SizeZ = 30 });
        fresh.Close();

        Assert.Equal(before, File.ReadAllBytes(made));
    }

    // ADR-0071 items 2, 3 and 5: an assembly holds a reference with each field, the event carries the record, and a
    // command of a part is refused before its handler, with no change of the version.
    [Fact]
    public async Task An_Assembly_Holds_References_And_Refuses_Each_Command_Of_A_Part()
    {
        var (session, events) = NewSession(Document.Assembly);
        var add = Reference();
        var result = await Applied(session, add);

        Assert.True(result.Outputs.TryGet<Guid>("referenceId", out var id));
        Assert.Equal(add.CommandId, id);
        var info = await References(session);
        Assert.Equal(Document.Assembly, info.Kind);
        Assert.Equal([new PartReference(id, Part, 3, 10, -2.5, 0.125)], info.References);
        var created = events.Snapshot().Single(record => record.Kind == "reference.created");
        var payload = (IReadOnlyDictionary<string, object?>)created.Payload!;
        Assert.Equal(
            [id, Part, 3L, 10.0, -2.5, 0.125],
            new[] { "referenceId", "part", "version", "x", "y", "z" }.Select(key => payload[key]));

        Assert.Equal(DiagnosticCodes.CommandKindRefused, await Refused(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 }));
        Assert.Equal(DiagnosticCodes.CommandKindRefused, await Refused(session, new CreateSketchCommand { DatumId = Engine.Contracts.Sketch.Datums.XY.Id }));
        Assert.Equal((1L, 0), await session.Read((document, _) => (document.Version, document.Bodies.Count)));
        await Applied(session, new NoOpCommand { Echo = "each kind" });
    }

    // ADR-0071 item 2: a part refuses a reference, and keeps each other command.
    [Fact]
    public async Task A_Part_Refuses_A_Reference()
    {
        var (session, _) = NewSession(Document.Part);

        Assert.Equal(DiagnosticCodes.CommandKindRefused, await Refused(session, Reference()));
        await Applied(session, new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        Assert.Empty((await References(session)).References);
    }

    // ADR-0071 item 3: the name of a part that is not a name of the store, a version below 1 and a place that is not
    // finite give E-ASSEMBLY-REFERENCE-INVALID. A command sent again after the cache of results forgot it gives
    // E-ASSEMBLY-ID-IN-USE, as for an image: 1024 refused Redo commands fill the cache.
    [Fact]
    public async Task A_Reference_With_A_Bad_Field_Is_Refused()
    {
        var (session, _) = NewSession(Document.Assembly);
        var first = await Applied(session, Reference());

        Assert.Equal(DiagnosticCodes.AssemblyReferenceInvalid, await Refused(session, Reference() with { Part = "part.3de" }));
        Assert.Equal(DiagnosticCodes.AssemblyReferenceInvalid, await Refused(session, Reference(version: 0)));
        Assert.Equal(DiagnosticCodes.AssemblyReferenceInvalid, await Refused(session, Reference() with { Y = double.NaN }));
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand());
        Assert.Equal(DiagnosticCodes.AssemblyIdInUse, await Refused(session, Reference() with { CommandId = first.CommandId }));
        Assert.Single((await References(session)).References);
    }

    // ADR-0071 item 5: an Undo removes the reference with reference.removed, and a Redo gives it back.
    [Fact]
    public async Task Undo_And_Redo_Remove_And_Give_Back_A_Reference()
    {
        var (session, events) = NewSession(Document.Assembly);
        var add = Reference();
        await Applied(session, add);

        await Applied(session, new UndoCommand());
        var undone = (await References(session)).References;
        await Applied(session, new RedoCommand());
        var redone = (await References(session)).References;

        Assert.Empty(undone);
        Assert.Equal(add.CommandId, Assert.Single(redone).Id);
        Assert.Equal(
            ["reference.created", "reference.removed", "reference.created"],
            events.Snapshot().Select(record => record.Kind).Where(kind => kind.StartsWith("reference.", StringComparison.Ordinal)));
    }

    // Finding E153 of the codebase review of 2026-11-06 (b) (TASK-0224): Replay.ReplayLog made a part, so the replay of an
    // assembly log refused its first AddPartReference with E-CMD-KIND-REFUSED. With the kind of the header it gives the
    // references and the events of the live session.
    [Fact]
    public async Task The_Replay_Of_An_Assembly_Log_Gives_Its_References_And_Its_Events()
    {
        var (session, events) = NewSession(Document.Assembly);
        await Applied(session, Reference());
        await Applied(session, Reference(version: 5));
        await Applied(session, new UndoCommand());
        await Applied(session, new RedoCommand());
        var log = await session.Read((document, _) => document.Log.ToList());
        var live = (await References(session)).References;
        var backend = new InProcessMeshBackend();

        var replay = await Replay.ReplayLog(log, EngineHosting.CreateDefault(backend, kind: Document.Assembly).CommandRegistry, backend, Document.Assembly);

        Assert.Equal(Document.Assembly, replay.Document.Kind);
        Assert.Equal(live.Select(reference => reference.Id).Order(), replay.Document.References.Select(reference => reference.Id).Order());
        Assert.Equal(events.Snapshot().Select(record => record.Kind), replay.Events.Snapshot().Select(record => record.Kind));
    }

    // ADR-0071 item 4: an assembly file has "kind":"assembly" in its header and opens again with its references; a part
    // session refuses it, also with no entry, and a header with another kind does not open.
    [Fact]
    public async Task An_Assembly_File_Opens_Again_Only_As_An_Assembly()
    {
        var path = Path.Combine(_folder, "assembly" + DocumentFile.Extension);
        var (session, _) = NewSession(Document.Assembly);
        DocumentFile.Create(session, path);
        await Applied(session, Reference());
        await Applied(session, Reference(version: 9) with { X = -4 });
        var written = (await References(session)).References;
        session.Close();

        // Finding E154 (TASK-0224, Q1 A): the header of a new assembly has the version 2.
        Assert.Equal(AssemblyHeader, File.ReadLines(path).First());
        Assert.Equal(Document.Assembly, DocumentFile.ReadKind(path));
        var (again, _) = NewSession(DocumentFile.ReadKind(path));
        await DocumentFile.Open(again, path);
        Assert.Equal(written, (await References(again)).References);
        again.Close();

        var (part, _) = NewSession(Document.Part);
        var refused = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(part, path));
        Assert.Equal(DiagnosticCodes.IoLoadFailed, refused.Code);

        // The header decides, so an assembly with no entry does not open as an empty part either.
        var empty = Path.Combine(_folder, "empty" + DocumentFile.Extension);
        File.WriteAllText(empty, OldHeader[..^1] + ",\"kind\":\"assembly\"}\n");
        var (blank, _) = NewSession(Document.Part);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, (await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(blank, empty))).Code);

        var other = Path.Combine(_folder, "other" + DocumentFile.Extension);
        File.WriteAllText(other, OldHeader[..^1] + ",\"kind\":\"drawing\"}\n");
        Assert.Equal(DiagnosticCodes.IoLoadFailed, Assert.Throws<DocumentFileException>(() => DocumentFile.ReadKind(other)).Code);
    }

    // Finding E154 of the codebase review of 2026-11-06 (b) (TASK-0224, Q1 A): an engine before TASK-0202 refuses each
    // header whose version is not 1, so the version 2 of an assembly stops it before it writes part entries. An assembly
    // file of TASK-0202 to TASK-0224 has the version 1 with the field kind, and it still opens as an assembly. A header of
    // the version 2 that is not an assembly, and a header of the version 3, do not open.
    [Fact]
    public async Task An_Assembly_Has_The_Version_2_And_An_Assembly_Of_The_Version_1_Still_Opens()
    {
        Assert.Equal(AssemblyHeader, EntryCodec.Header(Document.Millimetre, Document.Assembly));
        Assert.Equal(OldHeader, EntryCodec.Header(Document.Millimetre));

        var earlier = Path.Combine(_folder, "earlier" + DocumentFile.Extension);
        var (maker, _) = NewSession(Document.Assembly);
        DocumentFile.Create(maker, earlier);
        await Applied(maker, Reference());
        maker.Close();
        var lines = File.ReadAllLines(earlier);
        lines[0] = OldHeader[..^1] + ",\"kind\":\"assembly\"}";
        File.WriteAllText(earlier, string.Join('\n', lines) + "\n");
        var (again, _) = NewSession(DocumentFile.ReadKind(earlier));
        await DocumentFile.Open(again, earlier);
        Assert.Equal(Document.Assembly, again.Kind);
        Assert.Single((await References(again)).References);
        again.Close();

        foreach (var header in new[]
        {
            "{\"format\":\"3dengine-document\",\"formatVersion\":2,\"lengthUnit\":\"mm\"}",
            "{\"format\":\"3dengine-document\",\"formatVersion\":2,\"lengthUnit\":\"mm\",\"kind\":\"part\"}",
            "{\"format\":\"3dengine-document\",\"formatVersion\":3,\"lengthUnit\":\"mm\",\"kind\":\"assembly\"}",
        })
        {
            var file = Path.Combine(_folder, Guid.NewGuid().ToString("N") + DocumentFile.Extension);
            File.WriteAllText(file, header + "\n");
            Assert.Equal(DiagnosticCodes.IoLoadFailed, Assert.Throws<DocumentFileException>(() => DocumentFile.ReadKind(file)).Code);
        }
    }

    // ADR-0071 item 6: the snapshot of a reset gives the kind and each reference, and a part gives its kind and none.
    [Fact]
    public async Task The_Snapshot_Gives_The_Kind_And_The_References()
    {
        var (assembly, _) = NewSession(Document.Assembly);
        var add = Reference();
        await Applied(assembly, add);
        var (part, _) = NewSession(Document.Part);

        var snapshot = await assembly.Read((document, _) => SnapshotProjector.Project(document, seq: 0));
        var empty = await part.Read((document, _) => SnapshotProjector.Project(document, seq: 0));

        Assert.Equal(Document.Assembly, snapshot.Kind);
        Assert.Equal([new SnapshotReferenceEntry(add.CommandId, Part, 3, 10, -2.5, 0.125)], snapshot.References);
        Assert.Equal((Document.Part, 0), (empty.Kind, empty.References.Count));
    }

    // ADR-0071 items 4 and 6: a host asked for an assembly makes one, GET /document gives the kind, and a file that
    // exists keeps its own kind: a part file with the kind assembly does not open.
    [Fact]
    public async Task A_Host_Makes_An_Assembly_And_Gives_Its_Kind()
    {
        using var host = new EngineHost(new InProcessMeshBackend(), new DocumentFileOptions(null, Kind: Document.Assembly));
        await Applied(host.Session, Reference());
        await using var app = await StartSurface(host);
        using var http = new HttpClient();

        var state = await http.GetFromJsonAsync<JsonElement>(app.Urls.First() + "/document");

        Assert.Equal(("assembly", 1L), (state.GetProperty("kind").GetString(), state.GetProperty("version").GetInt64()));
        var old = Path.Combine(_folder, "old" + DocumentFile.Extension);
        File.WriteAllText(old, OldHeader + "\n" + OldEntry + "\n");
        Assert.Equal(DiagnosticCodes.IoLoadFailed, Assert.Throws<DocumentFileException>(
            () => new EngineHost(new InProcessMeshBackend(), new DocumentFileOptions(old, Kind: Document.Assembly))).Code);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, Assert.Throws<DocumentFileException>(
            () => new EngineHost(new InProcessMeshBackend(), new DocumentFileOptions(null, Kind: "drawing"))).Code);
    }

    // ADR-0071 item 4 and ADR-0002: the command line applies a reference in an assembly with --kind assembly, refuses it
    // in a part, and refuses another kind with the exit code 2.
    [Fact]
    public void The_Command_Line_Applies_A_Reference_In_An_Assembly()
    {
        var (exit, output) = Run("apply", "AddPartReference", "--kind", "assembly", "--param", "part=" + Part, "--param", "version=4", "--param", "x=1.5");
        var (partExit, partOutput) = Run("apply", "AddPartReference", "--param", "part=" + Part, "--param", "version=4");
        var (queryExit, queryOutput) = Run("query", "GetPartReferences", "--kind", "assembly");
        var (usageExit, _) = Run("apply", "NoOp", "--kind", "drawing", "--param", "echo=x");

        Assert.Equal(0, exit);
        Assert.Equal("Applied", JsonDocument.Parse(output).RootElement.GetProperty("status").GetString());
        Assert.Equal(1, partExit);
        Assert.Equal(DiagnosticCodes.CommandKindRefused, JsonDocument.Parse(partOutput).RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(0, queryExit);
        Assert.Equal("assembly", JsonDocument.Parse(queryOutput).RootElement.GetProperty("result").GetProperty("kind").GetString());
        Assert.Equal(2, usageExit);
    }

    // Finding E155 of the codebase review of 2026-11-06 (b) (TASK-0224): the export made a part session, so an export of an
    // assembly gave E-IO-LOAD-FAILED with the reason "the Document is a part". It gives E-EXPORT-KIND-REFUSED, writes no
    // file, and leaves the document.
    [Theory]
    [InlineData("stl")]
    [InlineData("step")]
    public async Task An_Export_Of_An_Assembly_Is_Refused_With_Its_Reason(string format)
    {
        var path = Path.Combine(_folder, "assembly" + DocumentFile.Extension);
        var (session, _) = NewSession(Document.Assembly);
        DocumentFile.Create(session, path);
        await Applied(session, Reference());
        session.Close();
        var before = File.ReadAllBytes(path);
        var target = Path.Combine(_folder, "assembly." + format);

        var (exit, output) = Run("export", format, path, target);

        Assert.Equal((1, DiagnosticCodes.ExportKindRefused), (exit, JsonDocument.Parse(output).RootElement.GetProperty("error").GetProperty("code").GetString()));
        Assert.False(File.Exists(target));
        Assert.Equal(before, File.ReadAllBytes(path));
    }

    // Finding E156 of the codebase review of 2026-11-06 (c) (TASK-0231): the read of the kind of E155 was outside the
    // handler of the export, so a path of the document with a NUL character threw ArgumentException out of Cli.Run.
    [Theory]
    [InlineData("stl")]
    [InlineData("step")]
    public void An_Export_Of_A_Document_Path_That_Is_Not_Valid_Gives_Its_Code(string format)
    {
        var (exit, output) = Run("export", format, Path.Combine(_folder, "a\0b.3de"), Path.Combine(_folder, "out." + format));

        Assert.Equal(1, exit);
        Assert.Equal(DiagnosticCodes.IoLoadFailed, JsonDocument.Parse(output).RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    private static (int Exit, string Output) Run(params string[] args)
    {
        var stdout = new StringWriter();
        var exit = global::Engine.Cli.Cli.RunAsync(args, stdout, new StringWriter(), CancellationToken.None, global::Engine.Cli.BackendOptions.ManagedForTests).GetAwaiter().GetResult();
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
