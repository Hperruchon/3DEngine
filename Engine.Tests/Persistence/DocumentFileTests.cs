using System.Text;
using Engine.Contracts;
using Engine.Contracts.Geometry;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Geometry.Manifold;
using Engine.Tests.Geometry;

namespace Engine.Tests.Persistence;

// ADR-0015 (TASK-0091): the document file. Each test works in its own folder under
// the folder of the system for scratch files, and removes it.
public sealed class DocumentFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-tests-" + Guid.NewGuid().ToString("N"));

    public DocumentFileTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string FileNamed(string name) => Path.Combine(_folder, name + DocumentFile.Extension);

    private static DocumentSession NewSession(IGeometryBackend? backend = null)
        => new(EngineHosting.CreateDefault(backend ?? new InProcessMeshBackend()));

    private static async Task<Guid> ApplyScene(DocumentSession session)
    {
        var box = await session.Apply(new CreateBoxCommand { SizeX = 2, SizeY = 3, SizeZ = 0.1 });
        var body = (Guid)box.Outputs.Values["bodyId"]!;
        var moved = await session.Apply(new TranslateCommand { BodyId = body, Dx = -0.0, Dy = 1.5, Dz = 1e-310 });
        var cutter = await session.Apply(new CreateBoxCommand { SizeX = 0.5, SizeY = 0.5, SizeZ = 5 });
        var cut = await session.Apply(new SubtractCommand { MinuendBodyId = (Guid)moved.Outputs.Values["bodyId"]!, SubtrahendBodyId = (Guid)cutter.Outputs.Values["bodyId"]! });
        var result = (Guid)cut.Outputs.Values["bodyId"]!;
        await session.Apply(new RenameBodyCommand { BodyId = result, NewName = "plate é 𝄞" });
        await session.Apply(new SetBodyVisibilityCommand { BodyId = result, Visible = false });
        return result;
    }

    // A tuple compares its arrays by reference, so the tests compare each part.
    private static void AssertSameState((long Version, string[] Log, string[] Bodies) expected, (long Version, string[] Log, string[] Bodies) actual)
    {
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.Log, actual.Log);
        Assert.Equal(expected.Bodies, actual.Bodies);
    }

    private static Task<(long Version, string[] Log, string[] Bodies)> State(DocumentSession session)
        => session.Read((document, _) => (
            document.Version,
            document.Log.Select(entry => $"{entry.Command} {entry.Backend}").ToArray(),
            document.Bodies.Select(body => $"{body.Handle.Id} {body.Kind} {body.Name} {body.Visible}").OrderBy(text => text, StringComparer.Ordinal).ToArray()));

    // One sample command for each handler of the catalog, with values that test the
    // encoding: a negative zero, a subnormal number, text outside ASCII, and an
    // expected version.
    private static readonly Dictionary<string, Command> Samples = new(StringComparer.Ordinal)
    {
        ["NoOp"] = new NoOpCommand { Echo = "a \"quoted\" line\n𝄞", ExpectedDocumentVersion = 3 },
        ["CreateBox"] = new CreateBoxCommand { SizeX = 0.1, SizeY = double.MaxValue, SizeZ = double.Epsilon },
        ["Translate"] = new TranslateCommand { BodyId = Guid.NewGuid(), Dx = -0.0, Dy = -1e-300, Dz = 123456789.125 },
        ["Subtract"] = new SubtractCommand { MinuendBodyId = Guid.NewGuid(), SubtrahendBodyId = Guid.NewGuid() },
        ["RenameBody"] = new RenameBodyCommand { BodyId = Guid.NewGuid(), NewName = "" },
        ["SetBodyVisibility"] = new SetBodyVisibilityCommand { BodyId = Guid.NewGuid(), Visible = false },
        ["Undo"] = new UndoCommand { ExpectedDocumentVersion = 4 },
        ["Redo"] = new RedoCommand(),
        ["CreateSketch"] = new CreateSketchCommand { DatumId = Engine.Contracts.Sketch.Datums.XZ.Id, SketchName = "plate é 𝄞" },
        ["AddSketchPoint"] = new AddSketchPointCommand { SketchId = Guid.NewGuid(), X = -0.0, Y = 1e-310 },
        ["AddSketchLine"] = new AddSketchLineCommand { SketchId = Guid.NewGuid(), StartPointId = Guid.NewGuid(), EndPointId = Guid.NewGuid() },
        ["AddSketchCircle"] = new AddSketchCircleCommand { SketchId = Guid.NewGuid(), CenterPointId = Guid.NewGuid(), Radius = 2.5 },
        ["AddSketchArc"] = new AddSketchArcCommand { SketchId = Guid.NewGuid(), CenterPointId = Guid.NewGuid(), StartPointId = Guid.NewGuid(), EndPointId = Guid.NewGuid() },
        ["AddSketchConstraint"] = new AddSketchConstraintCommand { SketchId = Guid.NewGuid(), Kind = "fixed", First = Guid.NewGuid(), Value = 0.1, SecondValue = -0.0 },
        ["SetSketchConstraintValue"] = new SetSketchConstraintValueCommand { SketchId = Guid.NewGuid(), ConstraintId = Guid.NewGuid(), Value = double.MaxValue },
        ["MoveSketchPoint"] = new MoveSketchPointCommand { SketchId = Guid.NewGuid(), PointId = Guid.NewGuid(), X = 123456789.125, Y = -1e-300 },
        ["DeleteSketchItem"] = new DeleteSketchItemCommand { SketchId = Guid.NewGuid(), ItemId = Guid.NewGuid() },
        ["Extrude"] = new ExtrudeCommand { SketchId = Guid.NewGuid(), Distance = -2.5 },
        ["PlaceImage"] = new PlaceImageCommand
        {
            Asset = new string('a', 64), DatumId = Engine.Contracts.Sketch.Datums.XZ.Id, Width = 640, Height = 480, OriginU = -1.5, OriginV = 2.25, Scale = 0.125,
        },
        ["CalibrateImage"] = new CalibrateImageCommand { ImageId = Guid.NewGuid(), X1 = 1, Y1 = 2.5, X2 = -3, Y2 = 4, Distance = 12.75 },
    };

    // ADR-0015 item 11: each handler has an encoder, and Create(Encode(c)) gives c.
    [Fact]
    public void Each_Handler_Of_The_Catalog_Reads_Back_Its_Own_Command()
    {
        var registry = new CommandRegistry();
        foreach (var handler in HandlerCatalog.CommandHandlers())
            registry.Register(handler);

        foreach (var handler in HandlerCatalog.CommandHandlers())
        {
            Assert.True(handler is ICommandEncoder, $"The handler of {handler.CommandName} has no encoder (ADR-0015 item 11).");
            Assert.True(Samples.TryGetValue(handler.CommandName, out var sample), $"The test has no sample command of {handler.CommandName}.");

            var line = EntryCodec.Entry(7, new BackendIdentity("managed", "1"), sample, ((ICommandEncoder)handler).Encode(sample), out var refusal);
            Assert.True(line is not null, refusal);
            var entry = EntryCodec.ReadEntry(line, registry);

            Assert.Equal(7, entry.Version);
            Assert.Equal(new BackendIdentity("managed", "1"), entry.Backend);
            Assert.Equal(sample, entry.Command);
        }
    }

    // ADR-0015 item 12: the bits of each edge value survive a write and a read.
    [Theory]
    [InlineData(-0.0)]
    [InlineData(double.Epsilon)]
    [InlineData(0.1)]
    [InlineData(double.MaxValue)]
    [InlineData(-2.2250738585072014E-308)]
    public void A_Number_Keeps_Its_Bits_Through_A_Line(double value)
    {
        var registry = new CommandRegistry();
        var handler = new TranslateCommandHandler();
        registry.Register(handler);
        var command = new TranslateCommand { BodyId = Guid.NewGuid(), Dx = value, Dy = value, Dz = value };

        var line = EntryCodec.Entry(1, new BackendIdentity("managed", "1"), command, handler.Encode(command), out _)!;
        var read = (TranslateCommand)EntryCodec.ReadEntry(line, registry).Command;

        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(read.Dx));
    }

    // Each command of the catalog, on the native backend, in a real file.
    [NativeManifoldFact]
    public async Task A_File_That_A_Session_Wrote_Opens_To_The_Same_Document()
    {
        var path = FileNamed("scene");
        using var firstBackend = new ManifoldGeometryBackend();
        using var secondBackend = new ManifoldGeometryBackend();
        var first = NewSession(firstBackend);
        DocumentFile.Create(first, path);
        await ApplyScene(first);
        var expected = await State(first);
        first.Close();

        var second = NewSession(secondBackend);
        var warnings = await DocumentFile.Open(second, path);

        Assert.Empty(warnings);
        AssertSameState(expected, await State(second));
        Assert.NotEqual(first.DocumentId, second.DocumentId);
        Assert.Equal(path, second.FilePath);
        second.Close();
        Assert.Equal(7, File.ReadAllLines(path).Length);
    }

    [Fact]
    public async Task A_Session_That_Opened_A_File_Writes_Its_Later_Commands_To_It()
    {
        var path = FileNamed("later");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        first.Close();

        var second = NewSession();
        await DocumentFile.Open(second, path);
        await second.Apply(new NoOpCommand { Echo = "two" });
        var expected = await State(second);
        second.Close();

        var third = NewSession();
        await DocumentFile.Open(third, path);
        Assert.Equal(expected.Log, (await State(third)).Log);
        Assert.Equal(2, (await State(third)).Version);
        third.Close();
    }

    // ADR-0031 item 3: an open rebuilds the history from the entries, so an undo and a
    // redo continue after a stop. Each command of the catalog, on the native backend.
    [NativeManifoldFact]
    public async Task An_Undo_And_A_Redo_Continue_After_An_Open_Of_The_File()
    {
        var path = FileNamed("history");
        using var firstBackend = new ManifoldGeometryBackend();
        using var secondBackend = new ManifoldGeometryBackend();
        var first = NewSession(firstBackend);
        DocumentFile.Create(first, path);
        await ApplyScene(first);
        await first.Apply(new UndoCommand());
        var afterFourSteps = await State(first);
        await first.Apply(new UndoCommand());
        await first.Apply(new UndoCommand());
        await first.Apply(new RedoCommand());
        var expected = await State(first);
        first.Close();

        var second = NewSession(secondBackend);
        await DocumentFile.Open(second, path);
        AssertSameState(expected, await State(second));

        var redo = await second.Apply(new RedoCommand());
        Assert.Equal(CommandStatus.Applied, redo.Status);
        Assert.Equal(afterFourSteps.Bodies, (await State(second)).Bodies);
        var undo = await second.Apply(new UndoCommand());
        Assert.Equal(CommandStatus.Applied, undo.Status);
        second.Close();
        Assert.Equal(1 + 6 + 4 + 2, File.ReadAllLines(path).Length);
    }

    // ADR-0015 item 4: a failed write rejects the command, the Document does not
    // change, and the session refuses each later command before its handler runs.
    [Fact]
    public async Task A_Failed_Write_Rejects_The_Command_And_Each_Later_Command()
    {
        var session = NewSession();
        var stream = new FailingStream();
        session.Attach(DocumentJournal.ForTest(FileNamed("failing"), Document.Millimetre, stream));

        var first = await session.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        // A stale version shows that the refusal comes before each other check.
        var second = await session.Apply(new NoOpCommand { Echo = "after", ExpectedDocumentVersion = 99 });

        Assert.Equal(CommandStatus.Rejected, first.Status);
        Assert.Equal(DiagnosticCodes.IoWriteFailed, first.Error!.Code);
        Assert.Equal(CommandStatus.Rejected, second.Status);
        Assert.Equal(DiagnosticCodes.IoWriteFailed, second.Error!.Code);
        var state = await State(session);
        Assert.Equal(0, state.Version);
        Assert.Empty(state.Bodies);
        Assert.Equal(1, stream.Writes);
    }

    [Fact]
    public async Task A_Command_That_The_File_Cannot_Hold_Is_Rejected_Before_The_Write()
    {
        var path = FileNamed("text");
        var session = NewSession();
        DocumentFile.Create(session, path);

        var result = await session.Apply(new NoOpCommand { Echo = "a\uD800b" });
        var infinite = await session.Apply(new CreateBoxCommand { SizeX = double.PositiveInfinity, SizeY = 1, SizeZ = 1 });
        session.Close();

        Assert.Equal(DiagnosticCodes.IoNotEncodable, result.Error!.Code);
        Assert.Equal(0, result.DocumentVersion);
        Assert.Equal(DiagnosticCodes.IoNotEncodable, infinite.Error!.Code);
        Assert.Single(File.ReadAllLines(path));
    }

    // ADR-0015 item 8: a torn last line is cut off with a warning, and the file then
    // takes the next entry at the right place. A torn line is a tail with no line feed, or
    // a complete last line that is not JSON.
    [Theory]
    [InlineData("{\"version\":2,\"backend\"")]
    [InlineData("{\"version\":2,\"backend\"\n")]
    public async Task A_Torn_Last_Line_Is_Cut_Off_With_A_Warning(string tail)
    {
        var path = FileNamed("torn");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        first.Close();
        var length = new FileInfo(path).Length;
        File.AppendAllText(path, tail);

        var second = NewSession();
        var warnings = await DocumentFile.Open(second, path);
        await second.Apply(new NoOpCommand { Echo = "two" });
        second.Close();

        Assert.Equal(DiagnosticCodes.IoTruncated, Assert.Single(warnings).Code);
        var third = NewSession();
        await DocumentFile.Open(third, path);
        Assert.Equal(2, (await State(third)).Version);
        third.Close();
        Assert.True(new FileInfo(path).Length > length);
    }

    // E76 of the second codebase review of 2026-10-28: a complete last line that is JSON and
    // does not bind refuses the open, and the file keeps it. The open cut it, so a file of a
    // newer engine lost its last command when an older engine opened it.
    [Theory]
    [InlineData("\"name\":\"NoOp\"", "\"name\":\"Fillet\"")]
    [InlineData("\"schemaVersion\":1", "\"schemaVersion\":99")]
    [InlineData("\"echo\":\"two\"", "\"echo\":2")]
    [InlineData("\"backend\":", "\"engine\":")]
    public async Task A_Complete_Last_Line_That_Does_Not_Bind_Refuses_The_Open_And_Stays(string from, string to)
    {
        var path = FileNamed("newer");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        await first.Apply(new NoOpCommand { Echo = "two" });
        first.Close();
        var lines = File.ReadAllText(path).Split('\n');
        var last = lines.Length - 2;
        Assert.Contains(from, lines[last], StringComparison.Ordinal);
        lines[last] = lines[last].Replace(from, to, StringComparison.Ordinal);
        File.WriteAllText(path, string.Join("\n", lines));
        var bytes = File.ReadAllBytes(path);

        var second = NewSession();
        var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(second, path));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }

    [Fact]
    public async Task A_Bad_Line_Before_The_Last_Line_Refuses_The_Open()
    {
        var path = FileNamed("bad");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        await first.Apply(new NoOpCommand { Echo = "two" });
        first.Close();
        var lines = File.ReadAllLines(path);
        lines[1] = "{not json";
        File.WriteAllText(path, string.Join("\n", lines) + "\n");

        var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(NewSession(), path));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
    }

    [Theory]
    [InlineData("\"version\":1,", "\"version\":2,")]
    [InlineData("\"name\":\"NoOp\"", "\"name\":\"NoSuchCommand\"")]
    [InlineData("\"echo\":\"one\"", "\"echo\":null")]
    public async Task An_Entry_That_Does_Not_Agree_Refuses_The_Open(string from, string to)
    {
        var path = FileNamed("disagree");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        await first.Apply(new NoOpCommand { Echo = "two" });
        first.Close();
        var text = File.ReadAllText(path);
        Assert.Contains(from, text, StringComparison.Ordinal);
        var index = text.IndexOf(from, StringComparison.Ordinal);
        File.WriteAllText(path, text[..index] + to + text[(index + from.Length)..]);

        var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(NewSession(), path));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
    }

    // ADR-0015 item 6: a replay uses the recorded backend.
    [Fact]
    public async Task An_Entry_Of_Another_Backend_Refuses_The_Open()
    {
        var path = FileNamed("backend");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        first.Close();

        var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(NewSession(NullGeometryBackend.Instance), path));

        Assert.Equal(DiagnosticCodes.IoBackendMismatch, error.Code);
    }

    [Fact]
    public async Task Another_Version_Of_The_Backend_Gives_One_Warning_And_The_Open_Continues()
    {
        var path = FileNamed("version");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new NoOpCommand { Echo = "one" });
        await first.Apply(new NoOpCommand { Echo = "two" });
        first.Close();
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"version\":\"1\"", "\"version\":\"0\"", StringComparison.Ordinal));

        var second = NewSession();
        var warnings = await DocumentFile.Open(second, path);

        Assert.Equal(DiagnosticCodes.IoBackendVersion, Assert.Single(warnings).Code);
        Assert.Equal(2, (await State(second)).Version);
        second.Close();
    }

    [Theory]
    [InlineData("\"lengthUnit\":\"mm\"", "\"lengthUnit\":\"m\"")]
    [InlineData("\"formatVersion\":1", "\"formatVersion\":2")]
    [InlineData("3dengine-document", "another-document")]
    public void A_Header_That_This_Engine_Does_Not_Read_Refuses_The_Open(string from, string to)
    {
        var path = FileNamed("header");
        File.WriteAllText(path, EntryCodec.Header(Document.Millimetre).Replace(from, to, StringComparison.Ordinal) + "\n");

        var error = Assert.Throws<DocumentFileException>(() => DocumentFile.ReadLengthUnit(path));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
    }

    // ADR-0015 item 9: a document with no name has no file before its first command,
    // and a save moves the file to the name; later entries go there.
    [Fact]
    public async Task A_Document_With_No_Name_Writes_From_Its_First_Command_And_A_Save_Moves_It()
    {
        var unsaved = Path.Combine(_folder, "unsaved");
        var session = NewSession();
        DocumentFile.Unsaved(session, unsaved);
        var unsavedPath = session.FilePath!;
        Assert.False(File.Exists(unsavedPath));

        await session.Apply(new NoOpCommand { Echo = "one" });
        Assert.True(File.Exists(Path.Combine(unsaved, session.DocumentId.ToString("D") + DocumentFile.Extension)));

        var target = FileNamed("saved");
        Assert.Null(await session.SaveAs(target));
        await session.Apply(new NoOpCommand { Echo = "two" });
        var expected = await State(session);
        session.Close();

        Assert.False(File.Exists(unsavedPath));
        var reopened = NewSession();
        await DocumentFile.Open(reopened, target);
        AssertSameState(expected, await State(reopened));
        reopened.Close();
    }

    [Fact]
    public async Task A_Save_To_A_File_That_Exists_Fails_And_The_File_Stays()
    {
        var path = FileNamed("first");
        var other = FileNamed("other");
        File.WriteAllText(other, "keep");
        var session = NewSession();
        DocumentFile.Create(session, path);
        await session.Apply(new NoOpCommand { Echo = "one" });

        var error = await session.SaveAs(other);
        await session.Apply(new NoOpCommand { Echo = "two" });
        session.Close();

        Assert.Equal(DiagnosticCodes.IoSaveFailed, error!.Code);
        Assert.Equal("keep", File.ReadAllText(other));
        Assert.Equal(3, File.ReadAllLines(path).Length);
    }

    // Finding E75 of the second codebase review of 2026-10-28: the live bus applies a
    // CommandId again after the cache forgot it (E17), so the file holds it two times.
    // The open looked in the cache, skipped the second entry with no error, and gave
    // another Document; the next command then made the file impossible to open.
    [Fact]
    public async Task An_Open_Applies_An_Entry_Whose_CommandId_Came_Before()
    {
        var path = FileNamed("again");
        var live = NewSession();
        DocumentFile.Create(live, path);
        var box = await live.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        var body = (Guid)box.Outputs.Values["bodyId"]!;
        var hide = new SetBodyVisibilityCommand { BodyId = body, Visible = false };
        await live.Apply(hide);
        await live.Apply(new SetBodyVisibilityCommand { BodyId = body, Visible = true });
        for (var index = 0; index < IdempotencyCache.DefaultCapacity; index++)
            await live.Apply(new RedoCommand());
        var again = await live.Apply(hide with { });
        var expected = await State(live);
        live.Close();

        var opened = NewSession();
        await DocumentFile.Open(opened, path);
        var actual = await State(opened);
        var next = await opened.Apply(new NoOpCommand { Echo = "next" });
        opened.Close();
        var third = NewSession();
        await DocumentFile.Open(third, path);
        var reopened = await State(third);
        third.Close();

        Assert.Equal((CommandStatus.Applied, 4L), (again.Status, again.DocumentVersion));
        Assert.Contains(expected.Bodies, text => text.StartsWith(body.ToString(), StringComparison.Ordinal) && text.EndsWith(" False", StringComparison.Ordinal));
        AssertSameState(expected, actual);
        Assert.Equal(5, next.DocumentVersion);
        Assert.Equal(5, reopened.Version);
    }

    // Finding E100 of the codebase review of 2026-10-30 (b) (TASK-0123, answer Q1 A): up to v0.117
    // a move that a client sent again after the cache forgot it entered the log two times, and the
    // guard of E95 refused the second entry at the open, so the file did not open. The file here is
    // the file of v0.117: the line of the move again, with the next version. A move sent again to a
    // live session is still refused (SketchGapTests).
    [Fact]
    public async Task An_Open_Applies_A_Move_That_A_Client_Sent_Again()
    {
        var path = FileNamed("move-again");
        var live = NewSession();
        DocumentFile.Create(live, path);
        var sketch = (Guid)(await live.Apply(new CreateSketchCommand { DatumId = Engine.Contracts.Sketch.Datums.XY.Id })).Outputs.Values["sketchId"]!;
        var point = (Guid)(await live.Apply(new AddSketchPointCommand { SketchId = sketch, X = 1, Y = 1 })).Outputs.Values["pointId"]!;
        await live.Apply(new MoveSketchPointCommand { SketchId = sketch, PointId = point, X = 2, Y = 2 });
        live.Close();
        var lines = File.ReadAllLines(path).ToList();
        var again = System.Text.Json.Nodes.JsonNode.Parse(lines[^1])!.AsObject();
        again["version"] = lines.Count;
        lines.Add(again.ToJsonString());
        File.WriteAllLines(path, lines);

        var opened = NewSession();
        var warnings = await DocumentFile.Open(opened, path);
        var (version, moved) = await opened.Read((document, _) => (document.Version, document.Sketches.Single().Points.Single(p => p.Id == point)));
        opened.Close();

        Assert.Empty(warnings);
        Assert.Equal(4, version);
        Assert.Equal((2.0, 2.0), (moved.X, moved.Y));
    }

    // ADR-0015 item 8: the versions of the entries count from 1, so an open needs a new
    // session. An open into a session with a command wrote a file that did not open
    // again (E75).
    [Fact]
    public async Task An_Open_Refuses_A_Session_That_Has_A_Command()
    {
        var path = FileNamed("source");
        var writer = NewSession();
        DocumentFile.Create(writer, path);
        await writer.Apply(new NoOpCommand { Echo = "one" });
        writer.Close();
        var session = NewSession();
        await session.Apply(new NoOpCommand { Echo = "before" });

        var refusal = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(session, path));
        var version = await session.Read((document, _) => document.Version);
        session.Close();

        Assert.Equal(DiagnosticCodes.IoLoadFailed, refusal.Code);
        Assert.Equal(1, version);
        Assert.Equal(2, File.ReadAllLines(path).Length);
    }

    // E78 of the second codebase review of 2026-10-28: the writer refuses a number that
    // is not finite, so the reader refuses it too. 1e400 read as an infinity.
    [Fact]
    public async Task A_Number_That_Is_Not_Finite_Refuses_The_Open()
    {
        var path = FileNamed("infinite");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new CreateBoxCommand { SizeX = 2, SizeY = 3, SizeZ = 4 });
        await first.Apply(new NoOpCommand { Echo = "after" });
        first.Close();
        var text = File.ReadAllText(path);
        Assert.Contains("\"sizeX\":2", text, StringComparison.Ordinal);
        File.WriteAllText(path, text.Replace("\"sizeX\":2", "\"sizeX\":1e400", StringComparison.Ordinal));

        var second = NewSession();
        var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(second, path));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
        Assert.Equal(0, (await State(second)).Version);
    }

    // E82: each failure of the file and of an open gives a DocumentFileException with its
    // code. A path of a folder gave an UnauthorizedAccessException or a plain IOException.
    [Fact]
    public void A_File_That_Cannot_Be_Made_Gives_The_Code_Of_An_Open()
    {
        var error = Assert.Throws<DocumentFileException>(() => DocumentFile.Create(NewSession(), _folder));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
    }

    // E82, and E28 of the review of 2026-10-08 for an open: an exception of a handler left
    // the open with no code.
    [Fact]
    public async Task An_Exception_Of_A_Handler_During_An_Open_Gives_The_Code_Of_An_Open()
    {
        var path = FileNamed("throws");
        var first = NewSession();
        DocumentFile.Create(first, path);
        await first.Apply(new CreateBoxCommand { SizeX = 1, SizeY = 1, SizeZ = 1 });
        first.Close();

        var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(NewSession(new ThrowingBackend()), path));

        Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
        Assert.Contains(nameof(InvalidCastException), error.Message, StringComparison.Ordinal);
    }

    // E83: an open holds the file before the entries apply. A file that another session
    // holds applied each entry, and then failed with no code. On Linux and macOS .NET takes
    // no lock for a write with the share of a read, so only Windows refuses the hold
    // (ADR-0015 item 13: the lock is advisory there).
    [Fact]
    public async Task An_Open_Of_A_File_That_Another_Session_Holds_Applies_No_Entry()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var path = FileNamed("held");
        var holder = NewSession();
        DocumentFile.Create(holder, path);
        await holder.Apply(new NoOpCommand { Echo = "one" });
        try
        {
            var second = NewSession();
            var error = await Assert.ThrowsAsync<DocumentFileException>(() => DocumentFile.Open(second, path));

            Assert.Equal(DiagnosticCodes.IoLoadFailed, error.Code);
            Assert.Equal(0, (await State(second)).Version);
        }
        finally
        {
            holder.Close();
        }
    }

    // E84: a failure of the first open of a document with no name breaks the journal, as a
    // failed write does. The journal stayed whole and wrote again at the next command.
    [Fact]
    public async Task A_Failed_First_Open_Breaks_The_Journal()
    {
        var blocker = Path.Combine(_folder, "unsaved");
        File.WriteAllText(blocker, "a file where the folder must be");
        var session = NewSession();
        DocumentFile.Unsaved(session, blocker);

        var first = await session.Apply(new NoOpCommand { Echo = "one" });
        File.Delete(blocker);
        var second = await session.Apply(new NoOpCommand { Echo = "two" });
        session.Close();

        Assert.Equal(DiagnosticCodes.IoWriteFailed, first.Error!.Code);
        Assert.Equal(DiagnosticCodes.IoWriteFailed, second.Error!.Code);
        Assert.False(Directory.Exists(blocker));
    }

    // E84: when the file does not open again after a move, the journal is broken. It had no
    // stream and was whole, so a later save made a file with a header only.
    [Fact]
    public async Task A_Failed_Open_After_A_Move_Breaks_The_Journal()
    {
        var path = FileNamed("before");
        File.WriteAllText(path, EntryCodec.Header(Document.Millimetre) + "\n");
        var opens = 0;
        Stream FailOnce(string file, FileMode mode)
            => opens++ == 0
                ? throw new IOException("The disk went away.")
                : new FileStream(file, mode, FileAccess.ReadWrite, FileShare.Read);
        var session = NewSession();
        session.Attach(DocumentJournal.ForTest(path, Document.Millimetre, new MemoryStream(), FailOnce));

        var move = await session.SaveAs(FileNamed("after"));
        var save = await session.SaveAs(FileNamed("third"));
        var command = await session.Apply(new NoOpCommand { Echo = "one" });
        session.Close();

        Assert.Equal(DiagnosticCodes.IoSaveFailed, move!.Code);
        Assert.Equal(DiagnosticCodes.IoWriteFailed, command.Error!.Code);
        Assert.Equal(DiagnosticCodes.IoSaveFailed, save!.Code);
        Assert.False(File.Exists(FileNamed("third")));
    }

    // E84: when the flush and the cut both fail, the file can keep the complete line of a
    // command that its client saw rejected. The rejection says so.
    [Fact]
    public async Task A_Failed_Cut_Says_That_The_File_Can_Keep_The_Line()
    {
        var session = NewSession();
        session.Attach(DocumentJournal.ForTest(FileNamed("cut"), Document.Millimetre, new CutFailingStream()));

        var result = await session.Apply(new NoOpCommand { Echo = "one" });
        session.Close();

        Assert.Equal(DiagnosticCodes.IoWriteFailed, result.Error!.Code);
        Assert.Contains("can keep the line of this command", result.Error.Message, StringComparison.Ordinal);
    }

    // A stream that takes each write and fails each flush and each cut.
    private sealed class CutFailingStream : MemoryStream
    {
        public override void Flush() => throw new IOException("The flush failed.");

        public override void SetLength(long value) => throw new IOException("The cut failed.");
    }

    // A backend whose look-up of an operation throws, as a defect of a handler does. Its
    // identity is the identity of the managed backend, so an open reaches the handler.
    private sealed class ThrowingBackend : IGeometryBackend
    {
        public BackendIdentity Identity { get; } = new InProcessMeshBackend().Identity;

        public BackendCapabilities Capabilities => BackendCapabilities.Mesh;

        public T? TryGet<T>() where T : class => throw new InvalidCastException("The backend is broken.");
    }

    // A stream that fails each write, for a write that fails.
    private sealed class FailingStream : MemoryStream
    {
        public int Writes { get; private set; }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Writes++;
            throw new IOException("The disk is full.");
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
    }
}
