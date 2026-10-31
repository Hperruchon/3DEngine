using ThreeDEngine.Core.Gizmo;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Tests;

// The translate gizmo (ADR-0038, TASK-0113), with no device: the place of the handles, the hit
// test, the offset of a drag, the shapes, and the drag in the intent machine. ADR-0038 names this
// file in enforced-by.
//
// The camera looks along −Z from (0, 0, 10) at the pivot (0, 0, 0), so X points to the right of
// the window, Y up, and Z to the eye. A window of 800 × 600 points with a field of view of 60
// degrees gives 1.9245… world units for 100 points at the depth 10.
public class GizmoTests
{
    private const double Width = 800;
    private const double Height = 600;
    private const ulong Ms = 1_000_000;

    private static readonly (double X, double Y, double Z) Pivot = (0, 0, 0);

    private static Camera Front(double distance = 10) => new() { Target = (0, 0, 0), Distance = distance, YawDegrees = 0, PitchDegrees = 0 };

    private static double PerPoint(double distance = 10) => 2 * distance * Math.Tan(Math.PI / 6) / Height;

    private static GizmoLayout Layout(double distance = 10) => TranslateGizmo.Layout(Front(distance), Width, Height, Pivot)!;

    // ADR-0038 item 3: each arrow is 100 points long on the screen at each distance.
    [Theory]
    [InlineData(10.0)]
    [InlineData(40.0)]
    public void Each_Arrow_Is_100_Points_Long_At_Each_Distance(double distance)
    {
        var layout = Layout(distance);

        Assert.Equal(400, layout.Origin.X, 3);
        Assert.Equal(300, layout.Origin.Y, 3);
        Assert.Equal(500, layout.Ends[GizmoHandle.X].X, 3);
        Assert.Equal(300, layout.Ends[GizmoHandle.X].Y, 3);
        Assert.Equal(400, layout.Ends[GizmoHandle.Y].X, 3);
        Assert.Equal(200, layout.Ends[GizmoHandle.Y].Y, 3);
        Assert.Equal(100 * PerPoint(distance), TranslateGizmo.WorldLength(Front(distance), Width, Height, Pivot)!.Value, 9);
    }

    // ADR-0038 item 4: a square wins over an arrow, an arrow takes a press within 8 points, and a
    // press far from each handle takes nothing.
    [Theory]
    [InlineData(480.0, 305.0, GizmoHandle.X)]
    [InlineData(395.0, 230.0, GizmoHandle.Y)]
    [InlineData(435.0, 265.0, GizmoHandle.XY)]
    [InlineData(480.0, 310.0, null)]
    [InlineData(600.0, 500.0, null)]
    public void A_Press_Takes_The_Handle_Under_It(double x, double y, GizmoHandle? expected)
    {
        Assert.Equal(expected, TranslateGizmo.Hit(Layout(), x, y));
    }

    // ADR-0038 item 8: an arrow moves along its axis by the motion of the pointer along it, and a
    // square moves in its plane.
    [Fact]
    public void A_Drag_Gives_The_Offset_Of_Its_Handle()
    {
        var alongX = TranslateGizmo.Offset(Front(), Width, Height, Pivot, GizmoHandle.X, (450, 300), (475, 300))!.Value;
        Assert.Equal(25 * PerPoint(), alongX.X, 9);
        Assert.Equal((0.0, 0.0), (alongX.Y, alongX.Z));

        // A motion across the arrow does not move the body across it.
        var slanted = TranslateGizmo.Offset(Front(), Width, Height, Pivot, GizmoHandle.X, (450, 300), (475, 290))!.Value;
        Assert.InRange(slanted.X, 0.99 * 25 * PerPoint(), 1.01 * 25 * PerPoint());
        Assert.Equal((0.0, 0.0), (slanted.Y, slanted.Z));

        var inXY = TranslateGizmo.Offset(Front(), Width, Height, Pivot, GizmoHandle.XY, (420, 280), (440, 260))!.Value;
        Assert.Equal(20 * PerPoint(), inXY.X, 9);
        Assert.Equal(20 * PerPoint(), inXY.Y, 9);
        Assert.Equal(0, inXY.Z, 12);
    }

    // ADR-0038 item 8: a ray parallel to the axis gives no offset, and the arrow that points to
    // the eye has no triangle.
    [Fact]
    public void An_Axis_That_Points_To_The_Eye_Gives_No_Offset_And_No_Triangle()
    {
        Assert.Null(TranslateGizmo.Offset(Front(), Width, Height, Pivot, GizmoHandle.Z, (400, 300), (420, 300)));
        Assert.Empty(TranslateGizmo.Shapes(Layout(), null).Single(shape => shape.Handle == GizmoHandle.Z).Triangles);
    }

    // ADR-0038 item 4: the handle under a drag is yellow, and each other keeps its colour.
    [Fact]
    public void The_Active_Handle_Is_Yellow()
    {
        var shapes = TranslateGizmo.Shapes(Layout(), GizmoHandle.X);

        Assert.Equal(6, shapes.Count);
        Assert.Equal(TranslateGizmo.Colour(GizmoHandle.X, GizmoHandle.X), shapes.Single(shape => shape.Handle == GizmoHandle.X).Colour);
        Assert.Equal(0.85f, shapes.Single(shape => shape.Handle == GizmoHandle.X).Colour.Y, 2);
        Assert.Equal(TranslateGizmo.Colour(GizmoHandle.Y, null), shapes.Single(shape => shape.Handle == GizmoHandle.Y).Colour);
        Assert.Equal(9, shapes.Single(shape => shape.Handle == GizmoHandle.X).Triangles.Length);
    }

    // A pivot behind the eye gives no gizmo.
    [Fact]
    public void A_Pivot_Behind_The_Eye_Gives_No_Gizmo()
    {
        Assert.Null(TranslateGizmo.Layout(Front(), Width, Height, (0, 0, 20)));
    }

    private static PointerSample Mouse(double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Mouse, 1, 0, x, y, 1, PointerButtons.Left, KeyModifiers.None, phase, ms * Ms);

    private static PointerSample Touch(ulong finger, double x, double y, PointerPhase phase, ulong ms)
        => new(DeviceKind.Touch, 1, finger, x, y, 1, PointerButtons.None, KeyModifiers.None, phase, ms * Ms);

    private static List<Intent> Run(IntentMachine machine, params InputSample[][] frames)
        => [.. frames.SelectMany(frame => machine.Step(frame))];

    private static IntentMachine OnHandle(IntentMode mode = IntentMode.Scene)
        => new() { Mode = mode, HitTest = (x, _) => x < 150 ? GizmoHandle.X : null };

    // ADR-0038 item 5: a press on a handle that does not move is a click, so it selects.
    [Fact]
    public void A_Still_Press_On_A_Handle_Selects()
    {
        var intents = Run(OnHandle(), [Mouse(100, 100, PointerPhase.Down, 0)], [Mouse(100, 100, PointerPhase.Up, 100)]);

        Assert.Equal([new SelectIntent(100, 100)], intents);
    }

    // ADR-0038 items 5 and 7: in the mode Sketch, and off a handle, a drag orbits as before.
    [Theory]
    [InlineData(IntentMode.Sketch, 100.0)]
    [InlineData(IntentMode.Scene, 200.0)]
    public void A_Drag_Off_A_Handle_Or_In_A_Sketch_Orbits(IntentMode mode, double x)
    {
        var intents = Run(OnHandle(mode), [Mouse(x, 100, PointerPhase.Down, 0)], [Mouse(x + 20, 100, PointerPhase.Move, 10)], [Mouse(x + 20, 100, PointerPhase.Up, 400)]);

        // The press of the mode Sketch also gives the mark of a snap (R11b), which is not a drag.
        Assert.IsType<OrbitIntent>(Assert.Single(intents, intent => intent is not SketchHoverIntent));
    }

    // ADR-0038 item 5: after Escape the rest of the press gives nothing, and a second finger
    // cancels a drag of one finger, which then pans.
    [Fact]
    public void A_Cancel_Ends_The_Drag_And_The_Press_Gives_Nothing_More()
    {
        var byKey = Run(OnHandle(),
            [Mouse(100, 100, PointerPhase.Down, 0)], [Mouse(120, 100, PointerPhase.Move, 10)],
            [new KeySample(InputKey.Escape, true, KeyModifiers.None, 20 * Ms)],
            [Mouse(140, 100, PointerPhase.Move, 30)], [Mouse(140, 100, PointerPhase.Up, 400)]);
        Assert.Equal([typeof(GizmoStartIntent), typeof(GizmoMoveIntent), typeof(GizmoCancelIntent)], byKey.Select(intent => intent.GetType()));

        var byFinger = Run(OnHandle(),
            [Touch(1, 100, 100, PointerPhase.Down, 0)], [Touch(1, 120, 100, PointerPhase.Move, 10)],
            [Touch(2, 200, 100, PointerPhase.Down, 20)],
            [Touch(1, 130, 110, PointerPhase.Move, 30), Touch(2, 210, 110, PointerPhase.Move, 30)]);
        Assert.Equal([typeof(GizmoStartIntent), typeof(GizmoMoveIntent), typeof(GizmoCancelIntent), typeof(PanIntent)], byFinger.Select(intent => intent.GetType()));
    }

    // Finding V64 (TASK-0144): Escape before the slop left the press open, and a motion after it
    // started the drag. The rest of the press gives nothing, and no drag started, so no cancel.
    [Fact]
    public void Escape_Before_The_Slop_Cancels_The_Press_On_A_Handle()
    {
        var intents = Run(OnHandle(),
            [Mouse(100, 100, PointerPhase.Down, 0)],
            [new KeySample(InputKey.Escape, true, KeyModifiers.None, 10 * Ms)],
            [Mouse(140, 100, PointerPhase.Move, 20)], [Mouse(140, 100, PointerPhase.Up, 400)]);

        Assert.Empty(intents);
    }

    // Finding V65 of the codebase review of 2026-10-31 (c) (TASK-0151): a press on a handle, Escape, and a
    // release with no motion inside the tap time gave a click or a tap, which selected.
    [Fact]
    public void Escape_Then_A_Still_Release_On_A_Handle_Gives_No_Click_And_No_Tap()
    {
        var mouse = Run(OnHandle(),
            [Mouse(100, 100, PointerPhase.Down, 0)],
            [new KeySample(InputKey.Escape, true, KeyModifiers.None, 10 * Ms)],
            [Mouse(100, 100, PointerPhase.Up, 100)]);
        var finger = Run(OnHandle(),
            [Touch(1, 100, 100, PointerPhase.Down, 0)],
            [new KeySample(InputKey.Escape, true, KeyModifiers.None, 10 * Ms)],
            [Touch(1, 100, 100, PointerPhase.Up, 100)]);
        var control = Run(OnHandle(), [Mouse(100, 100, PointerPhase.Down, 0)], [Mouse(100, 100, PointerPhase.Up, 100)]);

        Assert.Empty(mouse);
        Assert.Empty(finger);
        Assert.Equal([typeof(SelectIntent)], control.Select(intent => intent.GetType()));
    }

    // ADR-0038 item 5: a change of the mode during a drag cancels it.
    [Fact]
    public void A_Change_Of_The_Mode_Cancels_The_Drag()
    {
        var machine = OnHandle();
        Run(machine, [Mouse(100, 100, PointerPhase.Down, 0)], [Mouse(120, 100, PointerPhase.Move, 10)]);

        var changed = new List<Intent>();
        machine.Mode = IntentMode.Sketch;
        changed.AddRange(machine.Step([Mouse(130, 100, PointerPhase.Move, 20)]));
        changed.AddRange(machine.Step([Mouse(130, 100, PointerPhase.Up, 400)]));

        Assert.DoesNotContain(changed, intent => intent is GizmoMoveIntent or GizmoEndIntent);
    }

    // Finding V51 of the codebase review of 2026-10-30 (b) (TASK-0124): with yaw 60, a drag of the X
    // arrow toward −X gives a negative offset up to the vanishing point of −X, near x = 100. Past it,
    // the nearest point of the axis was behind the eye, and the offset jumped to +X. Now there is
    // no offset there, so the drag keeps its last offset (ADR-0038 item 8).
    [Fact]
    public void A_Drag_Past_The_Vanishing_Point_Of_An_Axis_Gives_No_Offset()
    {
        var camera = new Camera { Target = (0, 0, 0), Distance = 10, YawDegrees = 60, PitchDegrees = 0 };
        var layout = TranslateGizmo.Layout(camera, Width, Height, Pivot)!;
        var start = ((layout.Origin.X + layout.Ends[GizmoHandle.X].X) / 2, (double)layout.Origin.Y);

        foreach (var x in new[] { 300.0, 150.0, 110.0 })
            Assert.True(TranslateGizmo.Offset(camera, Width, Height, Pivot, GizmoHandle.X, start, (x, 300.0)) is { X: < 0 }, $"x = {x}");
        foreach (var x in new[] { 90.0, 50.0 })
            Assert.Null(TranslateGizmo.Offset(camera, Width, Height, Pivot, GizmoHandle.X, start, (x, 300.0)));
    }

    // Finding V53 (TASK-0124, answer Q2 A of the owner): during a drag of the gizmo, the wheel, the
    // camera keys, H, undo and redo give nothing; Escape still cancels. Before and after the drag,
    // the same inputs act.
    [Fact]
    public void Other_Input_During_A_Drag_Gives_Nothing()
    {
        InputSample[][] others =
        [
            [new WheelSample(1, 20 * Ms)],
            [new KeySample(InputKey.ArrowLeft, true, KeyModifiers.None, 30 * Ms)],
            [new KeySample(InputKey.Plus, true, KeyModifiers.None, 31 * Ms)],
            [new KeySample(InputKey.H, true, KeyModifiers.None, 32 * Ms)],
            [new KeySample(InputKey.Z, true, KeyModifiers.Control, 33 * Ms)],
            [new KeySample(InputKey.Y, true, KeyModifiers.Control, 34 * Ms)],
        ];

        var during = Run(OnHandle(), [[Mouse(100, 100, PointerPhase.Down, 0)], [Mouse(120, 100, PointerPhase.Move, 10)], .. others,
            [Mouse(121, 100, PointerPhase.Move, 60)], [Mouse(121, 100, PointerPhase.Up, 400)]]);
        Assert.Equal([typeof(GizmoStartIntent), typeof(GizmoMoveIntent), typeof(GizmoMoveIntent), typeof(GizmoEndIntent)], during.Select(intent => intent.GetType()));

        Assert.Equal(others.Length, Run(OnHandle(), others).Count);
    }
    // Finding V58 of the codebase review of 2026-10-31 (TASK-0133): a press on a handle before the slop
    // let a wheel and an undo act. They give nothing from the press on.
    [Fact]
    public void Other_Input_Before_The_Slop_Of_A_Press_On_A_Handle_Gives_Nothing()
    {
        var intents = Run(OnHandle(),
            [Mouse(100, 100, PointerPhase.Down, 0)],
            [new WheelSample(1, 5 * Ms)],
            [new KeySample(InputKey.Z, true, KeyModifiers.Control, 6 * Ms)],
            [Mouse(130, 100, PointerPhase.Move, 10)],
            [Mouse(130, 100, PointerPhase.Up, 400)]);

        Assert.Equal([typeof(GizmoStartIntent), typeof(GizmoMoveIntent), typeof(GizmoEndIntent)], intents.Select(intent => intent.GetType()));
    }

    // ADR-0041 item 6 (phase R11b, TASK-0134): the mode Scene gives no mark of a snap, and the mode
    // Sketch gives one for the mouse with no button and for the press.
    [Fact]
    public void Only_The_Mode_Sketch_Gives_The_Mark_Of_A_Snap()
    {
        var hover = new InputSample[] { new PointerSample(DeviceKind.Mouse, 1, 0, 200, 150, 1, PointerButtons.None, KeyModifiers.None, PointerPhase.Move, 0) };

        Assert.Empty(new IntentMachine { Mode = IntentMode.Scene }.Step(hover));
        Assert.Equal(new SketchHoverIntent(200, 150), Assert.Single(new IntentMachine { Mode = IntentMode.Sketch }.Step(hover)));
    }

}
