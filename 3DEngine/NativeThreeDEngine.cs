using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts.Sketch;
using SDL3;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Picking;
using ThreeDEngine.Core.Sketching;
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

    // ADR-0030 item 3: the document folder and the open of the desktop, and the result
    // of the last save or open, which the panel shows.
    private readonly DesktopDocument? _document;
    private string? _documentMessage;
    private readonly string _address;
    private GraphicsDevice? _graphicsDevice;
    private SceneRenderer? _renderer;
    private UiRenderer? _uiRenderer;
    private DesktopUi? _ui;

    // ADR-0033 item 13: the live sketches, which the panel draws over the scene.
    private SessionSketches? _sketches;

    // ADR-0034: the active sketch, its tool, and the commands of the sketch mode.
    private SketchActions? _sketchActions;
    private TimeSpan _delta;

    internal NativeThreeDEngine(Window window, EngineHost host, IntentSettings? settings = null, bool synchronizationValidation = false, string address = "", DesktopDocument? document = null)
    {
        _window = window;
        _document = document;
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
        _sketches = new SessionSketches(_host);
        _sketchActions = new SketchActions(_host.Session);
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

        // ADR-0034 item 1: the intent layer is in the mode Sketch while a sketch is active.
        _intents.Mode = _sketchActions?.Active is null ? IntentMode.Scene : IntentMode.Sketch;
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

                // ADR-0032 item 9: the keys and the taps of undo and redo act as the buttons.
                case UndoIntent when _actions is not null:
                    _documentMessage = _actions.Undo();
                    break;
                case RedoIntent when _actions is not null:
                    _documentMessage = _actions.Redo();
                    break;

                // ADR-0034 items 2 to 4: a tap or a click of the mode Sketch goes to the active tool.
                case SketchPointIntent point when _sketchActions is not null:
                    _sketchActions.Tap(Camera, point.X, point.Y, _window.Extent.width, _window.Extent.height);
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
                case SaveDocumentAction save:
                    _documentMessage = SaveDocument(save.Name);
                    break;
                case OpenDocumentAction open:
                    _documentMessage = OpenDocument(open.Name);
                    break;
                case UndoAction:
                    _documentMessage = _actions.Undo();
                    break;
                case RedoAction:
                    _documentMessage = _actions.Redo();
                    break;
                case CreateBoxAction box:
                    _ui.EndPad(_actions.CreateBox(box.X, box.Y, box.Z));
                    break;
                case MoveBodyAction move:
                    _ui.EndPad(_actions.Move(move.Body, move.Dx, move.Dy, move.Dz));
                    break;
                default:
                    ApplySketch(action);
                    break;
            }
        }
    }

    // ADR-0034 items 4 to 8: the actions of the window of the sketch and of its pads.
    private void ApplySketch(PanelAction action)
    {
        if (_ui is null || _sketchActions is not { } sketch)
            return;

        switch (action)
        {
            case NewSketchAction create:
                if (sketch.Create(create.Datum) is not null)
                    Face(create.Datum);
                break;
            case EditSketchAction edit:
                sketch.Edit(edit.Sketch);
                if (_sketches?.Live.FirstOrDefault(live => live.SketchId == edit.Sketch) is { } info)
                    Face(info.DatumId);
                break;
            case FinishSketchAction:
                sketch.Finish();
                break;
            case SketchToolAction tool:
                sketch.Tool.Kind = tool.Kind;
                break;
            case EndChainAction:
                sketch.Tool.End();
                break;
            case CloseChainAction:
                sketch.Close();
                break;
            case NextPointAction next:
                // The pad stays open for the next point of the chain.
                var rejection = sketch.Next(next.A, next.B);
                _ui.EndPad(rejection);
                if (rejection is null && sketch.Tool.Last is not null)
                    _ui.OpenNextPointPad(chainOpen: true);
                break;
            case ToggleSketchItemAction toggle:
                sketch.Tool.Toggle(toggle.Item);
                break;
            case AddConstraintAction constraint:
                var refused = sketch.AddConstraint(constraint.Kind, constraint.Items, constraint.Values);
                if (constraint.Values.Count > 0)
                    _ui.EndPad(refused);
                break;
            case SetConstraintValueAction value:
                _ui.EndPad(sketch.SetValue(value.Constraint, value.Values));
                break;
            case MoveSketchPointAction move:
                _ui.EndPad(sketch.MovePoint(move.Point, move.X, move.Y));
                break;
            case DeleteSketchItemsAction delete:
                sketch.Delete(delete.Items);
                break;
            case ExtrudeAction extrude:
                // ADR-0036 item 9: a success ends the mode, and the new body becomes the selection.
                var (extrudeRejection, body) = sketch.Extrude(extrude.Distance);
                _ui.EndPad(extrudeRejection);
                if (body is { } extruded)
                    _actions?.Select(extruded);
                break;
        }
    }

    // ADR-0034 item 6 (Q4 A): the camera looks along the normal of the datum of a new active sketch.
    private void Face(Guid datum)
    {
        if (Datums.Find(datum) is { } plane)
            SketchCamera.Face(Camera, (plane.Normal.X, plane.Normal.Y, plane.Normal.Z));
    }

    // A save in the serial section of the session, on the render loop (ADR-0030 item 3).
    private string SaveDocument(string name)
    {
        if (_document is null)
            return "The desktop has no document folder.";
        var path = _document.Folder.Resolve(name, out var refusal);
        if (path is null)
            return refusal!;
        var error = _host.Session.SaveAs(path).GetAwaiter().GetResult();
        return error is null ? $"Saved as {Path.GetFileName(path)}" : $"{error.Code}: {error.Message}";
    }

    // The check of the file; when it opens, the window loop ends and the program starts
    // again with it (ADR-0030 items 4 and 5).
    private string OpenDocument(string name)
    {
        if (_document is null)
            return "The desktop has no document folder.";
        var path = _document.Folder.Resolve(name, out var refusal);
        if (path is null)
            return refusal!;
        var error = _document.Folder.Open(path, CancellationToken.None).GetAwaiter().GetResult();
        return error is null ? $"Opening {Path.GetFileName(path)}" : $"{error.Code}: {error.Message}";
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
        _sketches?.Apply();
        SketchOverlay.Strokes? sketches = null;
        SketchPanel? sketchPanel = null;
        if (_sketches is not null && _sketchActions is { } sketchActions)
        {
            sketchActions.Follow(_sketches.Live);
            var tool = sketchActions.Tool;
            var focus = sketchActions.Active is { } active ? new SketchOverlay.Focus(active, [.. tool.Selection], tool.First) : null;
            sketches = SketchOverlay.Of(_sketches.Live, Camera, window.width, window.height, focus);
            sketchPanel = new SketchPanel(
                _sketches.Live,
                _sketches.Live.FirstOrDefault(sketch => sketch.SketchId == sketchActions.Active),
                tool.Kind,
                tool.Last is not null,
                tool.Selection,
                sketchActions.Message);
        }

        var panel = _ui.Frame((float)_delta.TotalSeconds, display, scale, _renderer.LiveBodies, Selection, _document is null ? null : new DocumentLine(_host.FileName, _documentMessage), sketches, sketchPanel);
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
        _sketches?.Dispose();
        _sketches = null;
        _ui?.Dispose();
        _ui = null;
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
