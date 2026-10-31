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
        // ADR-0029 item 4: a long press and a right click hide the body at the point; a tap of two
        // fingers, and Shift and H on the keyboard, show each hidden body.
        [typeof(HideIntent)] = (
            [[Touch(1, 120, 80, PointerPhase.Down, 0)], [Touch(1, 120, 80, PointerPhase.Up, 600)]],
            [[Mouse(120, 80, PointerButtons.Right, PointerPhase.Down, 0)], [Mouse(120, 80, PointerButtons.Right, PointerPhase.Up, 100)]]),
        [typeof(ShowAllIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 120), Touch(2, 200, 100, PointerPhase.Up, 120)]],
            [[new KeySample(InputKey.H, true, KeyModifiers.Shift, 10 * Ms)]]),
        // ADR-0032 item 9 (Q1 A): a tap of three fingers and Control and Z undo; a tap of four
        // fingers and Control and Y redo.
        [typeof(UndoIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0), Touch(3, 300, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 120), Touch(2, 200, 100, PointerPhase.Up, 120), Touch(3, 300, 100, PointerPhase.Up, 120)]],
            [[new KeySample(InputKey.Z, true, KeyModifiers.Control, 10 * Ms)]]),
        [typeof(RedoIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0), Touch(3, 300, 100, PointerPhase.Down, 0), Touch(4, 400, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 120), Touch(2, 200, 100, PointerPhase.Up, 120), Touch(3, 300, 100, PointerPhase.Up, 120), Touch(4, 400, 100, PointerPhase.Up, 120)]],
            [[new KeySample(InputKey.Y, true, KeyModifiers.Control, 10 * Ms)]]),
        // ADR-0034 item 1: in the mode Sketch, a tap and a click give a point of the sketch.
        [typeof(SketchPointIntent)] = (
            [[Touch(1, 120, 80, PointerPhase.Down, 0)], [Touch(1, 120, 80, PointerPhase.Up, 100)]],
            [[Mouse(120, 80, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(120, 80, PointerButtons.Left, PointerPhase.Up, 100)]]),
        // ADR-0041 item 6 (phase R11b): the mark of a snap. The mouse with no button and the first
        // contact of a finger, before the tap, give it.
        [typeof(SketchHoverIntent)] = (
            [[Touch(1, 120, 80, PointerPhase.Down, 0)]],
            [[Mouse(120, 80, PointerButtons.None, PointerPhase.Move, 0)]]),
        // ADR-0038 item 5: a drag that starts on a handle of the gizmo. A second finger and the key
        // Escape cancel it.
        [typeof(GizmoStartIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 110, 100, PointerPhase.Move, 10)], [Touch(1, 130, 105, PointerPhase.Move, 20)], [Touch(1, 130, 105, PointerPhase.Up, 400)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(110, 100, PointerButtons.Left, PointerPhase.Move, 10)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Move, 20)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Up, 400)]]),
        [typeof(GizmoMoveIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 110, 100, PointerPhase.Move, 10)], [Touch(1, 130, 105, PointerPhase.Move, 20)], [Touch(1, 130, 105, PointerPhase.Up, 400)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(110, 100, PointerButtons.Left, PointerPhase.Move, 10)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Move, 20)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Up, 400)]]),
        [typeof(GizmoEndIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 110, 100, PointerPhase.Move, 10)], [Touch(1, 130, 105, PointerPhase.Move, 20)], [Touch(1, 130, 105, PointerPhase.Up, 400)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(110, 100, PointerButtons.Left, PointerPhase.Move, 10)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Move, 20)], [Mouse(130, 105, PointerButtons.Left, PointerPhase.Up, 400)]]),
        [typeof(GizmoCancelIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 120, 100, PointerPhase.Move, 10)], [Touch(2, 200, 100, PointerPhase.Down, 20)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(120, 100, PointerButtons.Left, PointerPhase.Move, 10)], [new KeySample(InputKey.Escape, true, KeyModifiers.None, 20 * Ms)]]),
        [typeof(CancelIntent)] = (
            [[Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 120, 100, PointerPhase.Move, 10)], [Touch(1, 120, 100, PointerPhase.Cancel, 20)]],
            [[Mouse(100, 100, PointerButtons.Left, PointerPhase.Down, 0)], [Mouse(120, 100, PointerButtons.Left, PointerPhase.Move, 10)], [new FocusLostSample(20 * Ms)]]),
    };

    // The mode of the machine for the script of a kind; the other kinds run in the mode Scene.
    private static readonly Dictionary<Type, IntentMode> Modes = new() { [typeof(SketchPointIntent)] = IntentMode.Sketch, [typeof(SketchHoverIntent)] = IntentMode.Sketch };

    // The kinds whose script starts on a handle of the gizmo: the hit test gives the arrow X at
    // each point (ADR-0038 item 5).
    private static readonly HashSet<Type> OnGizmo = [typeof(GizmoStartIntent), typeof(GizmoMoveIntent), typeof(GizmoEndIntent), typeof(GizmoCancelIntent)];

    private static List<Intent> Run(InputSample[][] script, IntentSettings? settings = null, IntentMode mode = IntentMode.Scene, Func<double, double, GizmoHandle?>? hitTest = null)
    {
        var machine = new IntentMachine(settings) { Mode = mode, HitTest = hitTest };
        return [.. script.SelectMany(frame => machine.Step(frame))];
    }

    [Theory]
    [InlineData(nameof(OrbitIntent))]
    [InlineData(nameof(PanIntent))]
    [InlineData(nameof(ZoomIntent))]
    [InlineData(nameof(SelectIntent))]
    [InlineData(nameof(HideIntent))]
    [InlineData(nameof(ShowAllIntent))]
    [InlineData(nameof(CancelIntent))]
    [InlineData(nameof(UndoIntent))]
    [InlineData(nameof(RedoIntent))]
    [InlineData(nameof(SketchPointIntent))]
    [InlineData(nameof(GizmoStartIntent))]
    [InlineData(nameof(GizmoMoveIntent))]
    [InlineData(nameof(GizmoEndIntent))]
    [InlineData(nameof(GizmoCancelIntent))]
    public void A_Touch_Script_And_A_Mouse_Script_Give_The_Same_Intents(string kind)
    {
        var (type, (touch, mouse)) = Scripts.Single(script => script.Key.Name == kind);
        var mode = Modes.GetValueOrDefault(type, IntentMode.Scene);
        Func<double, double, GizmoHandle?>? hitTest = OnGizmo.Contains(type) ? (_, _) => GizmoHandle.X : null;

        var fromTouch = Run(touch, mode: mode, hitTest: hitTest);
        var fromMouse = Run(mouse, mode: mode, hitTest: hitTest);

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

    // ADR-0029 item 4: a press between a tap and a long press gives nothing, and a long press
    // that moved is a drag and not a hide.
    [Fact]
    public void A_Press_That_Is_Neither_A_Tap_Nor_A_Long_Press_Gives_No_Point_Intent()
    {
        var middle = Run([[Touch(1, 120, 80, PointerPhase.Down, 0)], [Touch(1, 120, 80, PointerPhase.Up, 400)]]);
        var moved = Run([[Touch(1, 120, 80, PointerPhase.Down, 0)], [Touch(1, 140, 80, PointerPhase.Move, 100)], [Touch(1, 140, 80, PointerPhase.Up, 700)]]);

        Assert.Empty(middle);
        Assert.DoesNotContain(moved, intent => intent is HideIntent or SelectIntent);
        Assert.Contains(moved, intent => intent is OrbitIntent);
    }

    // V36 of the second codebase review of 2026-10-28: a tap counts the most contacts down at one
    // time. Two fingers with one bounce gave Undo, the tap of three fingers.
    [Fact]
    public void A_Tap_Of_Two_Fingers_With_A_Bounce_Shows_All()
    {
        var bounce = Run([
            [Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0)],
            [Touch(2, 200, 100, PointerPhase.Up, 40)],
            [Touch(3, 200, 101, PointerPhase.Down, 60)],
            [Touch(1, 100, 100, PointerPhase.Up, 120), Touch(3, 200, 101, PointerPhase.Up, 120)]]);

        Assert.IsType<ShowAllIntent>(Assert.Single(bounce));
    }

    // A pair that moves is a pan or a pinch and not a tap of two fingers, and a slow pair taps
    // nothing.
    [Fact]
    public void A_Pair_That_Moves_Or_Stays_Too_Long_Does_Not_Show_All()
    {
        var moved = Run(Scripts[typeof(PanIntent)].Touch);
        var slow = Run([[Touch(1, 100, 100, PointerPhase.Down, 0), Touch(2, 200, 100, PointerPhase.Down, 0)], [Touch(1, 100, 100, PointerPhase.Up, 600), Touch(2, 200, 100, PointerPhase.Up, 600)]]);

        Assert.DoesNotContain(moved, intent => intent is ShowAllIntent);
        Assert.Empty(slow);
    }

    // The key H with no Shift hides the selection: a hide with no point.
    [Fact]
    public void The_Key_H_Hides_The_Selection()
    {
        var intent = Assert.Single(Run([[new KeySample(InputKey.H, true, KeyModifiers.None, 0)]]));

        Assert.Equal(new HideIntent(null, null), intent);
    }

    // ADR-0032 item 9: Control, Shift and Z redoes too, and Z or Y with no Control gives nothing.
    [Fact]
    public void The_Keys_Of_Undo_And_Redo_Need_Control()
    {
        Assert.Equal([new RedoIntent()], Run([[new KeySample(InputKey.Z, true, KeyModifiers.Control | KeyModifiers.Shift, 0)]]));
        Assert.Empty(Run([[new KeySample(InputKey.Z, true, KeyModifiers.None, 0), new KeySample(InputKey.Y, true, KeyModifiers.Shift, 0)]]));
        Assert.Empty(Run([[new KeySample(InputKey.Z, false, KeyModifiers.Control, 0)]]));
    }

    // A tap of three or four fingers is still and short, as a tap of two fingers is; five fingers
    // give nothing.
    [Fact]
    public void A_Tap_Of_Three_Fingers_That_Moves_Or_Lasts_Does_Not_Undo()
    {
        InputSample[] Down(int fingers) => [.. Enumerable.Range(1, fingers).Select(finger => (InputSample)Touch((ulong)finger, 100 * finger, 100, PointerPhase.Down, 0))];
        InputSample[] Up(int fingers, ulong ms) => [.. Enumerable.Range(1, fingers).Select(finger => (InputSample)Touch((ulong)finger, 100 * finger, 100, PointerPhase.Up, ms))];

        var moved = Run([Down(3), [Touch(1, 130, 100, PointerPhase.Move, 50)], Up(3, 120)]);
        var slow = Run([Down(3), Up(3, 600)]);
        var five = Run([Down(5), Up(5, 120)]);

        Assert.DoesNotContain(moved, intent => intent is UndoIntent);
        Assert.Empty(slow);
        Assert.Empty(five);
    }

    // Each gesture of ADR-0029 is a setting: a long press can select, and the map refuses a
    // show all for a gesture of a point.
    [Fact]
    public void The_Gestures_Of_A_Hide_Are_Settings()
    {
        var map = new Dictionary<Gesture, IntentAction>(IntentSettings.Default.Map) { [Gesture.LongPress] = IntentAction.Select };
        var settings = new IntentSettings { Map = map }.Validated();

        Assert.IsType<SelectIntent>(Assert.Single(Run(Scripts[typeof(HideIntent)].Touch, settings)));

        var wrong = new Dictionary<Gesture, IntentAction>(IntentSettings.Default.Map) { [Gesture.RightClick] = IntentAction.ShowAll };
        Assert.Throws<ArgumentException>(() => new IntentSettings { Map = wrong }.Validated());
    }

    // ADR-0034 item 1: in the mode Sketch a tap or a click gives a point of the sketch and no
    // selection; a long press, a right click, a tap of two fingers, H and Shift and H give
    // nothing; the drags, the wheel and the keys of undo still work.
    [Fact]
    public void The_Mode_Sketch_Gives_Points_And_No_Hide()
    {
        // The mark of a snap (R11b) is not a gesture of this test.
        static List<Intent> InSketch(InputSample[][] script) => [.. Run(script, mode: IntentMode.Sketch).Where(intent => intent is not SketchHoverIntent)];

        Assert.Empty(InSketch(Scripts[typeof(HideIntent)].Touch));
        Assert.Empty(InSketch(Scripts[typeof(HideIntent)].Mouse));
        Assert.Empty(InSketch(Scripts[typeof(ShowAllIntent)].Touch));
        Assert.Empty(InSketch(Scripts[typeof(ShowAllIntent)].Mouse));
        Assert.Empty(InSketch([[new KeySample(InputKey.H, true, KeyModifiers.None, 0)]]));
        Assert.DoesNotContain(InSketch(Scripts[typeof(SelectIntent)].Touch), intent => intent is SelectIntent);
        Assert.All(InSketch(Scripts[typeof(OrbitIntent)].Mouse), intent => Assert.IsType<OrbitIntent>(intent));
        Assert.All(InSketch(Scripts[typeof(ZoomIntent)].Mouse), intent => Assert.IsType<ZoomIntent>(intent));
        Assert.IsType<UndoIntent>(Assert.Single(InSketch(Scripts[typeof(UndoIntent)].Touch)));
        Assert.Empty(Run(Scripts[typeof(SketchPointIntent)].Mouse).OfType<SketchPointIntent>());
    }

    // A change of the mode ends the gesture in progress, so a press in one mode does not give a
    // point in the other.
    [Fact]
    public void A_Change_Of_The_Mode_Ends_The_Gesture_In_Progress()
    {
        var machine = new IntentMachine();
        machine.Step([Mouse(120, 80, PointerButtons.Left, PointerPhase.Down, 0)]);
        machine.Mode = IntentMode.Sketch;

        Assert.Empty(machine.Step([Mouse(120, 80, PointerButtons.Left, PointerPhase.Up, 100)]));
    }

    // The map of the mode Sketch is a setting, and it refuses a point of a sketch for a gesture
    // with no point.
    [Fact]
    public void The_Map_Of_The_Mode_Sketch_Is_A_Setting()
    {
        var map = new Dictionary<Gesture, IntentAction>(IntentSettings.Default.SketchMap) { [Gesture.LongPress] = IntentAction.SketchPoint };
        var settings = new IntentSettings { SketchMap = map }.Validated();

        Assert.IsType<SketchPointIntent>(Assert.Single(Run(Scripts[typeof(HideIntent)].Touch, settings, IntentMode.Sketch), intent => intent is not SketchHoverIntent));

        var wrong = new Dictionary<Gesture, IntentAction>(IntentSettings.Default.SketchMap) { [Gesture.HKey] = IntentAction.SketchPoint };
        Assert.Throws<ArgumentException>(() => new IntentSettings { SketchMap = wrong }.Validated());
    }

    private static bool Same(Intent a, Intent b) => (a, b) switch
    {
        (OrbitIntent x, OrbitIntent y) => Near(x.YawDegrees, y.YawDegrees) && Near(x.PitchDegrees, y.PitchDegrees),
        (PanIntent x, PanIntent y) => Near(x.DxPixels, y.DxPixels) && Near(x.DyPixels, y.DyPixels),
        (ZoomIntent x, ZoomIntent y) => Near(x.Factor, y.Factor),
        (SelectIntent x, SelectIntent y) => Near(x.X, y.X) && Near(x.Y, y.Y),
        (HideIntent x, HideIntent y) => x.X.HasValue == y.X.HasValue && Near(x.X ?? 0, y.X ?? 0) && Near(x.Y ?? 0, y.Y ?? 0),
        (ShowAllIntent, ShowAllIntent) => true,
        (CancelIntent, CancelIntent) => true,
        (UndoIntent, UndoIntent) => true,
        (RedoIntent, RedoIntent) => true,
        (SketchPointIntent x, SketchPointIntent y) => Near(x.X, y.X) && Near(x.Y, y.Y),
        (SketchHoverIntent x, SketchHoverIntent y) => Near(x.X, y.X) && Near(x.Y, y.Y),
        (GizmoStartIntent x, GizmoStartIntent y) => x.Handle == y.Handle && Near(x.X, y.X) && Near(x.Y, y.Y),
        (GizmoMoveIntent x, GizmoMoveIntent y) => Near(x.X, y.X) && Near(x.Y, y.Y),
        (GizmoEndIntent x, GizmoEndIntent y) => Near(x.X, y.X) && Near(x.Y, y.Y),
        (GizmoCancelIntent, GizmoCancelIntent) => true,
        _ => false,
    };

    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;
}
