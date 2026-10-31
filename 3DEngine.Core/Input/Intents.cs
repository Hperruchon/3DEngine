namespace ThreeDEngine.Core.Input;

// What the person asks for, with no kind of device (ADR-0026 item 2). The host applies
// each intent to the state of the client: in R7, the camera and the selection.
public abstract record Intent;

// Turn the camera about its target by these angles.
public sealed record OrbitIntent(double YawDegrees, double PitchDegrees) : Intent;

// Move the camera and its target in the plane of the view, by these pixels of the window.
public sealed record PanIntent(double DxPixels, double DyPixels) : Intent;

// Multiply the distance of the camera by this factor; a factor below 1 comes closer.
public sealed record ZoomIntent(double Factor) : Intent;

// Select what is at this point of the window, in pixels.
public sealed record SelectIntent(double X, double Y) : Intent;

// Hide the body at this point of the window, or the selected body when the point is null
// (ADR-0029 items 4 and 5).
public sealed record HideIntent(double? X, double? Y) : Intent;

// Show each hidden body (ADR-0029 items 4 and 5).
public sealed record ShowAllIntent : Intent;

// Stop the gesture in progress: a pointer was lost.
public sealed record CancelIntent : Intent;

// Undo the last step of the document, or redo the last undone step (ADR-0032 item 9).
public sealed record UndoIntent : Intent;

public sealed record RedoIntent : Intent;

// A point of the active sketch at this point of the window, in pixels (ADR-0034 item 1). Only the
// mode Sketch gives it; the host gives it to the active tool.
public sealed record SketchPointIntent(double X, double Y) : Intent;

// Phase R11b (TASK-0134, ADR-0041 item 6): the mouse at this point of the window with no button
// down, in the mode Sketch. The host shows there the mark of the point that a tap would take. A
// touch and a pen have no hover, so they give none.
public sealed record SketchHoverIntent(double X, double Y) : Intent;

// A handle of the translate gizmo (ADR-0038 item 4): an arrow along one axis, or a square in
// one plane.
public enum GizmoHandle
{
    X,
    Y,
    Z,
    XY,
    YZ,
    ZX,
}

// ADR-0038 item 5: a drag of a handle of the gizmo. The start gives the handle and the point of
// the press, each motion and the release give the point of the pointer, in pixels of the
// window, and a cancel ends the drag with no command.
public sealed record GizmoStartIntent(GizmoHandle Handle, double X, double Y) : Intent;

public sealed record GizmoMoveIntent(double X, double Y) : Intent;

public sealed record GizmoEndIntent(double X, double Y) : Intent;

public sealed record GizmoCancelIntent : Intent;

// The mode of the intent layer, which the host sets (ADR-0034 item 1). In the mode Sketch the
// entries of IntentSettings.SketchMap replace the entries of IntentSettings.Map.
public enum IntentMode
{
    Scene,
    Sketch,
}

// A gesture that the IntentMachine recognises (ADR-0026 item 3).
public enum Gesture
{
    OneFingerDrag,
    TwoFingerDrag,
    Pinch,
    Tap,
    LeftDrag,
    MiddleDrag,
    ShiftLeftDrag,
    Wheel,
    Click,
    ArrowKeys,
    ShiftArrowKeys,
    PlusMinusKeys,

    // ADR-0029 item 4: a press of a finger or a pen with no motion that lasts at least
    // LongPressNanoseconds, a click of the right button, a tap of two fingers, the key H, and
    // Shift and H.
    LongPress,
    RightClick,
    TwoFingerTap,
    HKey,
    ShiftHKey,

    // ADR-0032 item 9 (Q1 A): a tap of three fingers, a tap of four fingers, Control and Z,
    // Control and Y, and Control, Shift and Z.
    ThreeFingerTap,
    FourFingerTap,
    ControlZKey,
    ControlYKey,
    ControlShiftZKey,
}

// The action that a gesture gives.
public enum IntentAction
{
    None,
    Orbit,
    Pan,
    Zoom,
    Select,
    Hide,
    ShowAll,
    Undo,
    Redo,

    // ADR-0034 item 1: a point of the active sketch.
    SketchPoint,
}

// The settings of the intent layer: the map from each gesture to an action, and the
// numbers of the gestures. The map is a user setting of the client (ADR-0007, ADR-0026
// item 3, decision D4). A gesture of motion (a drag or the arrow keys) gives orbit, pan,
// zoom or nothing; a gesture of scale (a pinch, the wheel, plus and minus) gives zoom or
// nothing; a gesture of a point (a tap, a click, a long press, a right click) gives select,
// hide or nothing; a gesture with no point (a tap of two, three or four fingers, H, Shift and
// H, and the keys of undo and redo) gives hide of the selection, show all, undo, redo or
// nothing (ADR-0029 item 4, ADR-0032 item 9). A gesture of a point can also give a point of a
// sketch (ADR-0034 item 1).
public sealed record IntentSettings
{
    public static IntentSettings Default { get; } = new();

    public IReadOnlyDictionary<Gesture, IntentAction> Map { get; init; } = new Dictionary<Gesture, IntentAction>
    {
        [Gesture.OneFingerDrag] = IntentAction.Orbit,
        [Gesture.LeftDrag] = IntentAction.Orbit,
        [Gesture.ArrowKeys] = IntentAction.Orbit,
        [Gesture.TwoFingerDrag] = IntentAction.Pan,
        [Gesture.MiddleDrag] = IntentAction.Pan,
        [Gesture.ShiftLeftDrag] = IntentAction.Pan,
        [Gesture.ShiftArrowKeys] = IntentAction.Pan,
        [Gesture.Pinch] = IntentAction.Zoom,
        [Gesture.Wheel] = IntentAction.Zoom,
        [Gesture.PlusMinusKeys] = IntentAction.Zoom,
        [Gesture.Tap] = IntentAction.Select,
        [Gesture.Click] = IntentAction.Select,
        [Gesture.LongPress] = IntentAction.Hide,
        [Gesture.RightClick] = IntentAction.Hide,
        [Gesture.HKey] = IntentAction.Hide,
        [Gesture.TwoFingerTap] = IntentAction.ShowAll,
        [Gesture.ShiftHKey] = IntentAction.ShowAll,
        [Gesture.ThreeFingerTap] = IntentAction.Undo,
        [Gesture.ControlZKey] = IntentAction.Undo,
        [Gesture.FourFingerTap] = IntentAction.Redo,
        [Gesture.ControlYKey] = IntentAction.Redo,
        [Gesture.ControlShiftZKey] = IntentAction.Redo,
    };

    // ADR-0034 item 1: the entries that replace the entries of Map in the mode Sketch. A tap or a
    // click gives a point of the sketch, and the gestures of a hide and of a show all give
    // nothing, so a gesture never hides a body during a sketch.
    public IReadOnlyDictionary<Gesture, IntentAction> SketchMap { get; init; } = new Dictionary<Gesture, IntentAction>
    {
        [Gesture.Tap] = IntentAction.SketchPoint,
        [Gesture.Click] = IntentAction.SketchPoint,
        [Gesture.LongPress] = IntentAction.None,
        [Gesture.RightClick] = IntentAction.None,
        [Gesture.TwoFingerTap] = IntentAction.None,
        [Gesture.HKey] = IntentAction.None,
        [Gesture.ShiftHKey] = IntentAction.None,
    };

    // The degrees of orbit for each pixel of a drag.
    public double OrbitDegreesPerPixel { get; init; } = 0.3;

    // The factor of the distance for one step of zoom (a turn of the wheel, a key).
    public double ZoomPerStep { get; init; } = 0.9;

    // The pixels of a vertical drag that give one step of zoom, when a drag gives zoom.
    public double PixelsPerZoomStep { get; init; } = 50;

    // The pixels of motion that one press of an arrow key gives.
    public double PixelsPerKey { get; init; } = 16;

    // A press that moves less than this, in pixels, is not a drag yet.
    public double DragSlopPixels { get; init; } = 4;

    // A tap or a click: shorter than this time, and with less motion than the slop.
    public ulong TapMaxNanoseconds { get; init; } = 300_000_000;

    // A long press: at least this time, and with less motion than the slop. The state machine
    // decides it at the release (ADR-0029 item 4).
    public ulong LongPressNanoseconds { get; init; } = 500_000_000;

    public IntentAction ActionOf(Gesture gesture) => Map.TryGetValue(gesture, out var action) ? action : IntentAction.None;

    // The action of a gesture in a mode: in the mode Sketch, an entry of SketchMap wins.
    public IntentAction ActionOf(Gesture gesture, IntentMode mode)
        => mode == IntentMode.Sketch && SketchMap.TryGetValue(gesture, out var action) ? action : ActionOf(gesture);

    // Throws when a map gives a gesture an action that it cannot give.
    public IntentSettings Validated()
    {
        foreach (var (gesture, action) in Map.Concat(SketchMap))
        {
            var permitted = gesture switch
            {
                Gesture.Pinch or Gesture.Wheel or Gesture.PlusMinusKeys => action is IntentAction.None or IntentAction.Zoom,
                Gesture.Tap or Gesture.Click or Gesture.LongPress or Gesture.RightClick
                    => action is IntentAction.None or IntentAction.Select or IntentAction.Hide or IntentAction.SketchPoint,
                Gesture.TwoFingerTap or Gesture.HKey or Gesture.ShiftHKey or Gesture.ThreeFingerTap or Gesture.FourFingerTap
                    or Gesture.ControlZKey or Gesture.ControlYKey or Gesture.ControlShiftZKey
                    => action is IntentAction.None or IntentAction.Hide or IntentAction.ShowAll or IntentAction.Undo or IntentAction.Redo,
                _ => action is IntentAction.None or IntentAction.Orbit or IntentAction.Pan or IntentAction.Zoom,
            };
            if (!permitted)
                throw new ArgumentException($"The gesture {gesture} cannot give the action {action}.", nameof(Map));
        }

        return this;
    }
}
