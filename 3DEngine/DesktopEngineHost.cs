using SDL;
using ThreeDEngine.Vulkan;
using static SDL.SDL3;

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
        return RunSessions(args, Start, surface =>
        {
            // Finding V90 (TASK-0198): a later session gets no input of the time between two sessions.
            if (window is null)
                window = new Window("3DEngine Desktop", 1280, 720);
            else
                Window.FlushInput();
            Show(window, surface);
        });
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
    // port is free, the host disposes the session, and the next session holds the file of the open in the same window,
    // on the same port when the port is fixed. Until R14b the program started again with --file. The exit code is the
    // code of a first session that does not start, or 0.
    // Finding E150 of the codebase review of 2026-11-06 (TASK-0197), Q3 A: a next session that does not start ended the
    // run, with the first document already closed. ADR-0030 item 5 keeps the person in the window, so a session on the
    // previous file starts again, and the error goes to the standard error. When that session does not start either, the
    // run ends with its code.
    internal static int RunSessions(
        string[] args,
        Func<string[], string?, (DesktopSurface? Surface, int ExitCode, string? Message)> start,
        Action<DesktopSurface> show)
    {
        string? file = null;
        string? previous = null;
        var (first, again) = (true, false);
        while (true)
        {
            var (surface, exitCode, message) = start(args, file);
            if (surface is null)
            {
                Console.Error.WriteLine(message);
                if (first || again)
                    return exitCode;
                Console.Error.WriteLine($"3DEngine opens {previous ?? "the document of the start"} again");
                (file, again) = (previous, true);
                continue;
            }

            (first, again) = (false, false);
            string? requested;
            using (surface.Session)
            {
                using (surface)
                {
                    Console.WriteLine($"3DEngine serves the document at {surface.Address}");
                    Console.WriteLine($"3DEngine document file: {surface.Host.Session.FilePath ?? "none"}");
                    show(surface);
                    requested = surface.Document.RequestedOpen;
                    previous = surface.Host.Session.FilePath;
                }
            }

            if (requested is null)
                return 0;
            Console.WriteLine($"3DEngine opens {requested}");
            file = requested;
        }
    }

    // The window loop of one session, until the window closes, the surface stops, or an open is accepted.
    private static unsafe void Show(Window window, DesktopSurface surface)
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

                SDL_Event sdlEvent;
                while (SDL_PollEvent(&sdlEvent))
                {
                    if (sdlEvent.Type == SDL_EventType.SDL_EVENT_QUIT)
                    {
                        running = false;
                        break;
                    }

                    if (sdlEvent.Type == SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
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
