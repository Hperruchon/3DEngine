using Engine.Api.Http;
using SDL3;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// The render loop of the desktop (roadmap phases R3 to R7). At each frame the scene
// renderer brings the live bodies of the session into the scene and draws it into the
// swapchain of the window (SceneRenderer). SdlInput turns each event of the window into
// samples, and the intent layer turns the samples of a frame into intents, which move the
// camera and keep the point of a selection (ADR-0026). Until R7 the host read a left drag
// and the wheel itself.
public sealed class NativeThreeDEngine : IThreeDEngine, IDisposable
{
    private readonly SdlInput _input = new();
    private readonly IntentMachine _intents;

    private readonly Window _window;
    private readonly EngineHost _host;
    private GraphicsDevice? _graphicsDevice;
    private SceneRenderer? _renderer;

    public NativeThreeDEngine(Window window, EngineHost host, IntentSettings? settings = null, bool synchronizationValidation = false)
    {
        _window = window;
        _host = host;
        _intents = new IntentMachine(settings);
        _synchronizationValidation = synchronizationValidation;
    }

    // The validation layer also checks the synchronization (finding V16 of the codebase
    // review of 2026-10-17, TASK-0079). Until TASK-0079 the desktop turned on the core
    // checks only, so the present path never ran under the checks of synchronization.
    private readonly bool _synchronizationValidation;

    // The point of the last select intent, in pixels of the window: a state of the client
    // that R7 keeps and draws nothing for (ADR-0026 item 6).
    public (double X, double Y)? Selection { get; private set; }

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public Camera Camera { get; private set; } = new();

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window, _synchronizationValidation);
        Console.WriteLine($"Validation: {(_graphicsDevice.ValidationEnabled ? "on" : "off")}; synchronization validation: {(_graphicsDevice.SynchronizationValidationEnabled ? "on" : "off")}.");
        _renderer = new SceneRenderer(_graphicsDevice, _graphicsDevice.Presenter!.RenderPass, _host);
    }

    public void Update(TimeSpan deltaTime)
    {
        _ = deltaTime;
    }

    // One event of the window becomes samples of the next frame.
    public void HandleEvent(in SDL_Event sdlEvent)
    {
        var size = _window.Extent;
        _input.Translate(sdlEvent, size.width, size.height);
    }

    // The intents of the samples of one frame, applied to the camera and the selection.
    private void ApplyIntents()
    {
        foreach (var intent in _intents.Step(_input.TakeFrame()))
        {
            switch (intent)
            {
                case OrbitIntent orbit:
                    Camera.Orbit(orbit.YawDegrees, orbit.PitchDegrees);
                    break;
                case PanIntent pan:
                    Camera.Pan(pan.DxPixels, pan.DyPixels, Math.Max(1, _window.Extent.height));
                    break;
                case ZoomIntent zoom:
                    Camera.Zoom(zoom.Factor);
                    break;
                case SelectIntent select:
                    Selection = (select.X, select.Y);
                    break;
            }
        }
    }

    public void Render()
    {
        if (_graphicsDevice?.Presenter is not { } presenter || _renderer is null || CurrentScene is null)
        {
            throw new InvalidOperationException("Graphics device is not initialized.");
        }

        ApplyIntents();
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
