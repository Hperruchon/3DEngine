using Engine.Api.Http.Endpoints;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Drawing;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;
using Engine.Core.Queries;

namespace Engine.Tests.Drawing;

// ADR-0179 (phase R30i, TASK-0428): the title block of a sheet. ADR-0179 names this file in enforced-by. The field of the record,
// the command SetSheetTitle and its refusals, the events, undo and redo, a document file before R30i and the replay of a file
// with the block, and the layout of the frame, the centring marks, the block and the symbol of the projection.
public sealed class SheetTitleTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sheet-title-" + Guid.NewGuid().ToString("N"));

    public SheetTitleTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private static (DocumentSession Session, InMemoryEventSink Events) NewSession()
    {
        var kit = EngineHosting.CreateDefault(new InProcessMeshBackend());
        return (new DocumentSession(kit), kit.Events);
    }

    private static async Task<Guid> Applied(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>("sheetId", out var id) ? id : Guid.Empty;
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static Task<SheetRecord> Sheet(DocumentSession session)
        => session.Read((document, _) => document.Sheets.Single());

    private static async Task<SheetInfo> Read(DocumentSession session, Guid sheet)
        => (await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet })).Result!;

    private static SetSheetTitleCommand Full(Guid sheet) => new()
    {
        SheetId = sheet,
        Title = "Bracket",
        IdentificationNumber = "DWG-0042",
        Revision = "B",
        IssueDate = "2026-11-21",
        SheetNumber = "1/1",
        LegalOwner = "Owner é",
        Material = "AlMg3",
        Creator = "H. P.",
        GeneralTolerances = "ISO 2768-mK",
        ApprovalPerson = "A. B.",
        DocumentType = "Detail drawing",
    };

    private static readonly SheetTitleBlock FullBlock = new("Bracket", "DWG-0042", "B", "2026-11-21", "1/1", "Owner é", "AlMg3", "H. P.", "ISO 2768-mK", "A. B.", "Detail drawing");

    // Items 1 to 3 (Q5 A): a new sheet has no block, and its read gives each field empty. SetSheetTitle gives the whole block in
    // a new record of the sheet and sheet.changed with the block; a second SetSheetTitle with one field gives the other fields
    // empty. Each command is one step of undo, and a redo gives the record after.
    [Fact]
    public async Task SetSheetTitle_Gives_The_Whole_Block_In_One_Step_Of_Undo()
    {
        var (session, events) = NewSession();
        var id = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" });
        var created = (IReadOnlyDictionary<string, object?>)events.Snapshot().Single(record => record.Kind == "sheet.created").Payload!;
        Assert.True(created.ContainsKey("titleBlock"));
        Assert.Null(created["titleBlock"]);
        Assert.Null((await Sheet(session)).TitleBlock);
        Assert.Equal(SheetTitleBlock.Empty, (await Read(session, id)).TitleBlock);

        var before = events.Snapshot().Count;
        Assert.Equal(id, await Applied(session, Full(id)));
        Assert.Equal(["command.applied", "sheet.changed"], events.Snapshot().Skip(before).Select(record => record.Kind));
        var changed = (IReadOnlyDictionary<string, object?>)events.Snapshot()[^1].Payload!;
        Assert.Equal(FullBlock, changed["titleBlock"]);
        foreach (var (kind, payload) in new[] { ("sheet.created", created), ("sheet.changed", changed) })
        {
            var entry = SchemaEventsEndpoint.Kinds.Single(entry => entry.Kind == kind).Payload;
            Assert.Equal(entry.Keys.Order(StringComparer.Ordinal), payload.Keys.Order(StringComparer.Ordinal));
            Assert.Equal(("object", false), (entry["titleBlock"].Type, entry["titleBlock"].Required));
        }
        Assert.Equal(FullBlock, (await Sheet(session)).TitleBlock);
        Assert.Equal(FullBlock, (await Read(session, id)).TitleBlock);
        Assert.Equal(("Sheet 1", "A4"), ((await Sheet(session)).Name, (await Sheet(session)).Paper));

        await Applied(session, new SetSheetTitleCommand { SheetId = id, Revision = "C" });
        Assert.Equal(SheetTitleBlock.Empty with { Revision = "C" }, (await Sheet(session)).TitleBlock);

        await Applied(session, new UndoCommand());
        Assert.Equal(FullBlock, (await Sheet(session)).TitleBlock);
        await Applied(session, new UndoCommand());
        Assert.Null((await Sheet(session)).TitleBlock);
        await Applied(session, new RedoCommand());
        Assert.Equal(FullBlock, (await Sheet(session)).TitleBlock);
    }

    // Item 2: a sheet that is not live gives E-SHEET-NOT-FOUND. Each field follows the rules of a name: a control character, a
    // lone surrogate or more than 128 UTF-16 units gives E-SHEET-TITLE-INVALID, and the sheet keeps its block. 128 units pass.
    [Fact]
    public async Task Each_Field_Follows_The_Rules_Of_A_Name()
    {
        var (session, _) = NewSession();
        var id = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" });
        await Applied(session, Full(id));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, Full(Guid.NewGuid())));

        var handler = new SetSheetTitleCommandHandler();
        foreach (var field in SetSheetTitleCommandHandler.Fields)
        {
            foreach (var wrong in new[] { "a\nb", "\ud800", new string('x', 129) })
            {
                var input = new CommandInput(new Dictionary<string, object?> { ["sheetId"] = id, [field] = wrong }, Guid.NewGuid(), null);
                Assert.Equal(DiagnosticCodes.SheetTitleInvalid, await Refused(session, handler.Create(input)));
            }
        }

        Assert.Equal(FullBlock, (await Sheet(session)).TitleBlock);
        await Applied(session, new SetSheetTitleCommand { SheetId = id, Title = new string('x', 128) });
        Assert.Equal(128, (await Sheet(session)).TitleBlock!.Title.Length);
    }

    // Items 1 and 7: a document file that a session wrote before R30i, with no SetSheetTitle, opens with the same sheet and no
    // block, and its read gives each field empty. A SetSheetTitle after the open goes into the file, and the next open replays it
    // to the same block.
    [Fact]
    public async Task A_File_Before_R30i_Opens_With_No_Block_And_A_Block_Replays()
    {
        var path = Path.Combine(_folder, "old" + DocumentFile.Extension);
        File.WriteAllText(path,
            "{\"format\":\"3dengine-document\",\"formatVersion\":1,\"lengthUnit\":\"mm\"}\n"
            + "{\"version\":1,\"backend\":{\"name\":\"managed\",\"version\":\"1\"},\"command\":{\"name\":\"CreateSheet\",\"schemaVersion\":1,\"commandId\":\"5e000000-0000-0000-0000-000000000001\",\"expectedDocumentVersion\":null,\"parameters\":{\"landscape\":true,\"name\":\"Bracket\",\"paper\":\"A3\",\"thirdAngle\":false}}}\n");

        var (opened, _) = NewSession();
        Assert.Empty(await DocumentFile.Open(opened, path));
        var sheet = await Sheet(opened);
        Assert.Equal((Guid.Parse("5e000000-0000-0000-0000-000000000001"), "Bracket", "A3"), (sheet.Id, sheet.Name, sheet.Paper));
        Assert.Null(sheet.TitleBlock);
        Assert.Equal(SheetTitleBlock.Empty, (await Read(opened, sheet.Id)).TitleBlock);
        await Applied(opened, Full(sheet.Id));
        opened.Close();

        var (again, _) = NewSession();
        await DocumentFile.Open(again, path);
        Assert.Equal(FullBlock, (await Sheet(again)).TitleBlock);
        again.Close();
    }

    private static SheetInfo Paper(string paper, bool landscape, bool thirdAngle = false, SheetTitleBlock? block = null)
    {
        var (width, height) = paper switch
        {
            "A4" => landscape ? (297.0, 210.0) : (210.0, 297.0),
            _ => landscape ? (420.0, 297.0) : (297.0, 420.0),
        };
        return new SheetInfo(Guid.NewGuid(), "Sheet 1", paper, landscape, thirdAngle, width, height, [], []) { TitleBlock = block ?? SheetTitleBlock.Empty };
    }

    private static string Text(SheetLine line)
        => FormattableString.Invariant($"{line.Kind} {Math.Round(line.FromX, 9)},{Math.Round(line.FromY, 9)} {Math.Round(line.ToX, 9)},{Math.Round(line.ToY, 9)}{(line.Circle is { } c ? FormattableString.Invariant($" r{c.Radius}") : "")}");

    // Item 4 (Q4 A, Q6 A): the block is 180 mm wide and 44 mm high in the lower right corner of the frame. Its left side and its
    // top are thick, its rows and cells thin, and each field has its label at the upper left of its cell and its value at the
    // lower left. The title is the name of the sheet when its field is empty.
    [Fact]
    public void Each_Field_Has_Its_Cell_In_A_Block_Of_180_Millimetres()
    {
        var layout = TitleBlock.Layout(Paper("A3", landscape: true, block: FullBlock));
        Assert.Equal((230.0, 10.0, 410.0, 54.0), TitleBlock.Box(420, 297));

        var lines = layout.Lines.Select(Text).ToList();
        Assert.Contains("frame 230,10 230,54", lines);
        Assert.Contains("frame 230,54 410,54", lines);
        Assert.Equal(["block 230,20 410,20", "block 230,32 410,32", "block 230,42 410,42"], lines.Where(line => line.StartsWith("block 230,", StringComparison.Ordinal)));
        Assert.Equal(11, lines.Count(line => line.StartsWith("block ", StringComparison.Ordinal) && !line.StartsWith("block 230,", StringComparison.Ordinal)));

        string[] values = ["Owner é", "DWG-0042", "B", "2026-11-21", "1/1", "Bracket", "Detail drawing", "H. P.", "A. B.", "AlMg3", "ISO 2768-mK", "as shown", "mm", "A3"];
        var expected = TitleBlock.Cells.Where(cell => cell.Label.Length > 0).Zip(values).ToList();
        foreach (var (cell, value) in expected)
        {
            var label = Assert.Single(layout.Texts, text => text.Text == cell.Label);
            Assert.Equal((230 + cell.X0 + 1.5, 10 + cell.Y1 - 1.5 - 1.8), (label.X, label.Y));
            var shown = Assert.Single(layout.Texts, text => text.Text == value);
            Assert.Equal((230 + cell.X0 + 1.5, 10 + cell.Y0 + 2), (shown.X, shown.Y));
        }

        Assert.Equal(2 * expected.Count, layout.Texts.Count);
        Assert.Equal(5, Assert.Single(layout.Texts, text => text.Text == "Bracket").Size);
        var unnamed = TitleBlock.Layout(Paper("A3", landscape: true));
        Assert.Contains(unnamed.Texts, text => text.Text == "Sheet 1" && text.Size == 5);
        Assert.Equal(18, unnamed.Texts.Count);
    }

    // Item 4: a text wider than its cell gets a smaller size at which it fills the room of the cell; a short text keeps its size.
    [Fact]
    public void A_Long_Text_Gets_A_Smaller_Size_In_Its_Cell()
    {
        var title = new string('W', 60);
        var layout = TitleBlock.Layout(Paper("A3", landscape: true, block: SheetTitleBlock.Empty with { Title = title, Revision = "B" }));

        var shown = Assert.Single(layout.Texts, text => text.Text == title);
        Assert.True(shown.Size < 2, $"The size of a title of 60 W is {shown.Size}.");
        Assert.Equal(125 - 3, TitleBlock.Measure(title, shown.Size), 9);
        Assert.Equal(3.5, Assert.Single(layout.Texts, text => text.Text == "B").Size);
        foreach (var text in layout.Texts)
        {
            var cell = TitleBlock.Cells.Single(cell => 230 + cell.X0 + 1.5 == text.X && text.Y > 10 + cell.Y0 && text.Y < 10 + cell.Y1);
            Assert.True(TitleBlock.Measure(text.Text, text.Size) <= cell.X1 - cell.X0 - 3 + 1e-9, $"{text.Text} is wider than its cell.");
        }
    }

    // Item 6: the symbol of the projection at the middle of its cell, (392.5, 48) on A3 landscape: a trapezium whose small end is
    // on the left, two circles of the diameters 7 and 3.5, the axis and the cross of the circles. In first angle the circles are
    // on the right of the trapezium; in third angle on its left.
    [Theory]
    [InlineData(false, 383.75, 397.75)]
    [InlineData(true, 394.25, 387.25)]
    public void The_Symbol_Shows_The_Projection_Of_The_Sheet(bool thirdAngle, double cone, double circle)
    {
        var lines = TitleBlock.Layout(Paper("A3", landscape: true, thirdAngle)).Lines.Select(Text).ToList();

        string P(double x, double y) => FormattableString.Invariant($"{x},{y}");
        Assert.Equal(
        [
            $"symbol {P(cone, 46.25)} {P(cone, 49.75)}",
            $"symbol {P(cone + 7, 44.5)} {P(cone + 7, 51.5)}",
            $"symbol {P(cone, 49.75)} {P(cone + 7, 51.5)}",
            $"symbol {P(cone, 46.25)} {P(cone + 7, 44.5)}",
            $"symbol {P(circle + 3.5, 48)} {P(circle + 3.5, 48)} r3.5",
            $"symbol {P(circle + 1.75, 48)} {P(circle + 1.75, 48)} r1.75",
            $"centre {P(382.25, 48)} {P(402.75, 48)}",
            $"centre {P(circle, 43)} {P(circle, 53)}",
        ], lines.Where(line => line.StartsWith("symbol ", StringComparison.Ordinal) || line.StartsWith("centre ", StringComparison.Ordinal)));
        Assert.Equal(thirdAngle, circle < cone);
    }

    // Item 5: the frame and a centring mark at the middle of each side, from the edge of the paper to 5 mm past the frame. On A4
    // portrait the block holds the middle of the lower side, so that mark stops at the frame.
    [Fact]
    public void The_Centring_Marks_Go_From_The_Edge_To_Five_Millimetres_Past_The_Frame()
    {
        static IReadOnlyList<string> Frame(SheetInfo sheet) => [.. TitleBlock.Layout(sheet).Lines.Take(8).Select(Text)];

        Assert.Equal(
        [
            "frame 10,10 410,10", "frame 410,10 410,287", "frame 410,287 10,287", "frame 10,287 10,10",
            "frame 210,0 210,15", "frame 210,297 210,282", "frame 0,148.5 15,148.5", "frame 420,148.5 405,148.5",
        ], Frame(Paper("A3", landscape: true)));
        Assert.Equal(
        [
            "frame 10,10 200,10", "frame 200,10 200,287", "frame 200,287 10,287", "frame 10,287 10,10",
            "frame 105,0 105,10", "frame 105,297 105,282", "frame 0,148.5 15,148.5", "frame 210,148.5 195,148.5",
        ], Frame(Paper("A4", landscape: false)));
    }
}
