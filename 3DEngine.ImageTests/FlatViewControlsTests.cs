using System.Numerics;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0138 (phase R21b, TASK-0370): the button "Views" opens the net of the flat views; a tap on a view gives it and closes
// the net, and the keys 1, 3, 7 and 0 give the views. With no GPU. ADR-0138 names this file in its enforced-by field.
[Collection(DearImGui.Collection)]
public sealed class FlatViewControlsTests
{
    private static readonly LiveBody Body = new(Guid.Parse("7a000000-0000-0000-0000-000000000001"), "Box", "Block", true);

    private static void Frame(DesktopUi ui, Vector2 display, Guid? selection, params InputSample[] samples)
    {
        ui.Route(samples);
        ui.Frame(1f / 60f, display, Vector2.One, [Body], selection, new DocumentLine("block", null), ringCentre: selection is null ? null : display / 2f, ringBody: 80f);
    }

    private static void Tap(DesktopUi ui, Vector2 display, Guid? selection, Vector4 target)
    {
        var at = new Vector2((target.X + target.Z) / 2f, (target.Y + target.W) / 2f);
        Frame(ui, display, selection, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0));
        Frame(ui, display, selection, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Down, 50_000_000));
        Frame(ui, display, selection, new PointerSample(DeviceKind.Mouse, 1, 0, at.X, at.Y, 1, PointerButtons.Left, KeyModifiers.None, PointerPhase.Up, 100_000_000));
        Frame(ui, display, selection);
        Frame(ui, display, selection);
    }

    private static Vector4 Item(DesktopUi ui, string item) => ui.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}.");

    private static bool Meets(Vector4 a, Vector4 b) => a.X < b.Z && b.X < a.Z && a.Y < b.W && b.Y < a.W;

    // A tap on "Views" opens the net, with each view and "3D" inside the display and at least 44 pixels high; on a landscape
    // display the button is on the row of the anchors and the net above it, on a portrait display the button is at the upper
    // right and the net under it. A tap on a view gives its action and closes the net. The net lies on no other fixed control.
    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(1280, 720, true)]
    [InlineData(640, 360, false)]
    [InlineData(390, 844, true)]
    [InlineData(360, 640, false)]
    public void The_Button_Views_Opens_The_Net_And_A_View_Closes_It(int width, int height, bool selected)
    {
        var display = new Vector2(width, height);
        Guid? selection = selected ? Body.Id : null;
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, display, selection);
        Frame(ui, display, selection);
        Assert.Null(ui.ItemRectangle(DesktopUi.ViewItem(FlatView.Front)));

        var button = Item(ui, DesktopUi.ViewsLabel);
        var portrait = width < height;
        Assert.True(portrait ? button.Y < height / 4f && button.Z > width * 0.75f : button.Y > height * 0.75f && button.X < width / 3f, $"{width}x{height}: Views {button} is not in its place.");
        var others = ui.FixedRectangles.ToList();
        Tap(ui, display, selection, button);
        Assert.True(ui.ViewsOpen, "The net does not open.");

        var cells = DesktopUi.ViewNet.Select(cell => (cell.View, Box: Item(ui, DesktopUi.ViewItem(cell.View)))).ToList();
        Assert.Equal(7, cells.Count);
        foreach (var (view, box) in cells)
        {
            Assert.True(box.X >= 0f && box.Y >= 0f && box.Z <= width && box.W <= height, $"{width}x{height}: {view} {box} is out of the display.");
            Assert.True(box.W - box.Y >= DesktopUi.MinimumControlHeight, $"{view} {box} is lower than 44 pixels.");
            Assert.True(portrait ? box.Y > button.W : box.W < button.Y, $"{width}x{height}: {view} {box} is not on the side of the net of the button {button}.");
            Assert.DoesNotContain(others, other => Meets(other, box));
        }

        ui.TakeActions();
        Tap(ui, display, selection, cells.Single(cell => cell.View == FlatView.Front).Box);
        Assert.Equal([new FlatViewAction(FlatView.Front)], ui.TakeActions());
        Assert.False(ui.ViewsOpen, "The net stays open after a view.");
        Assert.Null(ui.ItemRectangle(DesktopUi.ViewItem(FlatView.Front)));
    }

    // The keys 1, 3 and 7 give Front, Right and Top, with Control Back, Left and Bottom, and 0 the 3D view; on the home screen
    // they give nothing.
    [Fact]
    public void The_Keys_Give_The_Views()
    {
        var display = new Vector2(1280, 720);
        using var ui = new DesktopUi("http://127.0.0.1:5180", Engine.Contracts.Document.Millimetre);
        Frame(ui, display, null);
        ui.TakeActions();
        KeySample Key(InputKey key, bool control = false) => new(key, true, control ? KeyModifiers.Control : KeyModifiers.None, 0);
        Frame(ui, display, null, Key(InputKey.Digit1), Key(InputKey.Digit3), Key(InputKey.Digit7), Key(InputKey.Digit1, true), Key(InputKey.Digit3, true), Key(InputKey.Digit7, true), Key(InputKey.Digit0));
        Assert.Equal(
            [new FlatViewAction(FlatView.Front), new FlatViewAction(FlatView.Right), new FlatViewAction(FlatView.Top), new FlatViewAction(FlatView.Back), new FlatViewAction(FlatView.Left), new FlatViewAction(FlatView.Bottom), new FlatViewAction(null)],
            ui.TakeActions().OfType<FlatViewAction>());

        ui.ShowHome(["block"]);
        Frame(ui, display, null, Key(InputKey.Digit1));
        Assert.DoesNotContain(ui.TakeActions(), action => action is FlatViewAction);
    }
}
