using ThreeDEngine.Core.Input;

namespace ThreeDEngine.Tests;

// The parity of touch and mouse (ADR-0026 item 5, objective 9 of docs/CHARTER.md,
// TASK-0082): for each kind of intent, a script of touch samples and a script of mouse
// samples on the same path give the same intents, and a kind of intent with no touch
// script fails the coverage check. No computer of the project has a touch screen, so the
// touch scripts are synthetic (decision D1). ADR-0026 names this file in enforced-by.
public class IntentParityTests
{
    private const ulong Ms = 1_000_000;

    private static PointerSample Touch(ulong finger, double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Touch, 1, finger, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, ms * Ms);

    private static PointerSample Mouse(double x, double y, PointerButtons buttons, PointerPhase phase, ulong ms, KeyModifiers modifiers = KeyModifiers.None)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, buttons, modifiers, phase, ms * Ms);

    // Each script is a list of frames, and each frame a list of samples.
    private static readonly Dictionary<Type, (InputSample[][] Touch, InputSample[][] Mouse)> Scripts = new()
    {
        [typeof(OrbitIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 110, 100, PointerPhase.Move, 10)], [Touch(1, 130, 105, PointerPhase.Move, 20)], [Touch(1, 130, 105, PointerPhase.Up, 400)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(110, 100, PointerButtons.Left, PointerPhase.Move, 10)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Move, 20)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Up, 400)]]),
        [typeof(PanIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0)], [Touch(1, 110, 110, PointerPhase.Move, 10), Touch(2, 210, 110, PointerPhase.Move, 10)], [Touch(1, 110, 110, PointerPhase.Up, 400), Touch(2, 210, 110, PointerPhase.Up, 400)]],
            [[Mouse(150, 100, PointerButtons.Middle, PointerPhase.Down, 0)], [Mouse(160, 110, PointerButtons.Middle, PointerPhase.Move, 10)], [Mouse(160, 110, PointerButtons.Middle, PointerPhase.Up, 400)]]),
        [typeof(ZoomIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0)], [Touch(1, 100 - 50.0 / 9, 100, PointerPhase.Move, 10), Touch(2, 200 + 50.0 / 9, 100, PointerPhase.Move, 10)]],
            [[new WheelSample(1, 10 * Ms)]]),
        [typeof(SelectIntent)] = (
            [[Touch(1, 120, 80, PointerPhase.Down, 0)], [Touch(1, 120, 80, PointerPhase.Up, 100)]],
            [[Mouse(120, 80, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(120, 80, PointerButtons.Left, PointerPhase.Up, 100)]]),
        [typeof(CancelIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 120, 100, PointerPhase.Move, 10)], [Touch(1, 120, 100, PointerPhase.Cancel, 20)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(120, 100, PointerButtons.Left, PointerPhase.Move, 10)], [new FocusLostSample(20 * Ms)]]),
    };

    private static List<Intent> Run(InputSample[][] script, IntentSettings? settings = null)
    {
        var machine = new IntentMachine(settings);
        return [.. script.SelectMany(frame => machine.Step(frame))];
    }

    [Theory]
    [InlineData(nameof(OrbitIntent))]
    [InlineData(nameof(PanIntent))]
    [InlineData(nameof(ZoomIntent))]
    [InlineData(nameof(SelectIntent))]
    [InlineData(nameof(CancelIntent))]
    public void A_Touch_Script_And_A_Mouse_Script_Give_The_Same_Intents(string kind)
    {
        var (touch, mouse) = Scripts.Single(script => script.Key.Name == kind).Value;

        var fromTouch = Run(touch);
        var fromMouse = Run(mouse);

        Assert.Contains(fromTouch, intent => intent.GetType().Name == kind);
        Assert.Equal(fromMouse.Count, fromTouch.Count);
        foreach (var (a, b) in fromTouch.Zip(fromMouse))
            Assert.True(Same(a, b), $"touch gave {a}, and mouse gave {b}");
    }

    // A new kind of intent with no touch script, or no mouse script, fails here, so
    // objective 9 is held by a test and not by text.
    [Fact]
    public void Each_Kind_Of_Intent_Has_A_Touch_Script_And_A_Mouse_Script()
    {
        var kinds = typeof(Intent).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Intent)) && !type.IsAbstract).ToList();

        Assert.NotEmpty(kinds);
        Assert.All(kinds, kind => Assert.True(
            Scripts.TryGetValue(kind, out var script) && script.Touch.Length > 0 && script.Mouse.Length > 0,
            $"The kind of intent {kind.Name} has no touch script and no mouse script in IntentParityTests."));
    }

    // Decision D6: the keyboard alone moves the camera.
    [Fact]
    public void The_Keyboard_Alone_Orbits_Pans_And_Zooms()
    {
        var intents = Run([[
            new KeySample(InputKey.ArrowLeft, true, KeyModifiers.None, 0),
            new KeySample(InputKey.ArrowUp, true, KeyModifiers.Shift, 0),
            new KeySample(InputKey.Plus, true, KeyModifiers.None, 0),
            new KeySample(InputKey.Minus, false, KeyModifiers.None, 0),
        ]]);

        Assert.Collection(
            intents,
            intent => Assert.True(Same(new OrbitIntent(4.8, 0), intent), $"the arrow gave {intent}"),
            intent => Assert.Equal(new PanIntent(0, -16), intent),
            intent => Assert.Equal(new ZoomIntent(0.9), intent));
    }

    // Decision D2: a pen takes the path of one finger.
    [Fact]
    public void A_Pen_Orbits_As_One_Finger()
    {
        static PointerSample Pen(double x, PointerPhase phase, ulong ms)
            => new(DeviceKind.Pen, 7, 3, x, 100, 0.5f, PointerButtons.None, KeyModifiers.None, phase, ms * Ms);

        var fromPen = Run([[Pen(100, PointerPhase.Down, 0)], [Pen(110, PointerPhase.Move, 10)], [Pen(110, PointerPhase.Up, 400)]]);
        var fromTouch = Run([[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 110, 100, PointerPhase.Move, 10)], [Touch(1, 110, 100, PointerPhase.Up, 400)]]);

        Assert.Equal(fromTouch, fromPen);
    }

    // A lost pointer ends the gesture, and nothing stays down after it.
    [Fact]
    public void A_Lost_Release_Cancels_And_Leaves_No_Gesture()
    {
        var machine = new IntentMachine();
        machine.Step([Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)]);
        machine.Step([Mouse(120, 100, PointerButtons.Left, PointerPhase.Move, 10)]);

        Assert.Equal([new CancelIntent()], machine.Step([new FocusLostSample(20 * Ms)]));
        Assert.Empty(machine.Step([Mouse(140, 100, PointerButtons.None, PointerPhase.Move, 30)]));
        Assert.Empty(machine.Step([Touch(1, 140, 100, PointerPhase.Move, 40)]));
    }

    // Decision D4: the action of a gesture is a setting.
    [Fact]
    public void A_Setting_Moves_An_Action_To_Another_Gesture()
    {
        var settings = IntentSettings.Default with
        {
            Map = new Dictionary<Gesture, IntentAction>(IntentSettings.Default.Map) { [Gesture.LeftDrag] = IntentAction.Pan, [Gesture.MiddleDrag] = IntentAction.Orbit },
        };
        var (_, leftDrag) = Scripts[typeof(OrbitIntent)];

        Assert.All(Run(leftDrag, settings), intent => Assert.IsType<PanIntent>(intent));
        Assert.All(Run(Scripts[typeof(PanIntent)].Mouse, settings), intent => Assert.IsType<OrbitIntent>(intent));
    }

    [Fact]
    public void A_Setting_Refuses_An_Action_That_A_Gesture_Cannot_Give()
    {
        var settings = IntentSettings.Default with
        {
            Map = new Dictionary<Gesture, IntentAction>(IntentSettings.Default.Map) { [Gesture.Tap] = IntentAction.Orbit },
        };

        Assert.Throws<ArgumentException>(() => new IntentMachine(settings));
    }

    // Two fingers that move together in one frame pan and do not pinch.
    [Fact]
    public void Two_Fingers_That_Move_Together_Do_Not_Pinch()
    {
        var intents = Run(Scripts[typeof(PanIntent)].Touch);

        Assert.All(intents, intent => Assert.IsType<PanIntent>(intent));
    }

    private static bool Same(Intent a, Intent b) => (a, b) switch
    {
        (OrbitIntent x, OrbitIntent y) => Near(x.YawDegrees, y.YawDegrees) && Near(x.PitchDegrees, y.PitchDegrees),
        (PanIntent x, PanIntent y) => Near(x.DxPixels, y.DxPixels) && Near(x.DyPixels, y.DyPixels),
        (ZoomIntent x, ZoomIntent y) => Near(x.Factor, y.Factor),
        (SelectIntent x, SelectIntent y) => Near(x.X, y.X) && Near(x.Y, y.Y),
        (CancelIntent, CancelIntent) => true,
        _ => false,
    };

    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;
}
