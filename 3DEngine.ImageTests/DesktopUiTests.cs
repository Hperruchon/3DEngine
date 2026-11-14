using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The panel of the desktop with the library and no GPU (ADR-0027 items 4 to 8, TASK-0084): a
// tap and a click give the same action, typed text reaches the text field, a gesture over the
// panel does not move the camera, and each control is large enough for a finger. These tests
// live here because only this verifier may reference the host; they run on each runner.
//
// Dear ImGui spreads a press and a release of one frame over later frames, so a tap here is a
// frame with the press, a frame with the release, and two frames with no sample.
[Collection(DearImGui.Collection)]
public class DesktopUiTests
{
    // ADR-0131 item 1 (phase R18k, TASK-0360): on this portrait display the panel shows with no selected body, and the rows of
    // a selected body show under the part, with the ring of the body under them. Until TASK-0360 the panel kept the controls
    // of the selected body on a portrait display.
    private static readonly Vector2 Display = new(720, 1280);

    private static readonly LiveBody[] Bodies =
    [
        new(Guid.Parse("11111111-0000-0000-0000-000000000000"), "Box", null, true),
        new(Guid.Parse("22222222-0000-0000-0000-000000000000"), "Solid", null, true),
    ];

    private static IReadOnlyList<InputSample> Step(DesktopUi ui, params InputSample[] frame) => StepWith(ui, null, frame);

    // One frame with this selection (ADR-0029 item 7).
    private static IReadOnlyList<InputSample> StepWith(DesktopUi ui, Guid? selection, params InputSample[] frame)
    {
        var intents = ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies, selection, ringCentre: Display / 2f, ringBody: 120f);
        return intents;
    }

    // Two frames, so that the panel has its layout and its rectangles.
    private static DesktopUi Opened()
    {
        var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Step(ui);
        Step(ui);
        return ui;
    }

    private static PointerSample Pointer(DeviceKind kind, (double X, double Y) at, PointerPhase phase)
        => new(kind, 1, kind == DeviceKind.Mouse ? 0UL : 3UL, at.X, at.Y, 1,
            kind == DeviceKind.Mouse ? PointerButtons.Left : PointerButtons.None, KeyModifiers.None, phase, 0);

    private static (double X, double Y) Centre(DesktopUi ui, string label)
    {
        var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
        return ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
    }

    private static void Tap(DesktopUi ui, DeviceKind kind, (double X, double Y) at, Guid? selection = null)
    {
        StepWith(ui, selection, Pointer(kind, at, PointerPhase.Down));
        StepWith(ui, selection, Pointer(kind, at, PointerPhase.Up));
        StepWith(ui, selection);
        StepWith(ui, selection);
    }

    private static void Type(DesktopUi ui, string text, string field = DesktopUi.FilterLabel, Guid? selection = null)
    {
        Tap(ui, DeviceKind.Mouse, Centre(ui, field), selection);
        StepWith(ui, selection, new TextSample(text, 0));
        StepWith(ui, selection);
    }

    // ADR-0027 item 6.
    [Fact]
    public void Each_Control_Is_At_Least_44_Pixels_High()
    {
        using var ui = Opened();

        Assert.True(ui.ControlHeight >= DesktopUi.MinimumControlHeight, $"A control is {ui.ControlHeight} pixels high.");
        foreach (var label in new[] { DesktopUi.FilterLabel, DesktopUi.ClearLabel })
        {
            var rectangle = ui.ItemRectangle(label)!.Value;
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{label} is {rectangle.W - rectangle.Y} pixels high.");
        }
    }

    [Fact]
    public void Typed_Text_Reaches_The_Filter_And_Filters_The_Bodies()
    {
        using var ui = Opened();
        Assert.Equal(2, ui.ShownBodies.Count);

        Type(ui, "solid");

        Assert.Equal("solid", ui.Filter);
        Assert.StartsWith("Solid", Assert.Single(ui.ShownBodies), StringComparison.Ordinal);
    }

    // Objective 9 of docs/CHARTER.md: a finger and a mouse give the same action.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Pen)]
    public void A_Tap_And_A_Click_On_Clear_Give_The_Same_Action(DeviceKind kind)
    {
        using var ui = Opened();
        Type(ui, "box");
        Assert.Equal("box", ui.Filter);

        Tap(ui, kind, Centre(ui, DesktopUi.ClearLabel));

        Assert.Equal(string.Empty, ui.Filter);
        Assert.Equal(2, ui.ShownBodies.Count);
    }

    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void A_Drag_Over_The_Panel_Does_Not_Move_The_Camera_And_A_Drag_Outside_Does(DeviceKind kind)
    {
        using var ui = Opened();
        var panel = ui.PanelRectangles.Single();
        (double X, double Y) inside = ((panel.X + panel.Z) / 2, (panel.Y + panel.W) / 2);
        (double X, double Y) outside = (panel.Z + 300, (panel.Y + panel.W) / 2);

        Assert.Empty(Drag(ui, kind, inside).OfType<OrbitIntent>());
        Assert.NotEmpty(Drag(ui, kind, outside).OfType<OrbitIntent>());
    }

    private static List<Intent> Drag(DesktopUi ui, DeviceKind kind, (double X, double Y) from)
    {
        var machine = new IntentMachine();
        var intents = new List<Intent>();
        intents.AddRange(machine.Step(Step(ui, Pointer(kind, from, PointerPhase.Down))));
        for (var step = 1; step <= 5; step++)
            intents.AddRange(machine.Step(Step(ui, Pointer(kind, (from.X + 20 * step, from.Y), PointerPhase.Move))));
        intents.AddRange(machine.Step(Step(ui, Pointer(kind, (from.X + 100, from.Y), PointerPhase.Up))));
        return intents;
    }

    // The key plus of a typed "+" reaches the text field and not the camera.
    [Fact]
    public void A_Key_Typed_Into_The_Filter_Does_Not_Zoom()
    {
        using var ui = Opened();
        Type(ui, "a");

        var machine = new IntentMachine();
        var intents = machine.Step(Step(ui, new KeySample(InputKey.Plus, true, KeyModifiers.None, 0), new TextSample("+", 0)));
        Step(ui);

        Assert.DoesNotContain(intents, intent => intent is ZoomIntent);
        Assert.Equal("a+", ui.Filter);
    }

    [Fact]
    public void A_Frame_Gives_Draw_Data_That_Names_The_Font_Texture()
    {
        using var ui = Opened();
        ui.SetFontTexture(7);

        var data = ui.Frame(1f / 60f, Display, Vector2.One, Bodies);

        Assert.True(UiDrawDataConverter.VertexLayoutAgrees);
        Assert.NotEmpty(data.Vertices);
        Assert.NotEmpty(data.Commands);
        Assert.All(data.Commands, command => Assert.Equal(7UL, command.Texture));
        Assert.All(data.Commands, command => Assert.True(command.FirstIndex + command.IndexCount <= data.Indices.Length));
        Assert.True(data.Indices.Max() < data.Vertices.Length);
        Assert.Equal(Display, data.DisplaySize);
    }

    // ADR-0029 item 7: a row gives the name, or the kind and a short identifier, and "hidden".
    [Fact]
    public void A_Row_Gives_The_Name_Or_The_Kind_And_The_Word_Hidden()
    {
        var id = Guid.Parse("33333333-0000-0000-0000-000000000000");

        Assert.Equal("Box  33333333", DesktopUi.RowText(new LiveBody(id, "Box", null, true)));
        Assert.Equal("Plate", DesktopUi.RowText(new LiveBody(id, "Box", "Plate", true)));
        Assert.Equal("Plate  (hidden)", DesktopUi.RowText(new LiveBody(id, "Box", "Plate", false)));
    }

    // ADR-0037 item 6 (K0c, TASK-0109): a body whose last rebuild failed has the word "fault" in
    // its row, and the selected body gives the reason; a body with no fault gives none. ADR-0131 item 1 (phase R18k,
    // TASK-0360): the first row of the rows of the selected body gives "failed" and the code in red, and a pointer over it
    // gives the reason. Until TASK-0360 the panel gave a line "Fault" on a portrait display.
    [Fact]
    public void A_Body_With_A_Fault_Shows_The_Word_And_The_Reason()
    {
        var id = Guid.Parse("44444444-0000-0000-0000-000000000000");
        var fault = new Engine.Contracts.BodyFault("W-SKETCH-REBUILD-FAILED", "The extrude keeps its last solid: the lines are open.");

        Assert.Equal("Plate  (fault)", DesktopUi.RowText(new LiveBody(id, "Extrude", "Plate", true, fault)));
        Assert.Equal("Plate  (hidden)  (fault)", DesktopUi.RowText(new LiveBody(id, "Extrude", "Plate", false, fault)));

        LiveBody[] faulty = [new(id, "Extrude", "Plate", true, fault)];
        LiveBody[] sound = [new(id, "Extrude", "Plate", true)];
        using var withFault = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        using var withNone = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        for (var frame = 0; frame < 2; frame++)
        {
            withFault.Frame(1f / 60f, Display, Vector2.One, faulty, id);
            withNone.Frame(1f / 60f, Display, Vector2.One, sound, id);
        }

        Assert.NotNull(withFault.ItemRectangle(DesktopUi.RowsTitleItem));
        Assert.NotNull(withNone.ItemRectangle(DesktopUi.RowsTitleItem));
        Assert.Contains(withFault.RowTitle(faulty[0]).State, part => part.Red && part.Text.StartsWith("failed W-SKETCH", StringComparison.Ordinal));
        Assert.DoesNotContain(withNone.RowTitle(sound[0]).State, part => part.Red || part.Text.StartsWith("failed", StringComparison.Ordinal));
    }

    // A tap of a finger and a click of the mouse on a row select its body.
    [Theory]
    [InlineData(DeviceKind.Mouse)]
    [InlineData(DeviceKind.Touch)]
    public void A_Tap_On_A_Row_Selects_Its_Body(DeviceKind kind)
    {
        using var ui = Opened();

        Tap(ui, kind, Centre(ui, DesktopUi.RowText(Bodies[1])));

        Assert.Equal(new SelectBodyAction(Bodies[1].Id), Assert.Single(ui.TakeActions()));
    }

    // The controls of the selected body give a rename, a hide and a show all, and the panel
    // changes nothing itself. ADR-0131 item 1 (phase R18k, TASK-0360): a tap on the first row of the rows opens the name
    // field, Hide is a segment of the ring, and Show all is in the panel with no selection. Until TASK-0360 the panel gave
    // the name field, Rename and Hide on a portrait display.
    [Fact]
    public void The_Controls_Of_The_Selected_Body_Give_A_Rename_A_Hide_And_A_Show_All()
    {
        using var ui = Opened();
        var selected = Bodies[0].Id;
        StepWith(ui, selected);
        StepWith(ui, selected);

        Rename(ui, "Plate", selected);
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.RingItem(DesktopUi.HideLabel)), selected);
        Step(ui);
        Step(ui);
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.ShowAllLabel));

        Assert.Equal(
            [new RenameBodyAction(selected, "Plate"), new SetVisibleAction(selected, false), new ShowAllAction()],
            ui.TakeActions());
        Assert.Empty(ui.TakeActions());
    }

    // ADR-0099 item 2: a tap on the first row of the rows opens the name field; the text and Enter rename the body. The focus
    // of ImGui takes the field in a frame after the request, and the field is active in the frame after that.
    private static void Rename(DesktopUi ui, string text, Guid selection)
    {
        Tap(ui, DeviceKind.Touch, Centre(ui, DesktopUi.RowsTitleItem), selection);
        StepWith(ui, selection);
        if (text.Length > 0)
            StepWith(ui, selection, new TextSample(text, 0));
        StepWith(ui, selection, new KeySample(InputKey.Enter, true, KeyModifiers.None, 0));
        StepWith(ui, selection, new KeySample(InputKey.Enter, false, KeyModifiers.None, 0));
        StepWith(ui, selection);
    }

    // ADR-0027 item 6 for the controls of ADR-0029 item 7. ADR-0131 item 1 (phase R18k, TASK-0360): the row of a body and
    // Show all are in the panel with no selection; with a selected body the first row of the rows, the list of a cut and the
    // segment Hide of the ring are at least 44 pixels high.
    [Fact]
    public void Each_Row_And_Each_Control_Of_The_Selection_Is_At_Least_44_Pixels_High()
    {
        using var ui = Opened();
        foreach (var label in new[] { DesktopUi.RowText(Bodies[0]), DesktopUi.ShowAllLabel })
        {
            var rectangle = ui.ItemRectangle(label) ?? throw new InvalidOperationException($"The panel has no control {label}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{label} is {rectangle.W - rectangle.Y} pixels high.");
        }

        var selected = Bodies[0].Id;
        StepWith(ui, selected);
        StepWith(ui, selected);
        foreach (var (label, rectangle) in new[] { (DesktopUi.RowsTitleItem, ui.ItemRectangle(DesktopUi.RowsTitleItem)), (DesktopUi.CutLabel, ui.ItemRectangle(DesktopUi.CutLabel)), (DesktopUi.HideLabel, ui.RingTarget(DesktopUi.HideLabel)) })
        {
            var shown = rectangle ?? throw new InvalidOperationException($"The window has no control {label}.");
            Assert.True(shown.W - shown.Y >= DesktopUi.MinimumControlHeight, $"{label} is {shown.W - shown.Y} pixels high.");
        }
    }

    [Fact]
    public void The_Font_Atlas_Is_Rgba_With_White_Glyphs()
    {
        using var ui = Opened();

        var (width, height, rgba) = ui.FontAtlas();

        Assert.Equal((int)(width * height * 4), rgba.Length);
        Assert.Contains(Enumerable.Range(0, rgba.Length / 4), pixel => rgba[pixel * 4 + 3] == 255 && rgba[pixel * 4] == 255);
    }

    // Finding V26 of the codebase review of 2026-10-28 (TASK-0090): a click on the scene ends
    // the text field, so the next key goes to the scene and not to the field.
    [Fact]
    public void A_Click_On_The_Scene_Ends_The_Text_Field_And_The_Next_Key_Goes_To_The_Scene()
    {
        using var ui = Opened();
        Type(ui, "a");

        Tap(ui, DeviceKind.Mouse, (1200, 600));
        var key = new KeySample(InputKey.H, true, KeyModifiers.None, 0);
        var intents = Step(ui, key);

        Assert.Contains(key, intents);
    }

    // Finding V26: a name typed for one body and a click on another body gives no rename of
    // the second body with the typed name. ADR-0131 item 1 (phase R18k, TASK-0360): the name field is in the rows; the click
    // on the scene closes it with no command. Until TASK-0360 the panel had the name field on a portrait display.
    [Fact]
    public void A_Name_Typed_For_One_Body_Does_Not_Rename_The_Body_That_A_Click_Selects()
    {
        using var ui = Opened();
        var first = Bodies[0].Id;
        var second = Bodies[1].Id;
        StepWith(ui, first);
        StepWith(ui, first);
        Tap(ui, DeviceKind.Touch, Centre(ui, DesktopUi.RowsTitleItem), first);
        StepWith(ui, first);
        StepWith(ui, first, new TextSample("X", 0));
        StepWith(ui, first);

        Tap(ui, DeviceKind.Mouse, (360, 300), first);
        Assert.Null(ui.ItemRectangle(DesktopUi.RenameItem));
        StepWith(ui, second);
        StepWith(ui, second);
        Rename(ui, string.Empty, second);

        Assert.Equal([new RenameBodyAction(second, string.Empty)], ui.TakeActions());
    }
}
