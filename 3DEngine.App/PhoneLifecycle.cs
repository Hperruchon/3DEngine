using SDL;

namespace ThreeDEngine.Desktop;

// ADR-0073 item 3 (phase R14e3, TASK-0227): the steps of the life of the program on the phone. Android destroys the
// surface of the window when the program goes into the background, so the host ends the engine, with its device and
// its surface, at the sign of the background, and makes a new engine when the program is in the foreground again.
// The session and its document stay through each step. Finding V101 of the codebase review of 2026-11-06 (c)
// (TASK-0232): SDL blocks the thread of the program while it is paused, so the loop reads the sign of the background at
// the return, and the engine ends then, just before the new engine starts.
public enum PhoneStep
{
    // The event goes to the engine.
    None,

    // The program goes into the background: the engine ends. With the block of SDL this comes at the return.
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
