using SDL;
using ThreeDEngine.Desktop;

namespace ThreeDEngine.ImageTests;

// ADR-0073 item 3 (phase R14e3, TASK-0227): the steps of the life of the program on the phone, from the events of SDL.
// The first sign of the background ends the engine, only the end of the return to the foreground makes a new one, and
// each other event goes to the engine.
public sealed class PhoneLifecycleTests
{
    [Theory]
    [InlineData(SDL_EventType.SDL_EVENT_QUIT, PhoneStep.Quit)]
    [InlineData(SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED, PhoneStep.Quit)]
    [InlineData(SDL_EventType.SDL_EVENT_TERMINATING, PhoneStep.Quit)]
    [InlineData(SDL_EventType.SDL_EVENT_WILL_ENTER_BACKGROUND, PhoneStep.Background)]
    [InlineData(SDL_EventType.SDL_EVENT_DID_ENTER_BACKGROUND, PhoneStep.Background)]
    [InlineData(SDL_EventType.SDL_EVENT_WILL_ENTER_FOREGROUND, PhoneStep.None)]
    [InlineData(SDL_EventType.SDL_EVENT_DID_ENTER_FOREGROUND, PhoneStep.Foreground)]
    [InlineData(SDL_EventType.SDL_EVENT_FINGER_DOWN, PhoneStep.None)]
    [InlineData(SDL_EventType.SDL_EVENT_LOW_MEMORY, PhoneStep.None)]
    public void Each_Event_Gives_Its_Step(SDL_EventType type, PhoneStep step)
        => Assert.Equal(step, PhoneLifecycle.Of(type));
}
