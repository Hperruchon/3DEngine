using ThreeDEngine.Core.Input;
using ThreeDEngine.Desktop.Ui;

namespace ThreeDEngine.ImageTests;

// The routing of the samples between the panels and the intents (ADR-0027 item 4,
// TASK-0084), with no library and no GPU. These tests live here because only this verifier
// may reference the host (ADR-0027 item 8). They run on each runner.
public class UiInputRouterTests
{
    // A panel that covers the left half of a display of 1000 x 1000.
    private static bool OverPanel(double x, double y) => x < 500;

    private static PointerSample Mouse(double x, double y, PointerButtons buttons, PointerPhase phase)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, buttons, KeyModifiers.None, phase, 0);

    private static PointerSample Finger(ulong finger, double x, double y, PointerPhase phase)
        => new(DeviceKind.Touch, 9, finger, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, 0);

    [Fact]
    public void A_Gesture_That_Starts_Over_A_Panel_Stays_With_The_Panel_When_It_Leaves_It()
    {
        var router = new UiInputRouter();

        var (panel, intents) = router.Route(
        [
            Mouse(100, 100, PointerButtons.Left, PointerPhase.Down),
            Mouse(700, 100, PointerButtons.Left, PointerPhase.Move),
            Mouse(700, 100, PointerButtons.Left, PointerPhase.Up),
        ], OverPanel, textActive: false);

        Assert.Equal(3, panel.Count);
        Assert.Empty(intents);
    }

    [Fact]
    public void A_Gesture_That_Starts_Outside_The_Panels_Stays_With_The_Intents_When_It_Enters_One()
    {
        var router = new UiInputRouter();

        var (panel, intents) = router.Route(
        [
            Finger(4, 700, 100, PointerPhase.Down),
            Finger(4, 100, 100, PointerPhase.Move),
            Finger(4, 100, 100, PointerPhase.Up),
        ], OverPanel, textActive: false);

        Assert.Empty(panel);
        Assert.Equal(3, intents.Count);
    }

    // A mouse with no button down shows a hover in the panel; the intents ignore it.
    [Fact]
    public void A_Hover_Of_The_Mouse_Goes_To_Both()
    {
        var router = new UiInputRouter();

        var (panel, intents) = router.Route([Mouse(100, 100, PointerButtons.None, PointerPhase.Move)], OverPanel, textActive: false);

        Assert.Single(panel);
        Assert.Single(intents);
    }

    [Fact]
    public void The_Wheel_Follows_The_Last_Place_Of_The_Mouse()
    {
        var router = new UiInputRouter();

        var over = router.Route([Mouse(100, 100, PointerButtons.None, PointerPhase.Move), new WheelSample(1, 0)], OverPanel, false);
        var outside = router.Route([Mouse(700, 100, PointerButtons.None, PointerPhase.Move), new WheelSample(1, 0)], OverPanel, false);

        Assert.Contains(over.Panel, sample => sample is WheelSample);
        Assert.DoesNotContain(over.Intents, sample => sample is WheelSample);
        Assert.Contains(outside.Intents, sample => sample is WheelSample);
        Assert.DoesNotContain(outside.Panel, sample => sample is WheelSample);
    }

    // While a text field is active, a key and the text go to the panel, so the key plus of a
    // typed number does not zoom. Else a key moves the camera and text goes nowhere.
    [Fact]
    public void Keys_And_Text_Go_To_The_Panel_Only_While_A_Text_Field_Is_Active()
    {
        var router = new UiInputRouter();
        InputSample[] frame = [new KeySample(InputKey.Plus, true, KeyModifiers.None, 0), new TextSample("+", 0)];

        var typing = router.Route(frame, OverPanel, textActive: true);
        var moving = router.Route(frame, OverPanel, textActive: false);

        Assert.Equal(2, typing.Panel.Count);
        Assert.Empty(typing.Intents);
        Assert.Empty(moving.Panel);
        Assert.IsType<KeySample>(Assert.Single(moving.Intents));
    }

    // A lost focus ends each gesture: the next move of that finger belongs to nobody.
    [Fact]
    public void A_Lost_Focus_Goes_To_Both_And_Ends_Each_Gesture()
    {
        var router = new UiInputRouter();
        router.Route([Finger(4, 100, 100, PointerPhase.Down)], OverPanel, false);

        var lost = router.Route([new FocusLostSample(0)], OverPanel, false);
        var after = router.Route([Finger(4, 120, 100, PointerPhase.Move)], OverPanel, false);

        Assert.Single(lost.Panel);
        Assert.Single(lost.Intents);
        Assert.Empty(after.Panel);
        Assert.Single(after.Intents);
    }
}
