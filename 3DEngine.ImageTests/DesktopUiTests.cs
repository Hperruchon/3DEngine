using System.Numerics;
using ThreeDEngine.Core.Input;
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
    private static readonly Vector2 Display = new(1280, 720);

    private static readonly (Guid Id, string Kind)[] Bodies =
    [
        (Guid.Parse("11111111-0000-0000-0000-000000000000"), "Box"),
        (Guid.Parse("22222222-0000-0000-0000-000000000000"), "Solid"),
    ];

    private static IReadOnlyList<InputSample> Step(DesktopUi ui, params InputSample[] frame)
    {
        var intents = ui.Route(frame);
        ui.Frame(1f / 60f, Display, Vector2.One, Bodies);
        return intents;
    }

    // Two frames, so that the panel has its layout and its rectangles.
    private static DesktopUi Opened()
    {
        var ui = new DesktopUi("http://127.0.0.1:5180");
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

    private static void Tap(DesktopUi ui, DeviceKind kind, (double X, double Y) at)
    {
        Step(ui, Pointer(kind, at, PointerPhase.Down));
        Step(ui, Pointer(kind, at, PointerPhase.Up));
        Step(ui);
        Step(ui);
    }

    private static void Type(DesktopUi ui, string text)
    {
        Tap(ui, DeviceKind.Mouse, Centre(ui, DesktopUi.FilterLabel));
        Step(ui, new TextSample(text, 0));
        Step(ui);
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

    [Fact]
    public void The_Font_Atlas_Is_Rgba_With_White_Glyphs()
    {
        using var ui = Opened();

        var (width, height, rgba) = ui.FontAtlas();

        Assert.Equal((int)(width * height * 4), rgba.Length);
        Assert.Contains(Enumerable.Range(0, rgba.Length / 4), pixel => rgba[pixel * 4 + 3] == 255 && rgba[pixel * 4] == 255);
    }
}
