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
        var status = ui.ItemRectangle(DesktopUi.StatusLabel) ?? throw new InvalidOperationException("No status line.");
        Assert.True(status.X > redo.Z && status.Y >= undo.Y && status.W <= undo.W, $"The status line at {status} is not on the row of the anchors.");
    }

    // Item 1: the title bar of the panel names the program, the file or "unsaved", and the unit; a host with no files, as
    // the phone, gives the program and the unit.
    [Fact]
    public void The_Title_Names_The_Document_And_The_Unit()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);

        Assert.Equal("3DEngine | mm | plate.3de", ui.Title(new DocumentLine("plate.3de", null)));
        Assert.Equal("3DEngine | mm | unsaved", ui.Title(new DocumentLine(null, null)));
        Assert.Equal("3DEngine | mm", ui.Title(new DocumentLine(null, "Applied", Files: false)));
        Assert.Equal("3DEngine | mm", ui.Title(null));
    }

    // Finding V115 of the codebase review of 2026-11-06 (e) (TASK-0252): with a long name, the program and the unit are
    // inside the title bar of the panel, before the arrow of the collapse and the padding; the name can be cut.
    [Fact]
    public void A_Long_File_Name_Does_Not_Cut_The_Unit()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var document = new DocumentLine("bracket-v2-final-for-the-printer.3de", null);
        Step(ui, document);
        Step(ui, document);

        var title = ui.Title(document);
        var unit = title[..(title.IndexOf("mm", StringComparison.Ordinal) + 2)];
        var panel = ui.PanelRectangles[0];
        Assert.True(ui.TextSize(unit).X + 64f < panel.Z - panel.X, $"The text \"{unit}\" does not fit in a panel of {panel.Z - panel.X} pixels.");
        Assert.True(ui.TextSize(title).X > panel.Z - panel.X - 64f, "The name is not long enough for the test.");
    }

    // Finding V114 of the codebase review of 2026-11-06 (e) (TASK-0252): a drag of the title bar of the panel down to the
    // anchors, and a display that becomes smaller, do not put the panel over Undo, and a tap on Undo gives its action.
    [Fact]
    public void A_Panel_That_The_Person_Drags_Down_Or_A_Smaller_Display_Does_Not_Cover_Undo()
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
        Frame(tall, Mouse(100, 26, PointerButtons.Left, PointerPhase.Down, 0));
        Frame(tall, Mouse(100, 600, PointerButtons.Left, PointerPhase.Move, 50));
        Frame(tall, Mouse(100, 900, PointerButtons.Left, PointerPhase.Move, 100));
        Frame(tall, Mouse(100, 900, PointerButtons.Left, PointerPhase.Up, 150));
        Frame(Display);
        Frame(Display);

        var undo = ui.ItemRectangle(DesktopUi.UndoLabel) ?? throw new InvalidOperationException("No Undo.");
        var panel = ui.PanelRectangles[0];
        Assert.True(panel.W <= undo.Y && panel.Y == 16f, $"The panel at {panel} covers Undo at {undo}.");
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
        Assert.Equal((16f + inset.X, 16f + inset.Y), (panel.X, panel.Y));
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

    // Finding V123 of the codebase review of 2026-11-07 (TASK-0264): a double click on the title bar collapses the panel,
    // and a drag of the collapsed panel to the bottom left put it over Undo and out of the display on the left, because
    // the clamp ran only for a panel that was not collapsed. Now the collapsed panel stays above the anchors and 16 pixels
    // inside the left edge, and a click on Undo gives its action.
    [Fact]
    public void A_Collapsed_Panel_That_The_Person_Drags_Does_Not_Cover_Undo()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        void Frame(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [], null, null);
        }

        Frame();
        Frame();
        Frame(Mouse(100, 26, PointerButtons.None, PointerPhase.Move, 0));
        Frame(Mouse(100, 26, PointerButtons.Left, PointerPhase.Down, 0));
        Frame(Mouse(100, 26, PointerButtons.Left, PointerPhase.Up, 20));
        Frame(Mouse(100, 26, PointerButtons.Left, PointerPhase.Down, 40));
        Frame(Mouse(100, 26, PointerButtons.Left, PointerPhase.Up, 60));
        Frame();
        var collapsed = ui.PanelRectangles[0];
        Assert.True(collapsed.W - collapsed.Y < 100f, $"The double click did not collapse the panel: {collapsed}.");
        Frame(Mouse(100, 26, PointerButtons.Left, PointerPhase.Down, 600));
        Frame(Mouse(80, 400, PointerButtons.Left, PointerPhase.Move, 650));
        Frame(Mouse(60, 690, PointerButtons.Left, PointerPhase.Move, 700));
        Frame(Mouse(60, 690, PointerButtons.Left, PointerPhase.Up, 750));
        Frame();
        Frame();

        var undo = ui.ItemRectangle(DesktopUi.UndoLabel) ?? throw new InvalidOperationException("No Undo.");
        var panel = ui.PanelRectangles[0];
        Assert.True(panel.W <= undo.Y && panel.X >= 16f, $"The collapsed panel at {panel} covers Undo at {undo} or leaves the display.");
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
