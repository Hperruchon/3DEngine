using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0080 (the redesign, slice b1, TASK-0248): the fixed controls, with no GPU. ADR-0080 names this file in its
// enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class FixedControlsTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static void Step(DesktopUi ui, DocumentLine? document, params InputSample[] frame)
    {
        ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [], null, document);
    }

    // Item 2: Undo and Redo are two round anchors of 48 pixels at the bottom left, outside the panel and under no panel,
    // and a tap on them does not reach the scene.
    [Fact]
    public void Undo_And_Redo_Are_Round_Anchors_At_The_Bottom_Left()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui, null);
        Step(ui, null);

        var undo = ui.ItemRectangle(DesktopUi.UndoLabel) ?? throw new InvalidOperationException("No Undo.");
        var redo = ui.ItemRectangle(DesktopUi.RedoLabel) ?? throw new InvalidOperationException("No Redo.");
        Assert.Equal((16f, Display.Y - 16f - DesktopUi.AnchorSize, DesktopUi.AnchorSize, DesktopUi.AnchorSize), (undo.X, undo.Y, undo.Z - undo.X, undo.W - undo.Y));
        Assert.True(redo.X > undo.Z && redo.Y == undo.Y, $"Redo at {redo} is not right of Undo at {undo}.");
        var panel = ui.PanelRectangles[0];
        Assert.True(panel.W <= undo.Y, $"The panel ends at {panel.W}, under the top of Undo at {undo.Y}.");
        Assert.True(ui.OverPanel((undo.X + undo.Z) / 2, (undo.Y + undo.W) / 2), "A tap on Undo reaches the scene.");
        // ADR-0093 item 3 (TASK-0304): the status line is at the lower right, on the row of the anchors.
        var status = ui.ItemRectangle(DesktopUi.StatusLabel) ?? throw new InvalidOperationException("No status line.");
        Assert.True(status.X > redo.Z && status.Y >= undo.Y && status.W <= undo.W, $"The status line at {status} is not on the row of the anchors.");
        Assert.True(Math.Abs(status.Z - (Display.X - 16f)) <= 1f, $"The status line at {status} does not end 16 pixels inside the right edge.");
        Assert.Equal("0 bodies · all correct · http://127.0.0.1:5180", ui.StatusText([]));
    }

    // Item 1, with ADR-0093 item 1 (TASK-0304): the top bar names the program, the file or "unsaved", and the unit; a host
    // with no files, as the phone, gives the program and the unit. It is at the upper left, with no ground.
    [Fact]
    public void The_Title_Names_The_Document_And_The_Unit()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);

        Assert.Equal(["3DEngine", "plate.3de", "mm"], ui.TitleParts(new DocumentLine("plate.3de", null)));
        Assert.Equal(["3DEngine", "unsaved", "mm"], ui.TitleParts(new DocumentLine(null, null)));
        Assert.Equal(["3DEngine", "mm"], ui.TitleParts(new DocumentLine(null, "Applied", Files: false)));
        Assert.Equal(["3DEngine", "mm"], ui.TitleParts(null));
        Step(ui, new DocumentLine("plate.3de", null));
        Step(ui, new DocumentLine("plate.3de", null));
        var title = ui.ItemRectangle(DesktopUi.TitleItem) ?? throw new InvalidOperationException("No top bar.");
        Assert.Equal((16f, 16f), (title.X, title.Y));
        Assert.True(title.W <= ui.PanelRectangles[0].Y, $"The top bar at {title} is under the top of the panel.");
    }

    // Finding V115 of the codebase review of 2026-11-06 (e) (TASK-0252), with ADR-0093 item 1 (TASK-0304): the top bar cuts a
    // long name to the width of the rows, so the unit after it stays on the display.
    [Fact]
    public void A_Long_File_Name_Does_Not_Cut_The_Unit()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var name = "bracket-v2-final-for-the-printer-and-the-workshop-of-the-second-floor.3de";
        var document = new DocumentLine(name, null);
        Step(ui, document);
        Step(ui, document);

        Assert.True(ui.TextSize(name).X > DesktopUi.RowsWidth, "The name is not long enough for the test.");
        var title = ui.ItemRectangle(DesktopUi.TitleItem) ?? throw new InvalidOperationException("No top bar.");
        var most = ui.TextSize("3DEngine").X + DesktopUi.RowsWidth + ui.TextSize("mm").X + 24f + 1f;
        Assert.True(title.Z - title.X <= most && title.Z < Display.X, $"The top bar at {title} is wider than {most}, so a long name pushes the unit out.");
    }

    // Finding V114 of the codebase review of 2026-11-06 (e) (TASK-0252): a drag down to the anchors, and a display that
    // becomes smaller, do not put the panel over Undo, and a tap on Undo gives its action. ADR-0093 item 2 (TASK-0304): the
    // panel has no title bar and stays under the top bar, so a drag from its top or from the top bar moves nothing.
    [Theory]
    [InlineData(26)]
    [InlineData(64)]
    public void A_Panel_That_The_Person_Drags_Down_Or_A_Smaller_Display_Does_Not_Cover_Undo(int from)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var tall = new Vector2(1280, 1024);
        void Frame(Vector2 display, params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, display, Vector2.One, [], null, null);
        }

        Frame(tall);
        Frame(tall);
        Frame(tall, Mouse(300, from, PointerButtons.Left, PointerPhase.Down, 0));
        Frame(tall, Mouse(300, 600, PointerButtons.Left, PointerPhase.Move, 50));
        Frame(tall, Mouse(300, 900, PointerButtons.Left, PointerPhase.Move, 100));
        Frame(tall, Mouse(300, 900, PointerButtons.Left, PointerPhase.Up, 150));
        Frame(Display);
        Frame(Display);

        var undo = ui.ItemRectangle(DesktopUi.UndoLabel) ?? throw new InvalidOperationException("No Undo.");
        var panel = ui.PanelRectangles[0];
        Assert.True(panel.W <= undo.Y && panel.Y == 16f + DesktopUi.TopBarHeight + 8f, $"The panel at {panel} covers Undo at {undo}, or it moved.");
        var (x, y) = ((undo.X + undo.Z) / 2, (undo.Y + undo.W) / 2);
        Frame(Display, Mouse(x, y, PointerButtons.None, PointerPhase.Move, 200));
        Frame(Display, Mouse(x, y, PointerButtons.Left, PointerPhase.Down, 250));
        Frame(Display, Mouse(x, y, PointerButtons.Left, PointerPhase.Up, 300));
        Frame(Display);
        Frame(Display);
        Assert.Contains(ui.TakeActions(), action => action is UndoAction);
    }

    // Finding V118 of the codebase review of 2026-11-06 (e) (TASK-0252): with the insets of the system bars of a phone, the
    // panel starts under the top inset, and Undo, Redo and the status line are above the bottom inset.
    [Fact]
    public void The_Fixed_Controls_And_The_Panel_Are_Inside_The_Safe_Area()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var inset = new Vector4(24f, 40f, 0f, 48f);
        for (var frame = 0; frame < 2; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null, safeInsets: inset);
        }

        var undo = ui.ItemRectangle(DesktopUi.UndoLabel) ?? throw new InvalidOperationException("No Undo.");
        var status = ui.ItemRectangle(DesktopUi.StatusLabel) ?? throw new InvalidOperationException("No status line.");
        var panel = ui.PanelRectangles[0];
        Assert.Equal((16f + inset.X, Display.Y - 16f - inset.W - DesktopUi.AnchorSize), (undo.X, undo.Y));
        Assert.True(status.W <= Display.Y - inset.W, $"The status line at {status} is under the bottom inset.");
        Assert.Equal((16f + inset.X, 16f + inset.Y + DesktopUi.TopBarHeight + 8f), (panel.X, panel.Y));
        var title = ui.ItemRectangle(DesktopUi.TitleItem) ?? throw new InvalidOperationException("No top bar.");
        Assert.Equal((16f + inset.X, 16f + inset.Y), (title.X, title.Y));
        Assert.True(panel.W <= undo.Y, $"The panel at {panel} covers Undo at {undo}.");
    }

    // Finding V142 of the codebase review of 2026-11-08 (b) (TASK-0302): on a narrow display the panel ends inside the display,
    // with the safe area of a phone too; on a wide display it keeps its width of 460. Until TASK-0302 it ended at 476 on a
    // display of 390.
    [Theory]
    [InlineData(390, 844, 0, 0)]
    [InlineData(390, 844, 24, 0)]
    [InlineData(1280, 720, 0, 0)]
    public void The_Panel_Ends_Inside_The_Display(int width, int height, int left, int right)
    {
        var display = new Vector2(width, height);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 3; frame++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, display, Vector2.One, [], null, null, safeInsets: new Vector4(left, 0f, right, 0f));
        }

        var panel = ui.PanelRectangles[0];
        Assert.True(panel.Z <= display.X - right, $"The panel ends at {panel.Z}, outside the display {display}.");
        if (width >= 500)
            Assert.Equal(460f, panel.Z - panel.X);
    }

    // Finding V123 of the codebase review of 2026-11-07 (TASK-0264): a double click on the title bar collapsed the panel, and
    // a drag of the collapsed panel put it over Undo. ADR-0093 item 2 (TASK-0304): the panel has no title bar, so a double
    // click and a drag at its top neither fold it nor move it, and a click on Undo gives its action.
    [Fact]
    public void A_Double_Click_And_A_Drag_At_The_Top_Of_The_Panel_Neither_Fold_Nor_Move_It()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null);
        }

        Frame();
        Frame();
        var before = ui.PanelRectangles[0];
        var top = before.Y + 4f;
        Frame(Mouse(300, top, PointerButtons.None, PointerPhase.Move, 0));
        Frame(Mouse(300, top, PointerButtons.Left, PointerPhase.Down, 0));
        Frame(Mouse(300, top, PointerButtons.Left, PointerPhase.Up, 20));
        Frame(Mouse(300, top, PointerButtons.Left, PointerPhase.Down, 40));
        Frame(Mouse(300, top, PointerButtons.Left, PointerPhase.Up, 60));
        Frame();
        Frame(Mouse(300, top, PointerButtons.Left, PointerPhase.Down, 600));
        Frame(Mouse(80, 400, PointerButtons.Left, PointerPhase.Move, 650));
        Frame(Mouse(60, 690, PointerButtons.Left, PointerPhase.Move, 700));
        Frame(Mouse(60, 690, PointerButtons.Left, PointerPhase.Up, 750));
        Frame();
        Frame();

        var undo = ui.ItemRectangle(DesktopUi.UndoLabel) ?? throw new InvalidOperationException("No Undo.");
        var panel = ui.PanelRectangles[0];
        Assert.Equal(before, panel);
        Assert.True(panel.W <= undo.Y && panel.X >= 16f, $"The panel at {panel} covers Undo at {undo} or leaves the display.");
        var (x, y) = ((undo.X + undo.Z) / 2, (undo.Y + undo.W) / 2);
        Frame(Mouse(x, y, PointerButtons.None, PointerPhase.Move, 1200));
        Frame(Mouse(x, y, PointerButtons.Left, PointerPhase.Down, 1250));
        Frame(Mouse(x, y, PointerButtons.Left, PointerPhase.Up, 1300));
        Frame();
        Frame();
        Assert.Contains(ui.TakeActions(), action => action is UndoAction);
    }

    private static PointerSample Mouse(double x, double y, PointerButtons buttons, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, buttons, KeyModifiers.None, phase, ms * 1_000_000);
}
