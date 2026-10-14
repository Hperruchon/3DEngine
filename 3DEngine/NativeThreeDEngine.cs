using Engine.Api.Http;
using SDL3;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// The render loop of the desktop (roadmap phases R3 to R6). At each frame the scene
// renderer brings the live bodies of the session into the scene and draws it into the
// swapchain of the window (SceneRenderer). A left drag of the mouse orbits the camera,
// and the wheel zooms it; R7 replaces this reading of the mouse with the intent layer.
public sealed class NativeThreeDEngine : IThreeDEngine, IDisposable
{
    // Degrees of orbit for each pixel of a drag, and the zoom of one step of the wheel.
    private const double OrbitDegreesPerPixel = 0.3;
    private const double ZoomPerStep = 0.9;

    private readonly Window _window;
    private readonly EngineHost _host;
    private GraphicsDevice? _graphicsDevice;
    private SceneRenderer? _renderer;

    public NativeThreeDEngine(Window window, EngineHost host)
    {
        _window = window;
        _host = host;
    }

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public Camera Camera { get; private set; } = new();

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window);
        _renderer = new SceneRenderer(_graphicsDevice, _graphicsDevice.Presenter!.RenderPass, _host);
    }

    public void Update(TimeSpan deltaTime)
    {
        _ = deltaTime;
    }

    // One event of the window: a left drag orbits the camera, and the wheel zooms it.
    public void HandleEvent(in SDL_Event sdlEvent)
    {
        if (sdlEvent.type == SDL_EventType.MouseMotion && (sdlEvent.motion.state & SDL_MouseButtonFlags.Left) != 0)
            Camera.Orbit(-sdlEvent.motion.xrel * OrbitDegreesPerPixel, sdlEvent.motion.yrel * OrbitDegreesPerPixel);
        else if (sdlEvent.type == SDL_EventType.MouseWheel)
            Camera.Zoom(Math.Pow(ZoomPerStep, sdlEvent.wheel.y));
    }

    public void Render()
    {
        if (_graphicsDevice?.Presenter is not { } presenter || _renderer is null || CurrentScene is null)
        {
            throw new InvalidOperationException("Graphics device is not initialized.");
        }

        var extent = presenter.Swapchain.Extent;
        var scene = CurrentScene;
        _renderer.Update(scene, Camera, (float)extent.width / extent.height);
        presenter.RenderFrame((commandBuffer, framebuffer, frameExtent) =>
            _renderer.Record(commandBuffer, framebuffer, frameExtent, scene, Camera));
    }

    // The scene gives the camera when it holds one; else the camera keeps its state.
    public void LoadScene(Scene scene)
    {
        CurrentScene = scene;
        Camera = scene.Cameras.FirstOrDefault() ?? Camera;
        Console.WriteLine($"Loaded scene '{scene.Name}' with {scene.Entities.Count} entities.");
    }

    public void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
