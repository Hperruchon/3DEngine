using SDL3;
using ThreeDEngine.Core.Services;
using ThreeDEngine.Vulkan;
using static SDL3.SDL3;

namespace ThreeDEngine.Desktop;

internal static class DesktopEngineHost
{
    // The surface starts before the window, so that an agent can reach the
    // document as soon as the window shows, and it stops when the window closes
    // (ADR-0019 item 6). The exit code is 0, or a code of DesktopSurface.
    public static int Run(string[] args)
    {
        var (surface, exitCode, message) = DesktopSurface.Start(args);
        if (surface is null)
        {
            Console.Error.WriteLine(message);
            return exitCode;
        }

        using (surface)
        {
            Console.WriteLine($"3DEngine serves the document at {surface.Address}");

            try
            {
                var scene = SampleSceneFactory.CreateDefault();
                var window = new Window("3DEngine Desktop", 1280, 720);
                using var engine = new NativeThreeDEngine(window);

                engine.Initialize();
                engine.LoadScene(scene);
                window.Show();

                bool running = true;
                DateTime lastFrame = DateTime.UtcNow;

                while (running)
                {
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

        return 0;
    }
}
