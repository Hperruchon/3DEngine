using SDL;
using ThreeDEngine.Core.Input;
using static SDL.SDL3;

namespace ThreeDEngine.Vulkan;

// The translation of SDL events into the samples of the intent layer (ADR-0026 item 4,
// TASK-0082). The window turns the hint SDL_HINT_TOUCH_MOUSE_EVENTS off; this translation
// also drops each mouse event whose device is SDL_TOUCH_MOUSEID, so a touch never counts
// two times (decision D8). It keeps the state of the Shift and Control keys from the key
// events, so it makes no call of SDL, and a test gives it events that it builds. The text
// input of SDL becomes a text sample (ADR-0027 item 5, TASK-0084).
//
// Register entry R-0043 and finding V85 (TASK-0188, TASK-0191): a press and a release keep the place of the event.
// Window corrects the place that SDL holds before SDL reads a button message, on Windows.
public sealed unsafe class SdlInput
{
    private readonly List<InputSample> _frame = new();
    private KeyModifiers _modifiers;

    // ADR-0150 item 1 (phase P0.154, TASK-0385): on macOS the Command key gives the modifier of the shortcuts, Control, as the
    // key Control does on the other platforms, so Command and K open the command line there. The other platforms leave the
    // key of the system out, as before.
    private readonly bool _macOS;

    public SdlInput()
        : this(OperatingSystem.IsMacOS())
    {
    }

    public SdlInput(bool macOS) => _macOS = macOS;

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
        switch (sdlEvent.Type)
        {
            case SDL_EventType.SDL_EVENT_MOUSE_MOTION when sdlEvent.motion.which != SDL_TOUCH_MOUSEID:
                _frame.Add(Mouse(sdlEvent.motion.which, sdlEvent.motion.x, sdlEvent.motion.y, Buttons(sdlEvent.motion.state), PointerPhase.Move, time));
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN or SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP when sdlEvent.button.which != SDL_TOUCH_MOUSEID:
                _frame.Add(Mouse(
                    sdlEvent.button.which,
                    sdlEvent.button.x,
                    sdlEvent.button.y,
                    Button(sdlEvent.button.button),
                    sdlEvent.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN ? PointerPhase.Down : PointerPhase.Up,
                    time));
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL when sdlEvent.wheel.which != SDL_TOUCH_MOUSEID:
                var steps = sdlEvent.wheel.direction == SDL_MouseWheelDirection.SDL_MOUSEWHEEL_FLIPPED ? -sdlEvent.wheel.y : sdlEvent.wheel.y;
                _frame.Add(new WheelSample(steps, time));
                break;

            // Finding V104 of the codebase review of 2026-11-06 (d) (TASK-0243): Android cancels a finger that the system
            // takes, as for the gesture of the back from the edge. Until TASK-0243 no sample ended it, so the finger stayed
            // down in the intent layer, and each later finger was a second contact that gave no tap.
            case SDL_EventType.SDL_EVENT_FINGER_DOWN or SDL_EventType.SDL_EVENT_FINGER_MOTION or SDL_EventType.SDL_EVENT_FINGER_UP
                or SDL_EventType.SDL_EVENT_FINGER_CANCELED:
                var finger = sdlEvent.tfinger;
                var phase = sdlEvent.Type switch
                {
                    SDL_EventType.SDL_EVENT_FINGER_DOWN => PointerPhase.Down,
                    SDL_EventType.SDL_EVENT_FINGER_UP => PointerPhase.Up,
                    SDL_EventType.SDL_EVENT_FINGER_CANCELED => PointerPhase.Cancel,
                    _ => PointerPhase.Move,
                };
                // SDL gives a finger in the range 0 to 1 of the window.
                _frame.Add(new PointerSample(
                    DeviceKind.Touch, (ulong)finger.touchID, (ulong)finger.fingerID,
                    finger.x * windowWidth, finger.y * windowHeight, finger.pressure,
                    PointerButtons.None, _modifiers, phase, time));
                break;

            case SDL_EventType.SDL_EVENT_KEY_DOWN or SDL_EventType.SDL_EVENT_KEY_UP:
                Key(sdlEvent.key.scancode, sdlEvent.key.key, sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN, time);
                break;

            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                var text = NativeStrings.Read(sdlEvent.text.text);
                if (text.Length > 0)
                    _frame.Add(new TextSample(text, time));
                break;

            case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                _modifiers = KeyModifiers.None;
                _frame.Add(new FocusLostSample(time));
                break;
        }
    }

    private PointerSample Mouse(SDL_MouseID which, float x, float y, PointerButtons buttons, PointerPhase phase, ulong time)
        => new(DeviceKind.Mouse, (ulong)which, 0, x, y, 1, buttons, _modifiers, phase, time);

    private void Key(SDL_Scancode code, SDL_Keycode keycode, bool down, ulong time)
    {
        if (code is SDL_Scancode.SDL_SCANCODE_LSHIFT or SDL_Scancode.SDL_SCANCODE_RSHIFT)
        {
            _modifiers = down ? _modifiers | KeyModifiers.Shift : _modifiers & ~KeyModifiers.Shift;
            return;
        }

        if (code is SDL_Scancode.SDL_SCANCODE_LCTRL or SDL_Scancode.SDL_SCANCODE_RCTRL
            || (_macOS && code is SDL_Scancode.SDL_SCANCODE_LGUI or SDL_Scancode.SDL_SCANCODE_RGUI))
        {
            _modifiers = down ? _modifiers | KeyModifiers.Control : _modifiers & ~KeyModifiers.Control;
            return;
        }

        InputKey? key = code switch
        {
            SDL_Scancode.SDL_SCANCODE_LEFT => InputKey.ArrowLeft,
            SDL_Scancode.SDL_SCANCODE_RIGHT => InputKey.ArrowRight,
            SDL_Scancode.SDL_SCANCODE_UP => InputKey.ArrowUp,
            SDL_Scancode.SDL_SCANCODE_DOWN => InputKey.ArrowDown,
            SDL_Scancode.SDL_SCANCODE_KP_PLUS or SDL_Scancode.SDL_SCANCODE_EQUALS => InputKey.Plus,
            SDL_Scancode.SDL_SCANCODE_KP_MINUS or SDL_Scancode.SDL_SCANCODE_MINUS => InputKey.Minus,
            SDL_Scancode.SDL_SCANCODE_BACKSPACE => InputKey.Backspace,
            SDL_Scancode.SDL_SCANCODE_DELETE => InputKey.Delete,
            SDL_Scancode.SDL_SCANCODE_HOME => InputKey.Home,
            SDL_Scancode.SDL_SCANCODE_END => InputKey.End,
            SDL_Scancode.SDL_SCANCODE_RETURN or SDL_Scancode.SDL_SCANCODE_KP_ENTER => InputKey.Enter,
            SDL_Scancode.SDL_SCANCODE_ESCAPE => InputKey.Escape,
            SDL_Scancode.SDL_SCANCODE_TAB => InputKey.Tab,
            SDL_Scancode.SDL_SCANCODE_H => InputKey.H,
            // ADR-0138 (phase R21b, TASK-0370): the digits of the views by the place of the key, on the top row and on the pad.
            SDL_Scancode.SDL_SCANCODE_0 or SDL_Scancode.SDL_SCANCODE_KP_0 => InputKey.Digit0,
            SDL_Scancode.SDL_SCANCODE_1 or SDL_Scancode.SDL_SCANCODE_KP_1 => InputKey.Digit1,
            SDL_Scancode.SDL_SCANCODE_3 or SDL_Scancode.SDL_SCANCODE_KP_3 => InputKey.Digit3,
            SDL_Scancode.SDL_SCANCODE_7 or SDL_Scancode.SDL_SCANCODE_KP_7 => InputKey.Digit7,
            _ => null,
        };

        // ADR-0032 item 9: Z and Y follow the letter of the layout, not the place of the key, so
        // Control and Z undoes on a QWERTY, an AZERTY and a QWERTZ keyboard.
        if ((uint)keycode == SDLK_Z)
            key = InputKey.Z;
        else if ((uint)keycode == SDLK_Y)
            key = InputKey.Y;
        // ADR-0098 item 1 (the redesign, slice o, TASK-0312): K follows the letter too, so Control and K open the command line.
        else if ((uint)keycode == SDLK_K)
            key = InputKey.K;
        // ADR-0127 item 4 (phase R18g, TASK-0354): F follows the letter too.
        else if ((uint)keycode == SDLK_F)
            key = InputKey.F;
        if (key is { } known)
            _frame.Add(new KeySample(known, down, _modifiers, time));
    }

    private static PointerButtons Buttons(SDL_MouseButtonFlags state)
        => ((state & (SDL_MouseButtonFlags)SDL_BUTTON_LMASK) != 0 ? PointerButtons.Left : PointerButtons.None)
        | ((state & (SDL_MouseButtonFlags)SDL_BUTTON_MMASK) != 0 ? PointerButtons.Middle : PointerButtons.None)
        | ((state & (SDL_MouseButtonFlags)SDL_BUTTON_RMASK) != 0 ? PointerButtons.Right : PointerButtons.None);

    // SDL numbers the buttons 1 (left), 2 (middle) and 3 (right).
    private static PointerButtons Button(byte button) => button switch
    {
        1 => PointerButtons.Left,
        2 => PointerButtons.Middle,
        3 => PointerButtons.Right,
        _ => PointerButtons.None,
    };
}
