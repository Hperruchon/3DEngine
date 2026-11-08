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
}
