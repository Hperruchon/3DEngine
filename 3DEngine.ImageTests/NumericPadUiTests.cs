using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The pad in the panel of the desktop (ADR-0032 items 3 to 6, TASK-0094), with no GPU: the
// buttons that open it, a tap and a click on its buttons, the keyboard that gives the same
// states, the keys that it takes while it is open, and the size of its controls.
[Collection(DearImGui.Collection)]
public sealed class NumericPadUiTests
{
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly LiveBody Body = new(Guid.Parse("22222222-0000-0000-0000-000000000000"), "Box", null, true);

    private static IReadOnlyList<InputSample> Step(DesktopUi ui, Guid? selection, params InputSample[] frame)
    {
        var intents = ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, [Body], selection);
        return intents;
    }

    private static DesktopUi Opened(Guid? selection = null)
    {
        var ui = new DesktopUi("http://127.0.0.1:5180");
        Step(ui, selection);
        Step(ui, selection);
        return ui;
    }

    private static (double X, double Y) Centre(DesktopUi ui, string item)
    {
        var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The panel has no control {item}.");
        return ((rectangle.X + rectangle.Z) / 2, (rectangle.Y + rectangle.W) / 2);
    }

    private static void Tap(DesktopUi ui, DeviceKind kind, string item, Guid? selection = null)
    {
        var at = Centre(ui, item);
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        Step(ui, selection, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Down, 0));
        Step(ui, selection, new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, PointerPhase.Up, 0));
        Step(ui, selection);
        Step(ui, selection);
    }

    private static DesktopUi WithNewBoxPad()
    {
        var ui = Opened();
        Tap(ui, DeviceKind.Mouse, DesktopUi.NewBoxLabel);
        Assert.NotNull(ui.Pad);
        Step(ui, null);
        return ui;
    }

    [Fact]
    public void New_Box_Opens_The_Pad_And_An_Apply_Gives_The_Typed_Sizes()
    {
        using var ui = WithNewBoxPad();

        foreach (var label in new[] { "Clear", "2", "5", "Next", "Clear", "2", "*", "pi", "Apply" })
            Tap(ui, DeviceKind.Touch, DesktopUi.PadItem(label));

        Assert.Equal([new CreateBoxAction(25, 2 * Math.PI, 10)], ui.TakeActions());
        Assert.NotNull(ui.Pad);
        ui.EndPad(null);
        Assert.Null(ui.Pad);
    }

    // ADR-0032 item 3: a script of buttons and a script of keys give the same states and the
    // same action.
    [Fact]
    public void The_Keyboard_Gives_The_States_And_The_Action_Of_The_Buttons()
    {
        using var buttons = WithNewBoxPad();
        foreach (var label in new[] { "Clear", "1", ".", "5", "Next", "Delete", "Delete", "sqrt(", "4", ")", "Next", "-", "3" })
            Tap(buttons, DeviceKind.Mouse, DesktopUi.PadItem(label));

        using var keys = WithNewBoxPad();
        Step(keys, null, new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0), new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0));
        Step(keys, null, new TextSample("1.5", 0), new KeySample(InputKey.Tab, true, KeyModifiers.None, 0));
        Step(keys, null, new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0), new KeySample(InputKey.Backspace, true, KeyModifiers.None, 0));
        Step(keys, null, new TextSample("sqrt(4)", 0), new KeySample(InputKey.Tab, true, KeyModifiers.None, 0), new TextSample("-3", 0));

        Assert.Equal(buttons.Pad!.Fields, keys.Pad!.Fields);
        Assert.Equal(buttons.Pad.Active, keys.Pad.Active);

        Tap(buttons, DeviceKind.Touch, DesktopUi.PadItem("Apply"));
        Step(keys, null, new KeySample(InputKey.Enter, true, KeyModifiers.None, 0));
        var action = Assert.Single(buttons.TakeActions());
        Assert.Equal(new CreateBoxAction(1.5, 2, 10 - 3), action);
        Assert.Equal([action], keys.TakeActions());
    }

    // ADR-0032 item 6: while the pad is open, no key and no text reaches the scene; Escape
    // closes it, and then the keys reach the scene again.
    [Fact]
    public void The_Pad_Takes_The_Keys_While_It_Is_Open()
    {
        using var ui = WithNewBoxPad();
        var arrow = new KeySample(InputKey.ArrowLeft, true, KeyModifiers.None, 0);
        var undo = new KeySample(InputKey.Z, true, KeyModifiers.Control, 0);

        Assert.Empty(Step(ui, null, arrow, undo, new TextSample("h", 0)));
        Step(ui, null, new KeySample(InputKey.Escape, true, KeyModifiers.None, 0));

        Assert.Null(ui.Pad);
        Assert.Equal([arrow, undo], Step(ui, null, arrow, undo));
    }

    [Fact]
    public void Move_Shows_Only_With_A_Selection_And_Gives_The_Offsets_Of_The_Selected_Body()
    {
        using var none = Opened();
        Assert.Null(none.ItemRectangle(DesktopUi.MoveLabel));

        using var ui = Opened(Body.Id);
        Tap(ui, DeviceKind.Touch, DesktopUi.MoveLabel, Body.Id);
        Assert.Equal(["dx", "dy", "dz"], ui.Pad!.Fields.Select(field => field.Name));
        Step(ui, Body.Id);
        foreach (var label in new[] { "Clear", "5", "Next", "Next", "Clear", "-", "2", "Apply" })
            Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem(label), Body.Id);

        Assert.Equal([new MoveBodyAction(Body.Id, 5, 0, -2)], ui.TakeActions());
    }

    // ADR-0032 item 8: a rejection of the command stays in the pad, and a bad field gives no action.
    [Fact]
    public void A_Rejection_And_A_Bad_Field_Keep_The_Pad_Open()
    {
        using var ui = WithNewBoxPad();

        ui.EndPad("E-GEOM-INVALID-PARAM: CreateBox requires sizeX, sizeY, sizeZ all strictly greater than zero.");
        Assert.StartsWith("E-GEOM-INVALID-PARAM", ui.Pad!.Message, StringComparison.Ordinal);
        // The line of the message moves the buttons down, so one frame places them first.
        Step(ui, null);

        Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem("("));
        Tap(ui, DeviceKind.Mouse, DesktopUi.PadItem("Apply"));
        Assert.Empty(ui.TakeActions());
        Assert.StartsWith("x: ", ui.Pad!.Message, StringComparison.Ordinal);
    }

    // ADR-0027 item 6 for the controls of ADR-0032 items 4 and 5.
    [Fact]
    public void Each_Control_Of_The_Pad_Is_At_Least_44_Pixels_High()
    {
        using var ui = WithNewBoxPad();

        var items = DesktopUi.PadRows.SelectMany(row => row).Select(DesktopUi.PadItem)
            .Concat(new[] { "x", "y", "z" }.Select(DesktopUi.PadItem))
            .Append(DesktopUi.NewBoxLabel);
        foreach (var item in items)
        {
            var rectangle = ui.ItemRectangle(item) ?? throw new InvalidOperationException($"The pad has no control {item}.");
            Assert.True(rectangle.W - rectangle.Y >= DesktopUi.MinimumControlHeight, $"{item} is {rectangle.W - rectangle.Y} pixels high.");
        }
    }
}
