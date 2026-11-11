using System.Numerics;
using Engine.Contracts;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// ADR-0116 (phase R20a, TASK-0341): only the icon and the label of a segment of a body ring take a tap, the rest of the band
// is the scene, and the ring and the history arc fade while the view turns, pans or zooms. ADR-0116 names this file in its
// enforced-by field. Problem 1 of the UX test of 2026-11-10: with "Fond" selected, a click on the module under the band
// opened the pad of Move of "Fond", and no click selected the module.
[Collection(DearImGui.Collection)]
public sealed class RingTapTests
{
    private static readonly Vector2 Display = new(1280, 720);
    private static readonly Vector2 Centre = new(700, 360);
    private static readonly Guid Body = Guid.Parse("65000000-0000-0000-0000-000000000000");
    private static readonly LiveBody Extrude = new(Body, "Extrude", "Fond", true);

    private static IReadOnlyList<InputSample> Step(DesktopUi ui, Vector2 display, params InputSample[] frame)
    {
        var scene = ui.Route(frame);
        ui.Frame(1f / 60f, display, Vector2.One, [Extrude], Body, null, ringCentre: display.X >= display.Y ? Centre : new Vector2(display.X / 2f, display.Y / 2f), ringBody: 120f);
        return scene;
    }

    private static PointerSample Pointer(DeviceKind kind, Vector2 at, PointerPhase phase, ulong ms)
    {
        var (pointer, buttons) = kind == DeviceKind.Mouse ? (0UL, PointerButtons.Left) : (3UL, PointerButtons.None);
        return new PointerSample(kind, 1, pointer, at.X, at.Y, 1, buttons, KeyModifiers.None, phase, ms * 1_000_000);
    }

    private static Vector2 Mid(Vector4 box) => new((box.X + box.Z) / 2f, (box.Y + box.W) / 2f);

    private static bool Inside(Vector4 box, Vector2 at) => at.X >= box.X && at.X < box.Z && at.Y >= box.Y && at.Y < box.W;

    // A point of the band of a segment that is not on the target of its tap: near the outer edge and near one side.
    private static Vector2 OffTarget(DesktopUi ui, string label)
    {
        var ring = ui.RingLayoutOfFrame ?? throw new InvalidOperationException("No ring.");
        var segment = ring.Segments(ui.RingLabels).Single(entry => entry.Label == label);
        var target = ui.RingTarget(label) ?? throw new InvalidOperationException($"No target of {label}.");
        foreach (var radius in new[] { ring.Outer - 3f, ring.Inner + 3f })
        {
            foreach (var angle in new[] { segment.From + 0.02f, segment.To - 0.02f })
            {
                var at = ring.Centre + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
                if (ring.At(ui.RingLabels, at) == label && !Inside(target, at))
                    return at;
            }
        }

        throw new InvalidOperationException($"The target of {label} fills its segment.");
    }

    // Items 1 and 3: a tap on the band of Move of the selected body, off its icon and its label, goes to the scene: the library
    // does not take it, the intent machine gives a select at that point, and no pad opens. A tap on the icon or on the label of
    // Move still opens the pad of Move. Until TASK-0341 the whole band took the tap and opened the pad.
    [Theory]
    [InlineData(DeviceKind.Touch)]
    [InlineData(DeviceKind.Mouse)]
    public void A_Tap_On_The_Band_Off_The_Label_Goes_To_The_Scene(DeviceKind kind)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Display);
        Step(ui, Display);

        var under = OffTarget(ui, DesktopUi.MoveLabel);
        Assert.False(ui.OverPanel(under.X, under.Y), $"The band of Move at {under} takes the tap.");
        var machine = new IntentMachine();
        var intents = new List<Intent>();
        intents.AddRange(machine.Step(Step(ui, Display, Pointer(kind, under, PointerPhase.Down, 0))));
        intents.AddRange(machine.Step(Step(ui, Display, Pointer(kind, under, PointerPhase.Up, 50))));
        Step(ui, Display);
        Assert.Null(ui.Pad);
        Assert.Empty(ui.TakeActions());
        Assert.Contains(intents, intent => intent is SelectIntent select && Math.Abs(select.X - under.X) < 0.5 && Math.Abs(select.Y - under.Y) < 0.5);

        foreach (var item in new[] { DesktopUi.RingIconItem(DesktopUi.MoveLabel), DesktopUi.RingTextItem(DesktopUi.MoveLabel) })
        {
            using var other = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
            Step(other, Display);
            Step(other, Display);
            var at = Mid(other.ItemRectangle(item) ?? throw new InvalidOperationException($"No item {item}."));
            Assert.True(other.OverPanel(at.X, at.Y), $"The item {item} does not take the tap.");
            Step(other, Display, Pointer(kind, at, PointerPhase.Down, 0));
            Step(other, Display, Pointer(kind, at, PointerPhase.Up, 50));
            Step(other, Display);
            Assert.Equal(DesktopUi.MoveLabel, other.Pad?.Title);
        }
    }

    // Item 1: a release on the band off the label after a press on the label gives no tool, as a release on another segment.
    [Fact]
    public void A_Press_On_The_Label_And_A_Release_Off_It_Gives_No_Tool()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Display);
        Step(ui, Display);
        var hide = Mid(ui.ItemRectangle(DesktopUi.RingTextItem(DesktopUi.HideLabel)) ?? throw new InvalidOperationException("No Hide."));
        var off = OffTarget(ui, DesktopUi.HideLabel);

        Step(ui, Display, Pointer(DeviceKind.Mouse, hide, PointerPhase.Down, 0));
        Step(ui, Display, Pointer(DeviceKind.Mouse, off, PointerPhase.Move, 20));
        Step(ui, Display, Pointer(DeviceKind.Mouse, off, PointerPhase.Up, 40));
        Step(ui, Display);

        Assert.DoesNotContain(ui.TakeActions(), action => action is SetVisibleAction);
    }

    // Item 4: on a landscape display, on a small window and on the phone, the target of each segment of a body ring is at least
    // 44 by 44 pixels, holds the icon and the label, and its centre is on its segment.
    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1440, 900)]
    [InlineData(640, 360)]
    [InlineData(390, 844)]
    [InlineData(360, 780)]
    public void Each_Target_Is_Large_Enough_For_A_Finger_And_On_Its_Segment(int width, int height)
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var display = new Vector2(width, height);
        Step(ui, display);
        Step(ui, display);

        var ring = ui.RingLayoutOfFrame ?? throw new InvalidOperationException("No ring.");
        Assert.NotEmpty(ui.RingLabels);
        foreach (var label in ui.RingLabels)
        {
            var target = ui.RingTarget(label) ?? throw new InvalidOperationException($"No target of {label}.");
            Assert.True(target.Z - target.X >= DesktopUi.ValueSize - 0.01f && target.W - target.Y >= DesktopUi.ValueSize - 0.01f, $"The target {target} of {label} at {width} x {height} is smaller than 44 by 44.");
            var text = ui.ItemRectangle(DesktopUi.RingTextItem(label)) ?? throw new InvalidOperationException($"No label {label}.");
            Assert.True(text.X >= target.X && text.Z <= target.Z && text.Y >= target.Y && text.W <= target.W, $"The label {text} of {label} is outside its target {target}.");
            if (ui.ItemRectangle(DesktopUi.RingIconItem(label)) is { } icon)
                Assert.True(icon.X >= target.X && icon.Z <= target.Z && icon.Y >= target.Y && icon.W <= target.W, $"The icon {icon} of {label} is outside its target {target}.");
            Assert.Equal(label, ring.At(ui.RingLabels, Mid(target)));
            Assert.True(ui.OverPanel(Mid(target).X, Mid(target).Y), $"The centre of the target of {label} at {width} x {height} does not take the tap.");
        }
    }

    // Item 3: the ring of an active sketch keeps its whole band as the target of a tap (ADR-0086), so it has no target.
    [Fact]
    public void The_Ring_Of_A_Sketch_Keeps_Its_Band()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        var sketch = new Engine.Core.Queries.SketchInfo(Guid.NewGuid(), Engine.Contracts.Sketch.Datums.XY.Id, "Plate", [], [], [], [], [], true, 0);
        var panel = new SketchPanel([sketch], sketch, ThreeDEngine.Core.Sketching.SketchToolKind.Line, false, [], null);
        for (var i = 0; i < 2; i++)
        {
            ui.Route([]);
            ui.Frame(1f / 60f, Display, Vector2.One, [Extrude], Body, null, sketchPanel: panel, ringCentre: Centre);
        }

        var ring = ui.RingLayoutOfFrame ?? throw new InvalidOperationException("No ring.");
        var segment = ring.Segments(ui.RingLabels)[0];
        var edge = ring.Centre + (new Vector2(MathF.Cos(segment.From + 0.02f), MathF.Sin(segment.From + 0.02f)) * (ring.Outer - 3f));
        Assert.Null(ui.RingTarget(segment.Label));
        Assert.True(ui.OverPanel(edge.X, edge.Y), "The band of a sketch ring does not take the tap.");
    }

    // Item 2: while the view turns, pans or zooms, the ring and the history arc are drawn with the alpha of the fade, under the
    // dim of a waiting proposal too; after the gesture they come back.
    [Fact]
    public void The_Ring_And_The_History_Arc_Fade_During_A_Gesture()
    {
        using var ui = new DesktopUi("http://127.0.0.1:5180", Document.Millimetre);
        Step(ui, Display);
        Assert.Equal((1f, 1f), (ui.RingAlphaOfFrame, ui.HistoryAlphaOfFrame));

        ui.CameraGesture = true;
        Step(ui, Display);
        Assert.Equal((DesktopUi.GestureFade, DesktopUi.GestureFade), (ui.RingAlphaOfFrame, ui.HistoryAlphaOfFrame));
        Assert.True(DesktopUi.GestureFade < DesktopUi.ProposalDim);

        ui.CameraGesture = false;
        Step(ui, Display);
        Assert.Equal((1f, 1f), (ui.RingAlphaOfFrame, ui.HistoryAlphaOfFrame));
    }

    // Item 2: an orbit, a pan or a zoom starts the gesture; it runs while such intents come and for the hold after the last one;
    // a select does not start it.
    [Fact]
    public void A_Gesture_Runs_Until_The_Hold_After_Its_Last_Intent()
    {
        var timer = new GestureFadeTimer();
        timer.Step([new SelectIntent(1, 1)], 1d / 60);
        Assert.False(timer.Running);

        foreach (Intent intent in new Intent[] { new OrbitIntent(1, 0), new PanIntent(2, 0), new ZoomIntent(1.1) })
        {
            timer.Step([intent], 1d / 60);
            Assert.True(timer.Running, $"{intent} does not start the gesture.");
            timer.Step([], GestureFadeTimer.Hold / 2);
            Assert.True(timer.Running, $"The gesture of {intent} ends before the hold.");
            timer.Step([], GestureFadeTimer.Hold);
            Assert.False(timer.Running, $"The gesture of {intent} runs after the hold.");
        }
    }
}
