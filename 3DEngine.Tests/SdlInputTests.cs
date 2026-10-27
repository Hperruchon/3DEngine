using SDL3;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Vulkan;
using static SDL3.SDL3;

namespace ThreeDEngine.Tests;

// The translation of SDL events into samples (ADR-0026 item 4, TASK-0082), with events
// that the test builds and no window.
public class SdlInputTests
{
    private static SDL_Event Finger(SDL_EventType type, ulong finger, float x, float y, ulong ms)
    {
        SDL_Event e = default;
        e.type = type;
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
        e.type = SDL_EventType.MouseMotion;
        e.motion.timestamp = ms * 1_000_000;
        e.motion.which = (SDL_MouseID)SDL_TOUCH_MOUSEID;
        e.motion.state = SDL_MouseButtonFlags.Left;
        e.motion.x = x;
        e.motion.y = y;
        e.motion.xrel = dx;
        return e;
    }

    private static SDL_Event SyntheticButton(SDL_EventType type, float x, float y, ulong ms)
    {
        SDL_Event e = default;
        e.type = type;
        e.button.timestamp = ms * 1_000_000;
        e.button.which = (SDL_MouseID)SDL_TOUCH_MOUSEID;
        e.button.button = 1;
        e.button.down = type == SDL_EventType.MouseButtonDown;
        e.button.x = x;
        e.button.y = y;
        return e;
    }

    private static SDL_Event Key(SDL_EventType type, SDL_Scancode code)
    {
        SDL_Event e = default;
        e.type = type;
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
            new[] { Finger(SDL_EventType.FingerDown, 5, 0.1f, 0.1f, 0), SyntheticButton(SDL_EventType.MouseButtonDown, 100, 100, 0) },
            new[] { Finger(SDL_EventType.FingerMotion, 5, 0.12f, 0.1f, 10), SyntheticMotion(120, 100, 20, 10) },
            new[] { Finger(SDL_EventType.FingerMotion, 5, 0.14f, 0.1f, 20), SyntheticMotion(140, 100, 20, 20) },
            new[] { Finger(SDL_EventType.FingerUp, 5, 0.14f, 0.1f, 400), SyntheticButton(SDL_EventType.MouseButtonUp, 140, 100, 400) },
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

        input.Translate(Finger(SDL_EventType.FingerDown, 9, 0.25f, 0.5f, 3), 800, 600);

        var sample = Assert.IsType<PointerSample>(Assert.Single(input.TakeFrame()));
        Assert.Equal(DeviceKind.Touch, sample.Kind);
        Assert.Equal(9UL, sample.PointerId);
        Assert.Equal((200.0, 300.0), (sample.X, sample.Y));
        Assert.Equal(PointerPhase.Down, sample.Phase);
    }

    // The translation keeps Shift from the key events, so an arrow with Shift pans.
    [Fact]
    public void Shift_And_An_Arrow_Give_A_Key_With_Shift()
    {
        var input = new SdlInput();

        input.Translate(Key(SDL_EventType.KeyDown, SDL_Scancode.LeftShift), 800, 600);
        input.Translate(Key(SDL_EventType.KeyDown, SDL_Scancode.Left), 800, 600);
        input.Translate(Key(SDL_EventType.KeyUp, SDL_Scancode.LeftShift), 800, 600);
        input.Translate(Key(SDL_EventType.KeyDown, SDL_Scancode.Left), 800, 600);

        Assert.Collection(
            input.TakeFrame(),
            sample => Assert.Equal(KeyModifiers.Shift, ((KeySample)sample).Modifiers),
            sample => Assert.Equal(KeyModifiers.None, ((KeySample)sample).Modifiers));
    }
}
