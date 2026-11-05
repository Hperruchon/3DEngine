using SDL3;
using ThreeDEngine.Vulkan;
using static SDL3.SDL3;

namespace ThreeDEngine.Desktop;

internal static class DesktopEngineHost
{
    // The surface starts before the window, so that an agent can reach the
    // document as soon as the window shows, and it stops when the window closes
    // (ADR-0019 item 6). When the surface stops first, for example at Ctrl+C, the
    // window closes too. The exit code is 0, or a code of DesktopSurface.
    public static int Run(string[] args)
    {
        Window? window = null;
        return RunSessions(args, Start, surface => Show(window ??= new Window("3DEngine Desktop", 1280, 720), surface));
    }

    // A failure before the start of the surface, for example a configuration that
    // the framework cannot read, gives the code of a failed start (TASK-0067).
    internal static (DesktopSurface? Surface, int ExitCode, string? Message) Start(string[] args, string? file)
    {
        try
        {
            return DesktopSurface.Start(args, file);
        }
        catch (Exception ex)
        {
            return (null, DesktopSurface.ExitStartFailed, $"The surface did not start: {ex.Message}");
        }
    }

    // Phase R14b (ADR-0061 item 9, TASK-0194), which amends ADR-0030 item 4: the sessions of the desktop, one after the
    // other, in one process. An open that a check accepted ends the window loop of a session. The surface stops, so the
    // port is free, the host disposes the session, and the next session holds the file of the open on the same port and
    // in the same window. Until R14b the program started again with --file. The exit code is the code of the first
    // session that does not start, or 0.
    internal static int RunSessions(
        string[] args,
        Func<string[], string?, (DesktopSurface? Surface, int ExitCode, string? Message)> start,
        Action<DesktopSurface> show)
    {
        string? file = null;
        while (true)
        {
            var (surface, exitCode, message) = start(args, file);
            if (surface is null)
            {
                Console.Error.WriteLine(message);
                return exitCode;
            }

            string? requested;
            using (surface.Session)
            {
                using (surface)
                {
                    Console.WriteLine($"3DEngine serves the document at {surface.Address}");
                    Console.WriteLine($"3DEngine document file: {surface.Host.Session.FilePath ?? "none"}");
                    show(surface);
                    requested = surface.Document.RequestedOpen;
                }
            }

            if (requested is null)
                return 0;
            Console.WriteLine($"3DEngine opens {requested}");
            file = requested;
        }
    }

    // The window loop of one session, until the window closes, the surface stops, or an open is accepted.
    private static void Show(Window window, DesktopSurface surface)
    {
        try
        {
            var scene = SessionScene.Create();
            using var engine = new NativeThreeDEngine(window, surface.Host, synchronizationValidation: surface.SynchronizationValidation, address: surface.Address, document: surface.Document, assets: surface.Assets);

            engine.Initialize();
            engine.LoadScene(scene);
            window.Show();

            bool running = true;
            DateTime lastFrame = DateTime.UtcNow;

            // ADR-0030 item 3: an open that a check accepted ends the loop.
            while (running && !surface.Stopping.IsCancellationRequested && surface.Document.RequestedOpen is null)
            {
                // A frame draws nothing while the window has no area, so the loop
                // waits for an event, at most 100 ms so that a stop of the surface
                // still closes the window. Until TASK-0079 the loop polled again at
                // once, and a minimised window took one processor core (finding V23
                // of the codebase review of 2026-10-17).
                if (window.HasNoArea)
                    Window.WaitForEvent(100);

                while (SDL_PollEvent(out SDL_Event sdlEvent))
                {
                    if (sdlEvent.type == SDL_EventType.Quit)
                    {
                        running = false;
                        break;
                    }

                    if (sdlEvent.type == SDL_EventType.WindowCloseRequested)
                    {
                        running = false;
                        break;
                    }

                    engine.HandleEvent(sdlEvent);
                }

                DateTime now = DateTime.UtcNow;
                engine.Update(now - lastFrame);
                engine.Render();
                lastFrame = now;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
