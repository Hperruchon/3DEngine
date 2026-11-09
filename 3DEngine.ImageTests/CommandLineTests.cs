using System.Numerics;
using Engine.Contracts.Sketch;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0098 (the redesign, slice o, TASK-0312): the command line, with no GPU. ADR-0098 names this file in enforced-by.
[Collection(DearImGui.Collection)]
public sealed class CommandLineTests
{
    private static readonly Vector2 Display = new(1440, 900);
    private static readonly LiveBody Extrude = new(Guid.NewGuid(), "Extrude", "Bracket", true);

    private static void Frame(DesktopUi ui, Vector2 display, SketchPanel? sketch = null, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Extrude], Extrude.Id, null, sketchPanel: sketch);
    }

    private static KeySample Key(InputKey key, KeyModifiers modifiers = KeyModifiers.None) => new(key, true, modifiers, 0);

    private static KeySample Up(InputKey key) => new(key, false, KeyModifiers.None, 0);

    // Types the text after Control and K, then each key with its release, one frame each.
    private static void Type(DesktopUi ui, string text, SketchPanel? sketch = null, params InputKey[] keys)
    {
        Frame(ui, Display, sketch, Key(InputKey.K, KeyModifiers.Control));
        // The focus of ImGui takes the field in a frame after the request, and the field is active in the frame after that;
        // after a pad closes it takes one frame more. A person types after many frames.
        Frame(ui, Display, sketch);
        Frame(ui, Display, sketch);
        Frame(ui, Display, sketch);
        Frame(ui, Display, sketch, new TextSample(text, 0));
        Frame(ui, Display, sketch);
        foreach (var key in keys)
        {
            Frame(ui, Display, sketch, Key(key));
            Frame(ui, Display, sketch, Up(key));
        }

        Frame(ui, Display, sketch);
    }

    // Items 1 and 3: Control and K give the field the keys; a text lists the commands whose name holds it, and Enter runs the
    // first one, as its button does.
    [Fact]
    public void Control_K_Then_A_Text_And_Enter_Runs_The_First_Match()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, Display);
        Frame(ui, Display);
        var field = ui.ItemRectangle(DesktopUi.CommandItem) ?? throw new InvalidOperationException("No command line.");
        Assert.Equal((Display.X - 16f, 16f), (field.Z, field.Y));

        Type(ui, "und", null, InputKey.Enter);

        Assert.Contains(ui.TakeActions(), action => action is UndoAction);
        Assert.Null(ui.ItemRectangle(DesktopUi.CommandListItem(DesktopUi.UndoLabel)));
    }

    // Item 3: a command that needs a number opens its pad (ADR-0032), and the arrows choose another match.
    [Fact]
    public void A_Command_With_A_Number_Opens_Its_Pad_And_The_Arrows_Choose()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, Display);
        Type(ui, "box", null, InputKey.Enter);
        Assert.Equal(DesktopUi.NewBoxLabel, ui.Pad?.Title);
        ui.EndPad(null);
        Frame(ui, Display);

        Type(ui, "sketch", null, InputKey.ArrowDown, InputKey.Enter);
        Assert.Equal([new NewSketchAction(Datums.XZ.Id)], ui.TakeActions().OfType<NewSketchAction>());
    }

    // Item 3: the commands follow the state: the tools of an active sketch, the tools of the selected body.
    [Fact]
    public void The_Commands_Follow_The_State()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, Display);
        Type(ui, "set dis");
        Assert.NotNull(ui.ItemRectangle(DesktopUi.CommandListItem(DesktopUi.SetDistanceLabel)));
        Frame(ui, Display, null, Key(InputKey.Escape));
        Frame(ui, Display, null, Up(InputKey.Escape));
        Frame(ui, Display);
        Assert.Null(ui.ItemRectangle(DesktopUi.CommandListItem(DesktopUi.SetDistanceLabel)));

        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], sketch, SketchToolKind.Select, false, [], null);
        Frame(ui, Display, panel);
        Type(ui, "lin", panel, InputKey.Enter);
        Assert.Contains(ui.TakeActions(), action => action == new SketchToolAction(SketchToolKind.Line));
    }

    // Item 2: Escape empties the field and gives the keys back, so the key H reaches the scene again; a tap on a listed
    // command runs it.
    [Fact]
    public void Escape_Gives_The_Keys_Back_And_A_Tap_Runs_A_Listed_Command()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, Display);
        Type(ui, "fra");
        var listed = ui.ItemRectangle(DesktopUi.CommandListItem(DesktopUi.FrameLabel)) ?? throw new InvalidOperationException("No Frame in the list.");
        var (x, y) = ((listed.X + listed.Z) / 2, (listed.Y + listed.W) / 2);
        Frame(ui, Display, null, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame(ui, Display, null, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 0));
        Frame(ui, Display, null, new PointerSample(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 0));
        Frame(ui, Display);
        Assert.Contains(ui.TakeActions(), action => action is FrameAction);

        Type(ui, "hid", null, InputKey.Escape);
        Frame(ui, Display);
        Assert.Contains(ui.Route([Key(InputKey.H)]), sample => sample is KeySample { Key: InputKey.H });
    }

    // Item 1: a portrait display has no command line.
    [Fact]
    public void A_Portrait_Display_Has_No_Command_Line()
    {
        using var ui = new DesktopUi(string.Empty, Engine.Contracts.Document.Millimetre);
        Frame(ui, new Vector2(390, 844));
        Frame(ui, new Vector2(390, 844));
        Assert.Null(ui.ItemRectangle(DesktopUi.CommandItem));
    }

    private static PointerSample Mouse(Vector2 at, PointerPhase phase)
        => new(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, phase == PointerPhase.Move ? PointerButtons.None : PointerButtons.Left, KeyModifiers.None, phase, 0);

    // Finding V152 of the codebase review of 2026-11-09 (TASK-0324): with a live sketch and no selection, the window of the
    // live sketches is under the field, inside the display and above the row of the anchors, and a click on the field and a
    // text list the commands, as in the probe of the review.
    [Fact]
    public void The_Window_Of_The_Live_Sketches_Leaves_The_Command_Line_Free()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], null, SketchToolKind.Line, false, [], null);
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], null, null, sketchPanel: panel);
        }

        Step();
        Step();
        var field = ui.ItemRectangle(DesktopUi.CommandItem) ?? throw new InvalidOperationException("No command line.");
        var window = ui.SketchWindowRectangle ?? throw new InvalidOperationException("No window of the live sketches.");
        Assert.True(window.Y >= field.W, $"The window of the live sketches at {window} covers the command line at {field}.");
        Assert.True(window.Z <= Display.X - 16f + 0.5f && window.W <= Display.Y - 16f - DesktopUi.AnchorRow + 0.5f, $"The window at {window} is outside its room in {Display}.");

        var centre = new Vector2((field.X + field.Z) / 2, (field.Y + field.W) / 2);
        Step(Mouse(centre, PointerPhase.Move));
        Step(Mouse(centre, PointerPhase.Down));
        Step(Mouse(centre, PointerPhase.Up));
        Step();
        Step(new TextSample("fra", 0));
        Step();
        Assert.NotNull(ui.ItemRectangle(DesktopUi.CommandListItem(DesktopUi.FrameLabel)));
    }

    // Finding V153 of the codebase review of 2026-11-09 (TASK-0324): two bodies with no name and of one kind give two commands
    // of a cut, each with the text of the row of its body, as the list of a cut in the rows.
    [Fact]
    public void Each_Command_Of_A_Cut_Names_Its_Body_As_Its_Row()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var first = new LiveBody(Guid.Parse("53000001-0000-0000-0000-000000000000"), "Box", null, true);
        var second = new LiveBody(Guid.Parse("53000002-0000-0000-0000-000000000000"), "Box", null, true);

        var cuts = ui.Commands([Extrude, first, second], Extrude.Id).Select(command => command.Name).Where(name => name.StartsWith(DesktopUi.CutLabel, StringComparison.Ordinal)).ToList();

        Assert.Equal([$"{DesktopUi.CutLabel} {DesktopUi.RowText(first)}", $"{DesktopUi.CutLabel} {DesktopUi.RowText(second)}"], cuts);
        Assert.NotEqual(cuts[0], cuts[1]);
    }

    // Finding V153 (TASK-0324): each listed command has an identifier of its own, so a tap on the second of two equal names,
    // for two bodies with one name, cuts with the second body.
    [Fact]
    public void A_Tap_On_The_Second_Of_Two_Equal_Names_Runs_It()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var first = new LiveBody(Guid.Parse("53000000-0000-0000-0000-000000000003"), "Box", "Plate", true);
        var second = new LiveBody(Guid.Parse("53000000-0000-0000-0000-000000000004"), "Box", "Plate", true);
        void Step(params InputSample[] samples)
        {
            ui.Route(samples);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude, first, second], Extrude.Id);
        }

        Step();
        Step(Key(InputKey.K, KeyModifiers.Control));
        Step();
        Step();
        Step();
        Step(new TextSample("cut with", 0));
        Step();
        ui.TakeActions();

        // The item of a name holds the rectangle of its last entry: the entry of the second body.
        var listed = ui.ItemRectangle(DesktopUi.CommandListItem($"{DesktopUi.CutLabel} Plate")) ?? throw new InvalidOperationException("No cut in the list.");
        var field = ui.ItemRectangle(DesktopUi.CommandItem)!.Value;
        Assert.True(listed.Y >= field.W + DesktopUi.MinimumControlHeight - 0.5f, $"The entry at {listed} is not the second one.");
        var centre = new Vector2((listed.X + listed.Z) / 2, (listed.Y + listed.W) / 2);
        Step(Mouse(centre, PointerPhase.Move));
        Step(Mouse(centre, PointerPhase.Down));
        Step(Mouse(centre, PointerPhase.Up));
        Step();

        Assert.Equal([new CutBodyAction(Extrude.Id, second.Id)], ui.TakeActions().OfType<CutBodyAction>());
    }

    // Finding V157 of the codebase review of 2026-11-09 (TASK-0324): Control and K on a portrait display, or on the home
    // screen, leaves no request of the focus, so after a turn of the display or the close of the home screen the key H
    // reaches the scene.
    [Fact]
    public void Control_K_Where_The_Field_Does_Not_Show_Leaves_No_Request()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        var portrait = new Vector2(390, 844);
        Frame(ui, portrait);
        Frame(ui, portrait, null, Key(InputKey.K, KeyModifiers.Control));
        Frame(ui, portrait);
        for (var frame = 0; frame < 4; frame++)
            Frame(ui, Display);
        Assert.Contains(ui.Route([Key(InputKey.H)]), sample => sample is KeySample { Key: InputKey.H });
        Frame(ui, Display, null, Up(InputKey.H));

        ui.ShowHome([]);
        Frame(ui, Display);
        Frame(ui, Display, null, Key(InputKey.K, KeyModifiers.Control));
        Frame(ui, Display);
        ui.ShowHome(null);
        for (var frame = 0; frame < 4; frame++)
            Frame(ui, Display);
        Assert.Contains(ui.Route([Key(InputKey.H)]), sample => sample is KeySample { Key: InputKey.H });
    }
}
