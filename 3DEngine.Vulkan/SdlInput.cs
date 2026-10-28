using SDL3;
using ThreeDEngine.Core.Input;
using static SDL3.SDL3;

namespace ThreeDEngine.Vulkan;

// The translation of SDL events into the samples of the intent layer (ADR-0026 item 4,
// TASK-0082). The window turns the hint SDL_HINT_TOUCH_MOUSE_EVENTS off; this translation
// also drops each mouse event whose device is SDL_TOUCH_MOUSEID, so a touch never counts
// two times (decision D8). It keeps the state of the Shift and Control keys from the key
// events, so it makes no call of SDL, and a test gives it events that it builds. The text
// input of SDL becomes a text sample (ADR-0027 item 5, TASK-0084).
public sealed unsafe class SdlInput
{
    private readonly List<InputSample> _frame = new();
    private KeyModifiers _modifiers;

    // The samples since the last call of TakeFrame, in their order.
    public IReadOnlyList<InputSample> TakeFrame()
    {
        var frame = _frame.ToArray();
        _frame.Clear();
        return frame;
    }

    // One event of a window of this size in pixels; an event that gives no sample is left out.
    public void Translate(in SDL_Event sdlEvent, double windowWidth, double windowHeight)
    {
        var time = sdlEvent.common.timestamp;
        switch (sdlEvent.type)
        {
            case SDL_EventType.MouseMotion when (uint)sdlEvent.motion.which != SDL_TOUCH_MOUSEID:
                _frame.Add(Mouse(sdlEvent.motion.which, sdlEvent.motion.x, sdlEvent.motion.y, Buttons(sdlEvent.motion.state), PointerPhase.Move, time));
                break;

            case SDL_EventType.MouseButtonDown or SDL_EventType.MouseButtonUp when (uint)sdlEvent.button.which != SDL_TOUCH_MOUSEID:
                _frame.Add(Mouse(
                    sdlEvent.button.which,
                    sdlEvent.button.x,
                    sdlEvent.button.y,
                    Button(sdlEvent.button.button),
                    sdlEvent.type == SDL_EventType.MouseButtonDown ? PointerPhase.Down : PointerPhase.Up,
                    time));
                break;

            case SDL_EventType.MouseWheel when (uint)sdlEvent.wheel.which != SDL_TOUCH_MOUSEID:
                var steps = sdlEvent.wheel.direction == SDL_MouseWheelDirection.Flipped ? -sdlEvent.wheel.y : sdlEvent.wheel.y;
                _frame.Add(new WheelSample(steps, time));
                break;

            case SDL_EventType.FingerDown or SDL_EventType.FingerMotion or SDL_EventType.FingerUp:
                var finger = sdlEvent.tfinger;
                var phase = sdlEvent.type switch
                {
                    SDL_EventType.FingerDown => PointerPhase.Down,
                    SDL_EventType.FingerUp => PointerPhase.Up,
                    _ => PointerPhase.Move,
                };
                // SDL gives a finger in the range 0 to 1 of the window.
                _frame.Add(new PointerSample(
                    DeviceKind.Touch, (ulong)finger.touchID, (ulong)finger.fingerID,
                    finger.x * windowWidth, finger.y * windowHeight, finger.pressure,
                    PointerButtons.None, _modifiers, phase, time));
                break;

            case SDL_EventType.KeyDown or SDL_EventType.KeyUp:
                Key(sdlEvent.key.scancode, sdlEvent.type == SDL_EventType.KeyDown, time);
                break;

            case SDL_EventType.TextInput:
                var text = NativeStrings.Read(sdlEvent.text.text);
                if (text.Length > 0)
                    _frame.Add(new TextSample(text, time));
                break;

            case SDL_EventType.WindowFocusLost:
                _modifiers = KeyModifiers.None;
                _frame.Add(new FocusLostSample(time));
                break;
        }
    }

    private PointerSample Mouse(SDL_MouseID which, float x, float y, PointerButtons buttons, PointerPhase phase, ulong time)
        => new(DeviceKind.Mouse, (ulong)which, 0, x, y, 1, buttons, _modifiers, phase, time);

    private void Key(SDL_Scancode code, bool down, ulong time)
    {
        if (code is SDL_Scancode.LeftShift or SDL_Scancode.RightShift)
        {
            _modifiers = down ? _modifiers | KeyModifiers.Shift : _modifiers & ~KeyModifiers.Shift;
            return;
        }

        if (code is SDL_Scancode.LeftControl or SDL_Scancode.RightControl)
        {
            _modifiers = down ? _modifiers | KeyModifiers.Control : _modifiers & ~KeyModifiers.Control;
            return;
        }

        InputKey? key = code switch
        {
            SDL_Scancode.Left => InputKey.ArrowLeft,
            SDL_Scancode.Right => InputKey.ArrowRight,
            SDL_Scancode.Up => InputKey.ArrowUp,
            SDL_Scancode.Down => InputKey.ArrowDown,
            SDL_Scancode.KpPlus or SDL_Scancode.Equals => InputKey.Plus,
            SDL_Scancode.KpMinus or SDL_Scancode.Minus => InputKey.Minus,
            SDL_Scancode.Backspace => InputKey.Backspace,
            SDL_Scancode.Delete => InputKey.Delete,
            SDL_Scancode.Home => InputKey.Home,
            SDL_Scancode.End => InputKey.End,
            SDL_Scancode.Return or SDL_Scancode.KpEnter => InputKey.Enter,
            SDL_Scancode.Escape => InputKey.Escape,
            SDL_Scancode.Tab => InputKey.Tab,
            _ => null,
        };
        if (key is { } known)
            _frame.Add(new KeySample(known, down, _modifiers, time));
    }

    private static PointerButtons Buttons(SDL_MouseButtonFlags state)
        => ((state & SDL_MouseButtonFlags.Left) != 0 ? PointerButtons.Left : PointerButtons.None)
        | ((state & SDL_MouseButtonFlags.Middle) != 0 ? PointerButtons.Middle : PointerButtons.None)
        | ((state & SDL_MouseButtonFlags.Right) != 0 ? PointerButtons.Right : PointerButtons.None);

    // SDL numbers the buttons 1 (left), 2 (middle) and 3 (right).
    private static PointerButtons Button(byte button) => button switch
    {
        1 => PointerButtons.Left,
        2 => PointerButtons.Middle,
        3 => PointerButtons.Right,
        _ => PointerButtons.None,
    };
}
