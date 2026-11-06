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

        Assert.Equal("3DEngine | plate.3de | mm", ui.Title(new DocumentLine("plate.3de", null)));
        Assert.Equal("3DEngine | unsaved | mm", ui.Title(new DocumentLine(null, null)));
        Assert.Equal("3DEngine | mm", ui.Title(new DocumentLine(null, "Applied", Files: false)));
        Assert.Equal("3DEngine | mm", ui.Title(null));
    }
}
