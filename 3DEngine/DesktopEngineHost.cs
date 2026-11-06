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
            // Finding V92 of the codebase review of 2026-11-06 (b) (TASK-0225): since R14b the window was made here,
            // outside the try of Show, so a window that SDL cannot create ended the desktop with an unhandled exception.
            var later = window is not null;
            try
            {
                window ??= new Window("3DEngine Desktop", 1280, 720);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"The window did not open: {ex.Message}");
                return false;
            }

            return Show(window, surface, later);
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
    // code of a first session that does not start, ExitWindowFailed when the window or its loop fails (V92), or 0.
    // Finding E150 of the codebase review of 2026-11-06 (TASK-0197), Q3 A: a next session that does not start ended the
    // run, with the first document already closed. ADR-0030 item 5 keeps the person in the window, so a session on the
    // previous file starts again, and the error goes to the standard error. When that session does not start either, the
    // run ends with its code.
    // Finding V91 of the codebase review of 2026-11-06 (b) (TASK-0225), Q2 A: the start again kept the path of the previous
    // document only, and opened a document of the unsaved folder as a named file, or ended the window when its journal
    // did not exist yet. The host keeps the kind of the previous document: a named file opens again by its path, and a
    // document of the unsaved folder starts again from its journal, as an unsaved document, or as a new one with no command.
    internal static int RunSessions(
        string[] args,
        Func<string[], string?, (DesktopSurface? Surface, int ExitCode, string? Message)> start,
        Action<DesktopSurface> show)
        => RunSessions(args, start, surface =>
        {
            show(surface);
            return true;
        });

    internal static int RunSessions(
        string[] args,
        Func<string[], string?, (DesktopSurface? Surface, int ExitCode, string? Message)> start,
        Func<DesktopSurface, bool> show)
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
            bool shown;
            using (surface.Session)
            {
                using (surface)
                {
                    Console.WriteLine($"3DEngine serves the document at {surface.Address}");
                    Console.WriteLine($"3DEngine document file: {surface.Host.Session.FilePath ?? "none"}");
                    shown = show(surface);
                    requested = surface.Document.RequestedOpen;
                    // V91: a document of the unsaved folder has no name; it starts again from its journal when the journal
                    // exists, and as a new document of the start when it does not.
                    var path = surface.Host.Session.FilePath;
                    previous = surface.Host.FileName is null && (path is null || !File.Exists(path)) ? null : path;
                }
            }

            if (!shown)
                return DesktopSurface.ExitWindowFailed;
            if (requested is null)
                return 0;
            Console.WriteLine($"3DEngine opens {requested}");
            file = requested;
        }
    }

    // The window loop of one session, until the window closes, the surface stops, or an open is accepted. False when the
    // loop failed (V92): until TASK-0225 the run then gave the exit code 0.
    private static unsafe bool Show(Window window, DesktopSurface surface, bool later)
    {
        try
        {
            var scene = SessionScene.Create();
            using var engine = new NativeThreeDEngine(window, new HostedSession(surface.Host), synchronizationValidation: surface.SynchronizationValidation, address: surface.Address, document: surface.Document, assets: surface.Assets);

            engine.Initialize();
            engine.LoadScene(scene);
            // Finding V93 of the codebase review of 2026-11-06 (b) (TASK-0225): the flush of V90 ran before the new device,
            // so the input of the time of Initialize still reached the first frame. It runs after the device and the scene.
            if (later)
                Window.FlushInput();
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
            Console.Error.WriteLine($"Error: {ex.Message}");
            return false;
        }

        return true;
    }
}
