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
        // A failure before the start of the surface, for example a configuration that
        // the framework cannot read, gives the code of a failed start (TASK-0067).
        DesktopSurface? surface;
        int exitCode;
        string? message;
        try
        {
            (surface, exitCode, message) = DesktopSurface.Start(args);
        }
        catch (Exception ex)
        {
            (surface, exitCode, message) = (null, DesktopSurface.ExitStartFailed, $"The surface did not start: {ex.Message}");
        }

        if (surface is null)
        {
            Console.Error.WriteLine(message);
            return exitCode;
        }

        using (surface)
        {
            Console.WriteLine($"3DEngine serves the document at {surface.Address}");
            Console.WriteLine($"3DEngine document file: {surface.Host.Session.FilePath ?? "none"}");

            try
            {
                var scene = SessionScene.Create();
                var window = new Window("3DEngine Desktop", 1280, 720);
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

        // ADR-0030 item 4: the surface is disposed, so the port is free, and the program
        // starts again with the file on the same port.
        if (surface.Document.RequestedOpen is { } requested)
        {
            Console.WriteLine($"3DEngine starts again with {requested}");
            DesktopDocument.StartAgain(args, requested);
        }

        return 0;
    }
}
