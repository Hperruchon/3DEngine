using Engine.Geometry.Manifold;
using SDL;
using ThreeDEngine.Desktop;
using ThreeDEngine.Vulkan;
using static SDL.SDL3;

namespace ThreeDEngine.Android;

// Phase R14e2 (ADR-0061 items 4 and 9, ADR-0072, TASK-0226): the host of the phone. It composes a session of its own on
// the native backend, with no surface and no file, and draws it with the application in one window, as the desktop
// draws one session. The person sends each command through the CommandBus of this process.
// Phase R14e3 (ADR-0073 item 3, TASK-0227): the session stays through the background. Each time in the foreground has
// its own engine, with its device and its surface, because Android destroys the surface in the background. The picker,
// the pre-rotation and the test of the owner come later; no device ran this program before the test of the owner.
internal static class PhoneHost
{
    public static void Run()
    {
        try
        {
            if (!ManifoldGeometryBackend.IsNativeAvailable())
            {
                Console.Error.WriteLine($"3DEngine: {ManifoldGeometryBackend.UnavailableReason()}");
                return;
            }

            // Phase R14f2 (TASK-0229): the smoke test of the emulator reads this line and the line of the first frame.
            Console.Error.WriteLine($"3DEngine: the session of the phone starts, with Manifold {ManifoldGeometryBackend.NativeVersion}.");
            using var host = new LocalSessionHost(new ManifoldGeometryBackend());
            var window = new Window("3DEngine", 1080, 2316);
            while (Foreground(window, host) && WaitForForeground())
                Console.Error.WriteLine("3DEngine: the program is in the foreground again, with the same document.");
        }
        catch (Exception ex)
        {
            // The log of Android receives the standard error of the program. Each line of the exception has the prefix, so
            // that the smoke test of the emulator gives the stack as public annotations (TASK-0229: the third run of the
            // job ended with PlatformNotSupportedException and showed no stack).
            Console.Error.WriteLine($"3DEngine: the session of the phone ended: {ex.GetType().Name}: {ex.Message}");
            foreach (var line in ex.ToString().Split('\n'))
                Console.Error.WriteLine($"3DEngine: | {line.TrimEnd()}");
        }
    }

    // The loop of one time in the foreground. False at a quit or a close, true when the program goes into the background.
    private static unsafe bool Foreground(Window window, LocalSessionHost host)
    {
        using var engine = new NativeThreeDEngine(window, host);
        engine.Initialize();
        engine.LoadScene(SessionScene.Create());
        window.Show();

        var lastFrame = DateTime.UtcNow;
        var first = true;
        while (true)
        {
            // A frame draws nothing while the window has no area, as on the desktop (finding V23).
            if (window.HasNoArea)
                Window.WaitForEvent(100);

            SDL_Event sdlEvent;
            while (SDL_PollEvent(&sdlEvent))
            {
                switch (PhoneLifecycle.Of(sdlEvent.Type))
                {
                    case PhoneStep.Quit:
                        return false;
                    case PhoneStep.Background:
                        Console.Error.WriteLine("3DEngine: the program goes into the background; the document stays.");
                        return true;
                }

                engine.HandleEvent(sdlEvent);
            }

            var now = DateTime.UtcNow;
            engine.Update(now - lastFrame);
            engine.Render();
            lastFrame = now;
            if (first)
                Console.Error.WriteLine("3DEngine: the first frame is drawn.");
            first = false;
        }
    }

    // The wait in the background, until the program is in the foreground again (true) or ends (false).
    private static unsafe bool WaitForForeground()
    {
        SDL_Event sdlEvent;
        while (SDL_WaitEvent(&sdlEvent))
        {
            switch (PhoneLifecycle.Of(sdlEvent.Type))
            {
                case PhoneStep.Quit:
                    return false;
                case PhoneStep.Foreground:
                    return true;
            }
        }

        return false;
    }
}
