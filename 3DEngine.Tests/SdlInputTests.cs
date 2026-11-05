using SDL;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Vulkan;
using static SDL.SDL3;

namespace ThreeDEngine.Tests;

// The translation of SDL events into samples (ADR-0026 item 4, TASK-0082), with events
// that the test builds and no window.
public unsafe class SdlInputTests
{
    private static SDL_Event Finger(SDL_EventType type, ulong finger, float x, float y, ulong ms)
    {
        SDL_Event e = default;
        e.type = (uint)(type);
        e.tfinger.timestamp = ms * 1_000_000;
        e.tfinger.touchID = (SDL_TouchID)1UL;
        e.tfinger.fingerID = (SDL_FingerID)finger;
        e.tfinger.x = x;
        e.tfinger.y = y;
        e.tfinger.pressure = 1;
        return e;
    }

    // The mouse event that SDL makes from a touch carries the device SDL_TOUCH_MOUSEID.
    private static SDL_Event SyntheticMotion(float x, float y, float dx, ulong ms)
    {
        SDL_Event e = default;
        e.type = (uint)(SDL_EventType.SDL_EVENT_MOUSE_MOTION);
        e.motion.timestamp = ms * 1_000_000;
        e.motion.which = SDL_TOUCH_MOUSEID;
        e.motion.state = (SDL_MouseButtonFlags)SDL_BUTTON_LMASK;
        e.motion.x = x;
        e.motion.y = y;
        e.motion.xrel = dx;
        return e;
    }

    private static SDL_Event SyntheticButton(SDL_EventType type, float x, float y, ulong ms)
    {
        SDL_Event e = default;
        e.type = (uint)(type);
        e.button.timestamp = ms * 1_000_000;
        e.button.which = SDL_TOUCH_MOUSEID;
        e.button.button = 1;
        e.button.down = type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN;
        e.button.x = x;
        e.button.y = y;
        return e;
    }

    private static SDL_Event Key(SDL_EventType type, SDL_Scancode code)
    {
        SDL_Event e = default;
        e.type = (uint)(type);
        e.key.scancode = code;
        return e;
    }

    // Decision D8: a touch counts one time, also when SDL makes mouse events from it.
    [Fact]
    public void A_Finger_With_Its_Synthetic_Mouse_Events_Gives_One_Orbit()
    {
        var input = new SdlInput();
        var machine = new IntentMachine();
        var intents = new List<Intent>();

        foreach (var frame in new[]
        {
            new[] { Finger(SDL_EventType.SDL_EVENT_FINGER_DOWN, 5, 0.1f, 0.1f, 0), SyntheticButton(SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN, 100, 100, 0) },
            new[] { Finger(SDL_EventType.SDL_EVENT_FINGER_MOTION, 5, 0.12f, 0.1f, 10), SyntheticMotion(120, 100, 20, 10) },
            new[] { Finger(SDL_EventType.SDL_EVENT_FINGER_MOTION, 5, 0.14f, 0.1f, 20), SyntheticMotion(140, 100, 20, 20) },
            new[] { Finger(SDL_EventType.SDL_EVENT_FINGER_UP, 5, 0.14f, 0.1f, 400), SyntheticButton(SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP, 140, 100, 400) },
        })
        {
            foreach (var e in frame)
                input.Translate(e, 1000, 1000);
            intents.AddRange(machine.Step(input.TakeFrame()));
        }

        // Two motions of 20 pixels each: two orbits of 6 degrees, and nothing from the mouse.
        Assert.Equal(2, intents.Count);
        Assert.All(intents, intent => Assert.Equal(-6, ((OrbitIntent)intent).YawDegrees, 3));
    }

    [Fact]
    public void A_Finger_Is_A_Touch_Sample_In_Pixels_Of_The_Window()
    {
        var input = new SdlInput();

        input.Translate(Finger(SDL_EventType.SDL_EVENT_FINGER_DOWN, 9, 0.25f, 0.5f, 3), 800, 600);

        var sample = Assert.IsType<PointerSample>(Assert.Single(input.TakeFrame()));
        Assert.Equal(DeviceKind.Touch, sample.Kind);
        Assert.Equal(9UL, sample.PointerId);
        Assert.Equal((200.0, 300.0), (sample.X, sample.Y));
        Assert.Equal(PointerPhase.Down, sample.Phase);
    }

    // ADR-0027 item 5 (TASK-0084): the text input of SDL becomes one text sample, in UTF-16,
    // and an empty text gives none.
    [Fact]
    public void The_Text_Input_Of_SDL_Is_A_Text_Sample()
    {
        var input = new SdlInput();
        var utf8 = System.Text.Encoding.UTF8.GetBytes("Boîte 1\0");
        var empty = new byte[] { 0 };

        fixed (byte* text = utf8)
        fixed (byte* nothing = empty)
        {
            SDL_Event e = default;
            e.type = (uint)(SDL_EventType.SDL_EVENT_TEXT_INPUT);
            e.text.text = text;
            input.Translate(e, 800, 600);
            e.text.text = nothing;
            input.Translate(e, 800, 600);
        }

        var sample = Assert.IsType<TextSample>(Assert.Single(input.TakeFrame()));
        Assert.Equal("Boîte 1", sample.Text);
    }

    // The keys of text editing keep Control from the key events, as an arrow keeps Shift.
    [Fact]
    public void Control_And_A_Key_Of_Text_Editing_Give_A_Key_With_Control()
    {
        var input = new SdlInput();

        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_LCTRL), 800, 600);
        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_BACKSPACE), 800, 600);
        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_UP, SDL_Scancode.SDL_SCANCODE_LCTRL), 800, 600);
        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_RETURN), 800, 600);

        Assert.Collection(
            input.TakeFrame(),
            sample => Assert.Equal((InputKey.Backspace, KeyModifiers.Control), (((KeySample)sample).Key, ((KeySample)sample).Modifiers)),
            sample => Assert.Equal((InputKey.Enter, KeyModifiers.None), (((KeySample)sample).Key, ((KeySample)sample).Modifiers)));
    }

    // The state machine of intents reads no text and no key of text editing, so a text field
    // can take them without a move of the camera.
    [Fact]
    public void Text_And_The_Keys_Of_Text_Editing_Give_No_Intent()
    {
        var machine = new IntentMachine();
        var editing = new[] { InputKey.Backspace, InputKey.Delete, InputKey.Home, InputKey.End, InputKey.Enter, InputKey.Escape, InputKey.Tab };
        var frame = new List<InputSample> { new TextSample("a", 0) };
        frame.AddRange(editing.Select(key => new KeySample(key, true, KeyModifiers.None, 1)));

        Assert.Empty(machine.Step(frame));
    }

    // The translation keeps Shift from the key events, so an arrow with Shift pans.
    [Fact]
    public void Shift_And_An_Arrow_Give_A_Key_With_Shift()
    {
        var input = new SdlInput();

        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_LSHIFT), 800, 600);
        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_LEFT), 800, 600);
        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_UP, SDL_Scancode.SDL_SCANCODE_LSHIFT), 800, 600);
        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_LEFT), 800, 600);

        Assert.Collection(
            input.TakeFrame(),
            sample => Assert.Equal(KeyModifiers.Shift, ((KeySample)sample).Modifiers),
            sample => Assert.Equal(KeyModifiers.None, ((KeySample)sample).Modifiers));
    }

    // ADR-0032 item 9: Z and Y follow the letter of the layout. On an AZERTY keyboard the place of
    // W gives the letter z and the place of Z gives w; on a QWERTZ keyboard the place of Z gives y.
    [Fact]
    public void Z_And_Y_Follow_The_Letter_And_Not_The_Place_Of_The_Key()
    {
        var input = new SdlInput();

        input.Translate(Key(SDL_EventType.SDL_EVENT_KEY_DOWN, SDL_Scancode.SDL_SCANCODE_LCTRL), 800, 600);
        input.Translate(Letter(SDL_Scancode.SDL_SCANCODE_W, SDLK_Z), 800, 600);
        input.Translate(Letter(SDL_Scancode.SDL_SCANCODE_Z, SDLK_W), 800, 600);
        input.Translate(Letter(SDL_Scancode.SDL_SCANCODE_Z, SDLK_Y), 800, 600);

        Assert.Collection(
            input.TakeFrame(),
            sample => Assert.Equal((InputKey.Z, KeyModifiers.Control), (((KeySample)sample).Key, ((KeySample)sample).Modifiers)),
            sample => Assert.Equal((InputKey.Y, KeyModifiers.Control), (((KeySample)sample).Key, ((KeySample)sample).Modifiers)));
    }

    private static SDL_Event Letter(SDL_Scancode code, uint letter)
    {
        var e = Key(SDL_EventType.SDL_EVENT_KEY_DOWN, code);
        e.key.key = (SDL_Keycode)letter;
        return e;
    }

    private static SDL_Event MouseMotion(float x, float y, bool held)
    {
        SDL_Event e = default;
        e.type = (uint)(SDL_EventType.SDL_EVENT_MOUSE_MOTION);
        e.motion.which = (SDL_MouseID)1;
        e.motion.state = held ? (SDL_MouseButtonFlags)SDL_BUTTON_LMASK : 0;
        e.motion.x = x;
        e.motion.y = y;
        return e;
    }

    private static SDL_Event MouseButton(SDL_EventType type, float x, float y)
    {
        SDL_Event e = default;
        e.type = (uint)(type);
        e.button.which = (SDL_MouseID)1;
        e.button.button = 1;
        e.button.down = type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN;
        e.button.x = x;
        e.button.y = y;
        return e;
    }

    // Finding V85 of the codebase review of 2026-11-05 (b) (TASK-0191): a hand gives a motion before each press, so SDL
    // gives the press at its place. A click at (500, 300) and a motion of the hand to (560, 300) in the same poll select
    // at the place of the click. TASK-0188 took the place of the cursor at the poll, and the select came at (560, 300).
    [Fact]
    public void A_Click_Keeps_The_Place_Of_Its_Event_When_The_Hand_Moves_On_In_The_Same_Poll()
    {
        var input = new SdlInput();
        var machine = new IntentMachine();

        input.Translate(MouseMotion(500, 300, held: false), 800, 600);
        input.Translate(MouseButton(SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN, 500, 300), 800, 600);
        input.Translate(MouseButton(SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP, 500, 300), 800, 600);
        input.Translate(MouseMotion(560, 300, held: false), 800, 600);

        Assert.Equal(new SelectIntent(500, 300), Assert.Single(machine.Step(input.TakeFrame()).OfType<SelectIntent>()));
    }

    // Register entry R-0043 and finding V85 (TASK-0188, TASK-0191): the rule of the hook of Window. A button message whose
    // place is not the place that SDL holds gives its place, as signed words of lParam; the same place, and each other
    // message, give nothing.
    [Theory]
    [InlineData(0x0201u, 300, 200, 300f, 200f, false)]
    [InlineData(0x0201u, 300, 200, 50f, 60f, true)]
    [InlineData(0x0202u, 300, 200, 50f, 60f, true)]
    [InlineData(0x0203u, 300, 200, 50f, 60f, true)]
    [InlineData(0x0204u, 300, 200, 50f, 60f, true)]
    [InlineData(0x0208u, 300, 200, 50f, 60f, true)]
    [InlineData(0x0209u, 300, 200, 50f, 60f, true)]
    [InlineData(0x0201u, -5, -7, 0f, 0f, true)]
    [InlineData(0x0200u, 300, 200, 50f, 60f, false)]
    [InlineData(0x020Au, 300, 200, 50f, 60f, false)]
    public void A_Button_Message_At_Another_Place_Than_The_Place_Of_SDL_Gives_Its_Place(uint message, int x, int y, float heldX, float heldY, bool corrected)
    {
        var lParam = (nint)(((y & 0xFFFF) << 16) | (x & 0xFFFF));

        Assert.Equal(corrected ? ((float X, float Y)?)((float)x, (float)y) : null, Window.Correction(message, lParam, heldX, heldY));
    }
}
