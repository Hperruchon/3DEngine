using System.Numerics;
using Engine.Api.Http;
using Engine.Contracts.Sketch;
using SDL3;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Gizmo;
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

    // ADR-0038: the drag of the translate gizmo, and the overlay pipeline that draws it.
    private readonly GizmoActions _gizmo = new();

    // ADR-0041 item 6 (phase R11b): the last point of the mouse with no button in the mode Sketch.
    private (double X, double Y)? _hover;
    private OverlayRenderer? _overlay;

    // ADR-0033 item 13: the live sketches, which the panel draws over the scene.
    private SessionSketches? _sketches;

    // ADR-0034: the active sketch, its tool, and the commands of the sketch mode.
    private SketchActions? _sketchActions;
    private TimeSpan _delta;

    // ADR-0046 items 2 to 4: the store of the assets, the live images, and the window of the images.
    private readonly Engine.Core.Assets.AssetStore? _assets;
    private SessionImages? _images;
    private ImageActions? _imageActions;

    internal NativeThreeDEngine(Window window, EngineHost host, IntentSettings? settings = null, bool synchronizationValidation = false, string address = "", DesktopDocument? document = null, Engine.Core.Assets.AssetStore? assets = null)
    {
        _window = window;
        _document = document;
        _assets = assets;
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
        _overlay = new OverlayRenderer(_graphicsDevice, _graphicsDevice.Presenter.RenderPass);
        _ui = new DesktopUi(_address, _host.Session.LengthUnit);
        var (width, height, rgba) = _ui.FontAtlas();
        _ui.SetFontTexture(_uiRenderer.CreateTexture(width, height, rgba));
        _actions = new BodyActions(_host.Session);
        _sketches = new SessionSketches(_host);
        _sketchActions = new SketchActions(_host.Session);
        _images = new SessionImages(_host, _assets);
        _imageActions = new ImageActions(_host.Session, _assets);
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
        var mode = _sketchActions?.Active is null ? IntentMode.Scene : IntentMode.Sketch;
        // Finding V67 of the codebase review of 2026-10-31 (c) (TASK-0151): the mark stayed after a change
        // of mode, after a finger lifted and after a cancel; ADR-0041 item 6 shows it while the finger
        // is down.
        if (mode != _intents.Mode)
            _hover = null;
        _intents.Mode = mode;

        // ADR-0038 item 5: the hit test of the gizmo that shows now, or none.
        var window = _window.Extent;
        var layout = GizmoLayout();
        _intents.HitTest = layout is null ? null : (x, y) => TranslateGizmo.Hit(layout, x, y);
        foreach (var intent in _intents.Step(_ui is null ? frame : _ui.Route(frame)))
        {
            // ADR-0038 item 2: a drag of the gizmo moves the drawing, and its end sends one Translate.
            if (_actions is not null && _renderer is not null
                && _gizmo.Apply(intent, Camera, window.width, window.height, Selection, _renderer.Pivot, _renderer.Preview, _actions.Move, out var rejection))
            {
                _documentMessage = rejection ?? _documentMessage;
                continue;
            }

            // ADR-0046 item 4: during a calibration a tap of the scene is a point of the image.
            if (intent is SelectIntent select && _imageActions is not null && _images is not null
                && _imageActions.Tap(Camera, select.X, select.Y, window.width, window.height, _images.Live))
                continue;

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

                // ADR-0041 item 6 (phase R11b): the mouse with no button, for the mark of the snap.
                case SketchHoverIntent hover:
                    _hover = (hover.X, hover.Y);
                    break;
            }
        }

        _hover = HoverMark.After(_hover, frame);
    }

    // ADR-0038 item 7: the gizmo shows on the selected body when the body is live and visible,
    // the mode is Scene, and no pad is open.
    private GizmoLayout? GizmoLayout()
    {
        var window = _window.Extent;
        if (_renderer is null || _ui?.Pad is not null || _sketchActions?.Active is not null || Selection is not { } body
            || window.width == 0 || window.height == 0)
            return null;
        return _gizmo.Layout(Camera, window.width, window.height, _renderer.Pivot(body));
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
                case ExportStlAction export:
                    _documentMessage = ExportStl(export.Name);
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
                case PlaceImageAction place:
                    _imageActions?.Place(place.Path, place.Datum);
                    break;
                case StartCalibrationAction start:
                    _imageActions?.StartCalibration(start.Image);
                    break;
                case CalibrateImageAction calibrate:
                    _imageActions?.Calibrate(calibrate.Distance);
                    break;
                case CancelCalibrationAction:
                    _imageActions?.Cancel();
                    break;
                case ProposeContourAction propose when _images is not null:
                    _imageActions?.Propose(propose.Image, _images.Live);
                    break;
                // ADR-0047 item 4: an accept makes the new sketch the active sketch, so the person edits it.
                case AcceptContourAction when _images is not null:
                    if (_imageActions?.Accept(_images.Live) is { } accepted && _sketchActions is { } editing)
                    {
                        editing.Edit(accepted.Sketch);
                        Face(accepted.Datum);
                    }
                    break;
                case RejectContourAction:
                    _imageActions?.Reject();
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

    // ADR-0048 item 6: the live bodies as one STL file in the document folder, with the name of the field and the
    // extension .stl; the rules of a name are the rules of a document file (ADR-0030 item 2).
    private string ExportStl(string name)
    {
        if (_document is null)
            return "The desktop has no document folder.";
        var path = _document.Folder.Resolve(name, out var refusal);
        if (path is null)
            return refusal!;
        path = Path.ChangeExtension(path, ".stl");
        var (content, error) = _host.Session.ReadGeometry(Engine.Core.Export.StlExport.Write).GetAwaiter().GetResult();
        if (error is not null)
            return $"{error.Code}: {error.Message}";
        try
        {
            File.WriteAllBytes(path, content!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"{Engine.Core.DiagnosticCodes.IoSaveFailed}: {ex.Message}";
        }

        return $"Exported {(content!.Length - 84) / 50} triangles to {Path.GetFileName(path)}";
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
        var renderer = _renderer;
        _images?.Apply((asset, picture) => renderer.Upload(asset, (uint)picture.Width, (uint)picture.Height, picture.Rgba));
        var images = _images?.Quads();
        var imagePanel = _images is not null && _imageActions is { } imageActions
            ? new ImagePanel(_images.Live, imageActions.Calibrating, imageActions.Points.Count, imageActions.Message, imageActions.Proposal?.Pixels.Count ?? 0)
            : null;
        SketchOverlay.Strokes? sketches = null;
        SketchPanel? sketchPanel = null;
        if (_sketches is not null && _sketchActions is { } sketchActions)
        {
            sketchActions.Follow(_sketches.Live);
            var tool = sketchActions.Tool;
            var focus = sketchActions.Active is { } active ? new SketchOverlay.Focus(active, [.. tool.Selection], tool.First) : null;
            sketches = SketchOverlay.Of(_sketches.Live, Camera, window.width, window.height, focus);
            if (_hover is { } hover && sketchActions.Mark(Camera, hover.X, hover.Y, window.width, window.height) is { } mark)
                sketches = sketches with { Snap = mark };
            sketchPanel = new SketchPanel(
                _sketches.Live,
                _sketches.Live.FirstOrDefault(sketch => sketch.SketchId == sketchActions.Active),
                tool.Kind,
                tool.Last is not null,
                tool.Selection,
                sketchActions.Message);
        }

        // ADR-0047 item 3: the lines of a proposal over the scene, as the lines of a sketch.
        if (_images is not null && _imageActions?.ProposalLines(_images.Live, Camera, window.width, window.height) is { Count: > 0 } proposal)
        {
            var strokes = sketches ?? new SketchOverlay.Strokes([], [], [], []);
            sketches = strokes with { Lines = [.. strokes.Lines, .. proposal] };
        }

        var panel = _ui.Frame((float)_delta.TotalSeconds, display, scale, _renderer.LiveBodies, Selection, _document is null ? null : new DocumentLine(_host.FileName, _documentMessage), sketches, sketchPanel, imagePanel);
        ApplyPanel(_renderer.LiveBodies);

        var ui = _uiRenderer;
        var selected = Selection;

        // ADR-0038 item 9: the gizmo after the scene and the highlight, and before the panels.
        var gizmo = GizmoLayout() is { } layout
            ? TranslateGizmo.Shapes(layout, _gizmo.Active).Select(shape => new OverlayDraw(shape.Triangles, shape.Colour)).ToArray()
            : [];
        var overlayRenderer = _overlay;
        presenter.RenderFrame((commandBuffer, framebuffer, frameExtent) =>
            _renderer.Record(commandBuffer, framebuffer, frameExtent, scene, Camera,
                overlay =>
                {
                    overlayRenderer?.Record(overlay, frameExtent, display, gizmo, presenter.CurrentImage);
                    ui.Record(overlay, frameExtent, panel, presenter.CurrentImage);
                }, selected, images));
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
        _overlay?.Dispose();
        _overlay = null;
        _sketches?.Dispose();
        _sketches = null;
        _images?.Dispose();
        _images = null;
        _ui?.Dispose();
        _ui = null;
        _graphicsDevice?.Dispose();
        _graphicsDevice = null;
    }
}
