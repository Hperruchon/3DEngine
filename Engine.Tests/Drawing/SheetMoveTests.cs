using System.Globalization;
using Engine.Contracts;
using Engine.Contracts.Drawing;
using Engine.Contracts.Handlers;
using Engine.Core;
using Engine.Core.Commands;
using Engine.Core.Geometry;
using Engine.Core.Hosting;

namespace Engine.Tests.Drawing;

// ADR-0168 item 3 (phase R30c, TASK-0408): MoveSheetViews moves a view, and with Aligned each view on a line of projection
// through it, in one step of undo.
public sealed class SheetMoveTests
{
    private static readonly double[] Front = [1, 0, 0, 0, 0, 1, 0, -1, 0];
    private static readonly double[] Top = [1, 0, 0, 0, 1, 0, 0, 0, 1];
    private static readonly double[] Right = [0, 1, 0, 0, 0, 1, 1, 0, 0];
    private static readonly double[] Left = [0, -1, 0, 0, 0, 1, -1, 0, 0];

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
        ScaleNumerator = 2, ScaleDenominator = 1, X = x, Y = y,
    };

    private static async Task<string> Places(DocumentSession session)
        => string.Join(" ", (await session.Read((document, _) => document.Sheets.Single())).Views.Select(view => string.Create(CultureInfo.InvariantCulture, $"({view.X},{view.Y})")));

    // The standard views of first angle on A3, and a view from the left right of the front view, and the same top view moved off
    // the line of the front view.
    private static async Task<(DocumentSession Session, Guid Sheet, Guid Front, Guid Top)> Setup()
    {
        var session = new DocumentSession(EngineHosting.CreateDefault(new InProcessMeshBackend()));
        var sheet = await Applied(session, new CreateSheetCommand { SheetName = "Sheet 1", Paper = "A3" }, "sheetId");
        var front = await Applied(session, View(sheet, Front, 255, 203.5), "viewId");
        var top = await Applied(session, View(sheet, Top, 255, 143.5), "viewId");
        await Applied(session, View(sheet, Right, 145, 203.5), "viewId");
        await Applied(session, View(sheet, Left, 365, 203.5), "viewId");
        await Applied(session, View(sheet, Top, 100, 60), "viewId");
        return (session, sheet, front, top);
    }

    // With Aligned, the front view takes the top view under it and the side views at its sides; the top view off its line stays.
    // One Undo gives each place back.
    [Fact]
    public async Task An_Aligned_Move_Takes_The_Views_On_The_Lines_Of_Projection()
    {
        var (session, sheet, front, _) = await Setup();
        var before = await Places(session);

        await Applied(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = 245, Y = 213.5, Aligned = true }, "viewId");

        Assert.Equal("(245,213.5) (245,153.5) (135,213.5) (355,213.5) (100,60)", await Places(session));
        await session.Apply(new UndoCommand());
        Assert.Equal(before, await Places(session));
    }

    // Finding E225 of the codebase review of 2026-11-18 (b) (TASK-0416): a move to the place that the view has, aligned or not,
    // is refused with E-SHEET-VIEW-INVALID, so the log gets no step of undo that changes nothing.
    [Fact]
    public async Task A_Move_To_The_Place_Of_The_View_Is_Refused()
    {
        var (session, sheet, front, _) = await Setup();
        var before = await Places(session);
        var count = await session.Read((document, _) => document.Log.Count);

        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = 255, Y = 203.5 }));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = 255, Y = 203.5, Aligned = true }));

        Assert.Equal(before, await Places(session));
        Assert.Equal(count, await session.Read((document, _) => document.Log.Count));
        await Applied(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = 255, Y = 203 }, "viewId");
    }

    // With no Aligned flag, only the view moves.
    [Fact]
    public async Task A_Move_Not_Aligned_Takes_One_View()
    {
        var (session, sheet, _, top) = await Setup();

        await Applied(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = top, X = 255, Y = 120 }, "viewId");

        Assert.Equal("(255,203.5) (255,120) (145,203.5) (365,203.5) (100,60)", await Places(session));
    }

    // A sheet that is not live, a view that is not on the sheet, and a new place off the paper, also of a view that the move
    // takes, are refused with their codes, and no view moves.
    [Fact]
    public async Task Each_Wrong_Move_Gives_Its_Code()
    {
        var (session, sheet, front, _) = await Setup();
        var before = await Places(session);

        Assert.Equal(DiagnosticCodes.SheetNotFound, await Refused(session, new MoveSheetViewsCommand { SheetId = Guid.NewGuid(), ViewId = front, X = 1, Y = 1 }));
        Assert.Equal(DiagnosticCodes.SheetViewNotFound, await Refused(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = Guid.NewGuid(), X = 1, Y = 1 }));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = 500, Y = 100 }));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = 255, Y = 50, Aligned = true }));
        Assert.Equal(DiagnosticCodes.SheetViewInvalid, await Refused(session, new MoveSheetViewsCommand { SheetId = sheet, ViewId = front, X = double.NaN, Y = 50 }));
        Assert.Equal(before, await Places(session));
    }

    // The handler reads an input with and with no flag, as a client of the HTTP surface gives it.
    [Fact]
    public void The_Handler_Reads_The_Flag()
    {
        var handler = new MoveSheetViewsCommandHandler();
        Dictionary<string, object?> Input(bool? aligned)
        {
            var input = new Dictionary<string, object?> { ["sheetId"] = Guid.NewGuid(), ["viewId"] = Guid.NewGuid(), ["x"] = 10.0, ["y"] = 20.0 };
            if (aligned is { } flag)
                input["aligned"] = flag;
            return input;
        }

        Assert.True(((MoveSheetViewsCommand)handler.Create(new CommandInput(Input(true), Guid.NewGuid(), 0))).Aligned);
        Assert.False(((MoveSheetViewsCommand)handler.Create(new CommandInput(Input(null), Guid.NewGuid(), 0))).Aligned);
        Assert.False(handler.Parameters["aligned"].Required);
    }
}
