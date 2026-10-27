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

// Stop the gesture in progress: a pointer was lost.
public sealed record CancelIntent : Intent;

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
}

// The action that a gesture gives.
public enum IntentAction
{
    None,
    Orbit,
    Pan,
    Zoom,
    Select,
}

// The settings of the intent layer: the map from each gesture to an action, and the
// numbers of the gestures. The map is a user setting of the client (ADR-0007, ADR-0026
// item 3, decision D4). A gesture of motion (a drag or the arrow keys) gives orbit, pan,
// zoom or nothing; a gesture of scale (a pinch, the wheel, plus and minus) gives zoom or
// nothing; a gesture of a point (a tap, a click) gives select or nothing.
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

    public IntentAction ActionOf(Gesture gesture) => Map.TryGetValue(gesture, out var action) ? action : IntentAction.None;

    // Throws when the map gives a gesture an action that it cannot give.
    public IntentSettings Validated()
    {
        foreach (var (gesture, action) in Map)
        {
            var permitted = gesture switch
            {
                Gesture.Pinch or Gesture.Wheel or Gesture.PlusMinusKeys => action is IntentAction.None or IntentAction.Zoom,
                Gesture.Tap or Gesture.Click => action is IntentAction.None or IntentAction.Select,
                _ => action is IntentAction.None or IntentAction.Orbit or IntentAction.Pan or IntentAction.Zoom,
            };
            if (!permitted)
                throw new ArgumentException($"The gesture {gesture} cannot give the action {action}.", nameof(Map));
        }

        return this;
    }
}
