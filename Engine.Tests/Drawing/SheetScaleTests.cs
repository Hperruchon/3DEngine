using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;
using Engine.Core.Queries;

namespace Engine.Tests.Drawing;

// ADR-0167 item 1 (phase R30b, TASK-0407): ScaleSheetView changes the scale of one view or of each view of a sheet in one
// step of undo; the places and the dimensions stay.
public sealed class SheetScaleTests
{
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];

    private static DocumentSession NewSession() => new(EngineHosting.CreateDefault(new InProcessMeshBackend()));

    private static async Task<Guid> Applied(DocumentSession session, Command command, string output)
    {
        var result = await session.Apply(command);
        Assert.True(result.Status == CommandStatus.Applied, $"{command.Name}: {result.Error?.Code} {result.Error?.Message}");
        return result.Outputs.TryGet<Guid>(output, out var id) ? id : throw new InvalidOperationException($"No output {output}.");
    }

    private static async Task<string> Refused(DocumentSession session, Command command)
    {
        var result = await session.Apply(command);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        return result.Error!.Code;
    }

    private static AddSheetViewCommand View(Guid sheet, double[] d, double x, double y) => new()
    {
        SheetId = sheet, RightX = d[0], RightY = d[1], RightZ = d[2], UpX = d[3], UpY = d[4], UpZ = d[5], EyeX = d[6], EyeY = d[7], EyeZ = d[8],
        ScaleNumerator = 1, ScaleDenominator = 1, X = x, Y = y,
    };

    private static Task<SheetRecord> Sheet(DocumentSession session) => session.Read((document, _) => document.Sheets.Single());

    private static string Text(SheetRecord sheet)
        => string.Join(" ", sheet.Views.Select(view => string.Create(CultureInfo.InvariantCulture, $"{view.ScaleNumerator}:{view.ScaleDenominator}@({view.X},{view.Y})")))
            + " | " + string.Join(" ", sheet.Dimensions.Select(dimension => string.Create(CultureInfo.InvariantCulture, $"{dimension.Id}@({dimension.X},{dimension.Y})")));

    // A box of 40 x 24 x 10 at the origin, a sheet with a front view and a top view at 1 : 1, and a dimension of the extrude
    // of no body: a lost dimension keeps its record, which is all this test needs.
    private static async Task<(DocumentSession Session, Guid Sheet, Guid Front, Guid Top)> Setup()
    {
        var session = NewSession();
        await Applied(session, new CreateBoxCommand { SizeX = 40, SizeY = 24, SizeZ = 10 }, "bodyId");
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A3" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, 200, 180), "viewId");
        var top = await Applied(session, View(sheet, Top, 200, 120), "viewId");
        return (session, sheet, front, top);
    }

    // One view: its scale changes, its place, the other view and the dimensions stay, and GetSheet draws the view at the new
    // scale. One Undo gives the old record, and Redo the new one.
    [Fact]
    public async Task One_View_Takes_The_New_Scale_In_One_Step()
    {
        var (session, sheet, front, _) = await Setup();
        var before = await Sheet(session);
        var width = Width(await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet }), 0);

        await Applied(session, new ScaleSheetViewCommand { SheetId = sheet, ViewId = front, ScaleNumerator = 2, ScaleDenominator = 1 }, "sheetId");

        var after = await Sheet(session);
        Assert.Equal("2:1@(200,180) 1:1@(200,120) | ", Text(after));
        Assert.Equal(2 * width, Width(await session.Query<SheetInfo>(new GetSheetQuery { SheetId = sheet }), 0), 9);

        await session.Apply(new UndoCommand());
        Assert.Equal(Text(before), Text(await Sheet(session)));
        await session.Apply(new RedoCommand());
        Assert.Equal(Text(after), Text(await Sheet(session)));
    }

    // No view named: each view takes the scale, in one step of undo.
    [Fact]
    public async Task No_View_Named_Scales_Each_View_In_One_Step()
    {
        var (session, sheet, _, _) = await Setup();
        var before = await Sheet(session);

        await Applied(session, new ScaleSheetViewCommand { SheetId = sheet, ScaleNumerator = 1, ScaleDenominator = 2 }, "sheetId");

        Assert.Equal("1:2@(200,180) 1:2@(200,120) | ", Text(await Sheet(session)));
        await session.Apply(new UndoCommand());
        Assert.Equal(Text(before), Text(await Sheet(session)));
    }

    // A sheet that is not live, a view that is not on the sheet and a part of the scale that is not greater than zero are refused
    // with their codes, and the sheet does not change. A sheet with no view and no view named keeps its record.
    [Fact]
    public async Task Each_Wrong_Scale_Command_Gives_Its_Code()
    {
        var (session, sheet, front, _) = await Setup();
        var before = Text(await Sheet(session));

        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new ScaleSheetViewCommand { SheetId = Guid.NewGuid(), ScaleNumerator = 1, ScaleDenominator = 1 }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, new ScaleSheetViewCommand { SheetId = sheet, ViewId = Guid.NewGuid(), ScaleNumerator = 1, ScaleDenominator = 1 }));
        foreach (var (numerator, denominator) in new[] { (0L, 1L), (1L, 0L), (-2L, 1L), (1L, -5L) })
            Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new ScaleSheetViewCommand { SheetId = sheet, ViewId = front, ScaleNumerator = numerator, ScaleDenominator = denominator }));
        Assert.Equal(before, Text(await Sheet(session)));

        var empty = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 2" }, "sheetId");
        await Applied(session, new ScaleSheetViewCommand { SheetId = empty, ScaleNumerator = 2, ScaleDenominator = 1 }, "sheetId");
        Assert.Empty((await session.Read((document, _) => document.Sheets.Single(sheet => sheet.Id == empty))).Views);
    }

    // The handler reads a query input with and with no view, as a client of the HTTP surface gives it, and encodes the same
    // fields.
    [Fact]
    public void The_Handler_Reads_And_Encodes_An_Optional_View()
    {
        var handler = new ScaleSheetViewCommandHandler();
        var (sheet, view) = (Guid.NewGuid(), Guid.NewGuid());
        var one = (ScaleSheetViewCommand)handler.Create(new Engine.Contracts.Handlers.CommandInput(
            new Dictionary<string, object?> { ["sheetId"] = sheet, ["viewId"] = view, ["scaleNumerator"] = 5L, ["scaleDenominator"] = 1L }, Guid.NewGuid(), 0));
        var each = (ScaleSheetViewCommand)handler.Create(new Engine.Contracts.Handlers.CommandInput(
            new Dictionary<string, object?> { ["sheetId"] = sheet, ["scaleNumerator"] = 1L, ["scaleDenominator"] = 10L }, Guid.NewGuid(), 0));

        Assert.Equal((sheet, (Guid?)view, 5L, 1L), (one.SheetId, one.ViewId, one.ScaleNumerator, one.ScaleDenominator));
        Assert.Null(each.ViewId);
        Assert.False(handler.Encode(each).ContainsKey("viewId"));
        Assert.Equal(view, handler.Encode(one)["viewId"]);
        Assert.False(handler.Parameters["viewId"].Required);
    }

    // The width on the paper of the lines of a view.
    private static double Width(QueryResult<SheetInfo> read, int view)
    {
        var lines = read.Result!.Views[view].Lines;
        return lines.Max(line => Math.Max(line.FromX, line.ToX)) - lines.Min(line => Math.Min(line.FromX, line.ToX));
    }
}
