using SDL3;
using ThreeDEngine.Core.Services;
using Vortice.Vulkan;
using static SDL3.SDL3;

namespace ThreeDEngine.Desktop;

internal static class DesktopEngineHost
{
    public static void Run()
    {
        try
        {
            var scene = SampleSceneFactory.CreateDefault();
            var window = new Window("3DEngine Desktop", 1280, 720);
            var engine = new NativeThreeDEngine(window);

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
}
