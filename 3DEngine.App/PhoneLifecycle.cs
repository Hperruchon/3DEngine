using SDL;

namespace ThreeDEngine.Desktop;

// ADR-0073 item 3 (phase R14e3, TASK-0227): the steps of the life of the program on the phone. Android destroys the
// surface of the window when the program goes into the background, so the host ends the engine, with its device and
// its surface, at the first sign of the background, and makes a new engine when the program is in the foreground again.
// The session and its document stay through each step.
public enum PhoneStep
{
    // The event goes to the engine.
    None,

    // The program goes into the background: the engine ends before Android destroys its surface.
    Background,

    // The program is in the foreground again: a new engine makes a new surface.
    Foreground,

    // The program ends.
    Quit,
}

internal static class PhoneLifecycle
{
    public static PhoneStep Of(SDL_EventType type) => type switch
    {
        SDL_EventType.SDL_EVENT_QUIT or SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED or SDL_EventType.SDL_EVENT_TERMINATING => PhoneStep.Quit,
        SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND or SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND => PhoneStep.Background,
        SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND => PhoneStep.Foreground,
        _ => PhoneStep.None,
    };
}
