using System.Numerics;
using Engine.Api.Http;
using SDL3;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Desktop.Ui;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop;

// The render loop of the desktop (roadmap phases R3 to R7). At each frame the scene
// renderer brings the live bodies of the session into the scene and draws it into the
// swapchain of the window (SceneRenderer). SdlInput turns each event of the window into
// samples, and the intent layer turns the samples of a frame into intents, which move the
// camera and keep the point of a selection (ADR-0026). Until R7 the host read a left drag
// and the wheel itself. The user interface takes each sample first, and the host draws its
// panel after the scene, in the same render pass (ADR-0027, TASK-0084).
public sealed class NativeThreeDEngine : IThreeDEngine, IDisposable
{
    private readonly SdlInput _input = new();
    private readonly IntentMachine _intents;

    private readonly Window _window;
    private readonly EngineHost _host;
    private readonly string _address;
    private GraphicsDevice? _graphicsDevice;
    private SceneRenderer? _renderer;
    private UiRenderer? _uiRenderer;
    private DesktopUi? _ui;
    private TimeSpan _delta;

    public NativeThreeDEngine(Window window, EngineHost host, IntentSettings? settings = null, bool synchronizationValidation = false, string address = "")
    {
        _window = window;
        _host = host;
        _address = address;
        _intents = new IntentMachine(settings);
        _synchronizationValidation = synchronizationValidation;
    }

    // The validation layer also checks the synchronization (finding V16 of the codebase
    // review of 2026-10-17, TASK-0079). Until TASK-0079 the desktop turned on the core
    // checks only, so the present path never ran under the checks of synchronization.
    private readonly bool _synchronizationValidation;

    // The selected body, a state of the client (ADR-0007, ADR-0029 item 1). Until R8b the
    // desktop kept the point of the last select intent and drew nothing for it.
    public Guid? Selection => _actions?.Selection;

    private BodyActions? _actions;

    public string Name => "3DEngine Desktop Runtime";

    public Scene? CurrentScene { get; private set; }

    public Camera Camera { get; private set; } = new();

    public void Initialize()
    {
        _graphicsDevice = new GraphicsDevice(Name, enableValidation: true, _window, _synchronizationValidation);
        Console.WriteLine($"Validation: {(_graphicsDevice.ValidationEnabled ? "on" : "off")}; synchronization validation: {(_graphicsDevice.SynchronizationValidationEnabled ? "on" : "off")}.");
        _renderer = new SceneRenderer(_graphicsDevice, _graphicsDevice.Presenter!.RenderPass, _host);

        _uiRenderer = new UiRenderer(_graphicsDevice, _graphicsDevice.Presenter.RenderPass);
        _ui = new DesktopUi(_address);
        var (width, height, rgba) = _ui.FontAtlas();
        _ui.SetFontTexture(_uiRenderer.CreateTexture(width, height, rgba));
        _actions = new BodyActions(_host.Session);
    }

    public void Update(TimeSpan deltaTime)
    {
        _delta = deltaTime;
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
        var frame = _input.TakeFrame();
        foreach (var intent in _intents.Step(_ui is null ? frame : _ui.Route(frame)))
        {
            // A select, a hide and a show all go to the selection and to the session (ADR-0029).
            if (_actions is not null && _renderer is not null && _actions.Apply(intent, Pick, _renderer.LiveBodies))
                continue;

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
            }
        }
    }

    // The nearest visible body under a point of the window, or null (ADR-0029 item 1).
    private Guid? Pick(double x, double y)
    {
        var window = _window.Extent;
        if (_renderer is null || window.width == 0 || window.height == 0)
            return null;

        return Picker.Nearest(Picker.RayThrough(Camera, x, y, window.width, window.height), _renderer.PickBodies)?.Id;
    }

    // The actions of the controls of the panel, as commands or as a change of the selection.
    private void ApplyPanel(IReadOnlyList<LiveBody> live)
    {
        if (_ui is null || _actions is null)
            return;

        foreach (var action in _ui.TakeActions())
        {
            switch (action)
            {
                case SelectBodyAction select:
                    _actions.Select(select.Body);
                    break;
                case RenameBodyAction rename:
                    _actions.Rename(rename.Body, rename.Name);
                    break;
                case SetVisibleAction visible:
                    _actions.SetVisible(visible.Body, visible.Visible);
                    break;
                case ShowAllAction:
                    _actions.ShowAll(live);
                    break;
            }
        }
    }

    public void Render()
    {
        if (_graphicsDevice?.Presenter is not { } presenter || _renderer is null || _ui is null || _uiRenderer is null || CurrentScene is null)
        {
            throw new InvalidOperationException("Graphics device is not initialized.");
        }

        ApplyIntents();
        var extent = presenter.Swapchain.Extent;
        var scene = CurrentScene;
        _renderer.Update(scene, Camera, (float)extent.width / extent.height);

        // The samples are in pixels of the window; the swapchain can have more pixels.
        var window = _window.Extent;
        var display = new Vector2(window.width, window.height);
        var scale = window.width > 0 && window.height > 0
            ? new Vector2((float)extent.width / window.width, (float)extent.height / window.height)
            : Vector2.One;
        _actions?.Forget(_renderer.LiveBodies);
        var panel = _ui.Frame((float)_delta.TotalSeconds, display, scale, _renderer.LiveBodies, Selection);
        ApplyPanel(_renderer.LiveBodies);

        var ui = _uiRenderer;
        var selected = Selection;
        presenter.RenderFrame((commandBuffer, framebuffer, frameExtent) =>
            _renderer.Record(commandBuffer, framebuffer, frameExtent, scene, Camera,
                overlay => ui.Record(overlay, frameExtent, panel, presenter.CurrentImage), selected));
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
        // The scene renderer waits until the device is idle, so the renderer of the user
        // interface can go after it.
        _renderer?.Dispose();
        _renderer = null;
        _uiRenderer?.Dispose();
        _uiRenderer = null;
        _ui?.Dispose();
        _ui = null;
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
