namespace ThreeDEngine.Core.Input;

// The samples of the input of one window (ADR-0026 item 1). The host collects the
// samples of one frame, in their order, and gives them to the IntentMachine. A sample
// names its kind of device; an intent does not.

public enum DeviceKind
{
    Mouse,
    Touch,

    // A pen takes the path of one finger (ADR-0026 item 1, decision D2).
    Pen,
}

public enum PointerPhase
{
    Down,
    Move,
    Up,

    // The pointer is lost, for example when the window loses the focus.
    Cancel,
}

[Flags]
public enum PointerButtons
{
    None = 0,
    Left = 1,
    Middle = 2,
    Right = 4,
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
}

// The keys that the default map of gestures reads (ADR-0026 item 3).
public enum InputKey
{
    ArrowLeft,
    ArrowRight,
    ArrowUp,
    ArrowDown,
    Plus,
    Minus,
}

// One sample, at a time in nanoseconds of one clock of the host.
public abstract record InputSample(ulong TimestampNs);

// A pointer: the button that changed for a Down or an Up, and the buttons held for a Move.
// The position is in pixels of the window, with x to the right and y down.
public sealed record PointerSample(
    DeviceKind Kind,
    ulong DeviceId,
    ulong PointerId,
    double X,
    double Y,
    float Pressure,
    PointerButtons Buttons,
    KeyModifiers Modifiers,
    PointerPhase Phase,
    ulong TimestampNs) : InputSample(TimestampNs);

// A turn of the wheel: positive away from the person.
public sealed record WheelSample(double Steps, ulong TimestampNs) : InputSample(TimestampNs);

// A key that goes down (also when it repeats) or up.
public sealed record KeySample(InputKey Key, bool Down, KeyModifiers Modifiers, ulong TimestampNs) : InputSample(TimestampNs);

// The window lost the focus, so each pointer that is down is lost.
public sealed record FocusLostSample(ulong TimestampNs) : InputSample(TimestampNs);
