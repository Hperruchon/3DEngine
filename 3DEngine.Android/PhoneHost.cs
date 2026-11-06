using Engine.Geometry.Manifold;
using SDL;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using static SDL.SDL3;

namespace ThreeDEngine.Android;

// Phase R14e2 (ADR-0061 items 4 and 9, ADR-0072, TASK-0226): the host of the phone. It composes a session of its own on
// the native backend, with no surface and no file, and draws it with the application in one window, as the desktop
// draws one session. The person sends each command through the CommandBus of this process. The picker, the loss of the
// surface, the pre-rotation and the pause come in later slices of R14e; no device ran this slice.
internal static class PhoneHost
{
    public static unsafe void Run()
    {
        try
        {
            using var host = new LocalSessionHost(new ManifoldGeometryBackend());
            var window = new Window("3DEngine", 1080, 2316);
            using var engine = new NativeThreeDEngine(window, host);

            engine.Initialize();
            engine.LoadScene(SessionScene.Create());
            window.Show();

            var running = true;
            var lastFrame = DateTime.UtcNow;
            while (running)
            {
                // A frame draws nothing while the window has no area, as on the desktop (finding V23).
                if (window.HasNoArea)
                    Window.WaitForEvent(100);

                SDL_Event sdlEvent;
                while (SDL_PollEvent(&sdlEvent))
                {
                    if (sdlEvent.Type is SDL_EventType.SDL_EVENT_QUIT or SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
                    {
                        running = false;
                        break;
                    }

                    engine.HandleEvent(sdlEvent);
                }

                var now = DateTime.UtcNow;
                engine.Update(now - lastFrame);
                engine.Render();
                lastFrame = now;
            }
        }
        catch (Exception ex)
        {
            // The log of Android receives the standard error of the program.
            Console.Error.WriteLine($"3DEngine: the session of the phone ended: {ex}");
        }
    }
}
