using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Persistence;

namespace Engine.Tests.Drawing;

// ADR-0153 (phase P10.1, TASK-0389): a drawing sheet in the Document. ADR-0153 names this file in enforced-by. The record, the
// three commands, each refusal, the events, undo and redo, and the document file.
public sealed class SheetTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "3de-sheet-" + Guid.NewGuid().ToString("N"));

    public SheetTests() => Directory.CreateDirectory(_folder);

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

    private static Task<IReadOnlyList<SheetRecord>> Sheets(DocumentSession session)
        => session.Read<IReadOnlyList<SheetRecord>>((document, _) => document.Sheets.ToList());

    // Items 1 to 3 and 6: CreateSheet gives a sheet with the identifier of its command, its name, its paper, its orientation and
    // its projection, with no view and no dimension, and the event sheet.created carries the record.
    [Fact]
    public async Task A_Created_Sheet_Has_Its_Record_And_Its_Event()
    {
        var (session, events) = NewSession();
        var command = new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A3", Landscape = false };

        var id = await Applied(session, command);

        Assert.Equal(command.CommandId, id);
        var sheet = Assert.Single(await Sheets(session));
        Assert.Equal((id, "Sheet 1", "A3", false, false), (sheet.Id, sheet.Name, sheet.Paper, sheet.Landscape, sheet.ThirdAngle));
        Assert.Empty(sheet.Views);
        Assert.Empty(sheet.Dimensions);
        var created = (IReadOnlyDictionary<string, object?>)events.Snapshot().Single(record => record.Kind == "sheet.created").Payload!;
        Assert.Equal((id, "Sheet 1", "A3"), ((Guid)created["sheetId"]!, (string)created["name"]!, (string)created["paper"]!));
    }

    // Items 1, 2 and 6: the default paper is A4 landscape with ISO first angle; a wrong paper, an empty name, a sheet that is
    // not live and the identifier of a command in the log are refused.
    [Fact]
    public async Task The_Default_Paper_Is_A4_And_Each_Wrong_Command_Gives_Its_Code()
    {
        var (session, _) = NewSession();
        var id = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" });

        var sheet = Assert.Single(await Sheets(session));
        Assert.Equal(("A4", true, false), (sheet.Paper, sheet.Landscape, sheet.ThirdAngle));
        Assert.Equal(DiagnosticCodes.SheetInvalid, await Refused(session, new CreateSheetCommand { SheetName = "Sheet 2", Paper = "Letter" }));
        Assert.Equal(DiagnosticCodes.SheetInvalid, await Refused(session, new CreateSheetCommand { SheetName = " " }));
        Assert.Equal(DiagnosticCodes.SheetInvalid, await Refused(session, new RenameSheetCommand { SheetId = id, SheetName = "" }));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new RenameSheetCommand { SheetId = Guid.NewGuid(), SheetName = "Other" }));
        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new RemoveSheetCommand { SheetId = Guid.NewGuid() }));
        for (var i = 0; i < 1024; i++)
            await session.Apply(new RedoCommand()); // as for an image: refused Redo commands fill the cache of the results
        Assert.Equal(DiagnosticCodes.SheetIdInUse, await Refused(session, new CreateSheetCommand { SheetName = "Sheet 3" } with { CommandId = id }));
        Assert.Single(await Sheets(session));
    }

    // Items 1 and 6: a rename gives a new record with the same identifier and sheet.changed; a remove gives sheet.removed; an
    // undo of each gives back the record before, and a redo the record after.
    [Fact]
    public async Task Rename_And_Remove_Have_Their_Events_And_Undo_And_Redo_Give_Each_Record()
    {
        var (session, events) = NewSession();
        var id = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1" });
        async Task<IReadOnlyList<string>> Kinds(Command command)
        {
            var before = events.Snapshot().Count;
            var result = await session.Apply(command);
            Assert.Equal(CommandStatus.Applied, result.Status);
            return [.. events.Snapshot().Skip(before).Select(record => record.Kind)];
        }

        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RenameSheetCommand { SheetId = id, SheetName = "Front and top" }));
        Assert.Equal("Front and top", Assert.Single(await Sheets(session)).Name);
        Assert.Equal(["command.applied", "sheet.removed"], await Kinds(new RemoveSheetCommand { SheetId = id }));
        Assert.Empty(await Sheets(session));

        Assert.Equal(["command.applied", "sheet.created"], await Kinds(new UndoCommand()));
        Assert.Equal("Front and top", Assert.Single(await Sheets(session)).Name);
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new UndoCommand()));
        Assert.Equal("Sheet 1", Assert.Single(await Sheets(session)).Name);
        Assert.Equal(["command.applied", "sheet.removed"], await Kinds(new UndoCommand()));
        Assert.Empty(await Sheets(session));

        Assert.Equal(["command.applied", "sheet.created"], await Kinds(new RedoCommand()));
        Assert.Equal(["command.applied", "sheet.changed"], await Kinds(new RedoCommand()));
        Assert.Equal(["command.applied", "sheet.removed"], await Kinds(new RedoCommand()));
        Assert.Empty(await Sheets(session));
    }

    // Item 1: an open of the document file replays the commands and gives the same sheet.
    [Fact]
    public async Task An_Open_Of_The_File_Gives_The_Same_Sheet()
    {
        var path = Path.Combine(_folder, "sheet" + DocumentFile.Extension);
        var (live, _) = NewSession();
        DocumentFile.Create(live, path);
        var id = await Applied(live, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A2", ThirdAngle = true });
        await Applied(live, new RenameSheetCommand { SheetId = id, SheetName = "Bracket" });
        var expected = Assert.Single(await Sheets(live));
        live.Close();

        var (opened, _) = NewSession();
        await DocumentFile.Open(opened, path);
        var actual = Assert.Single(await Sheets(opened));
        opened.Close();

        Assert.Equal((expected.Id, expected.Name, expected.Paper, expected.Landscape, expected.ThirdAngle),
            (actual.Id, actual.Name, actual.Paper, actual.Landscape, actual.ThirdAngle));
        Assert.Equal(("Bracket", "A2", true), (actual.Name, actual.Paper, actual.ThirdAngle));
    }
}
