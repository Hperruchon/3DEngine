using System.Numerics;
using Engine.Contracts.Sketch;
using SDL;
using ThreeDEngine.Core.Abstractions;
using ThreeDEngine.Core.Gizmo;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Measuring;
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
    private readonly ISessionHost _host;

    // ADR-0030 item 3: the document folder and the open of the desktop, and the result
    // of the last save or open, which the panel shows.
    private readonly IDocumentFiles? _document;
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

    internal NativeThreeDEngine(Window window, ISessionHost host, IntentSettings? settings = null, bool synchronizationValidation = false, string address = "", IDocumentFiles? document = null, Engine.Core.Assets.AssetStore? assets = null)
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

    // ADR-0056 item 6 (phase R13): the distance in effect of the selected body, for its box, so that GetBodyInfo runs one
    // time for each change of the selection or of its solid and not in each frame.
    private (Guid Body, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Still, double? Distance)? _sizeOf;

    // Finding V87 of the codebase review of 2026-11-05 (b) (TASK-0191): the key was the box with the offset of a preview,
    // so a drag of the gizmo queried GetBodyInfo in each frame, against ADR-0056 item 6. The key is the box with no
    // offset: a preview does not change the engine, so a drag queries one time.
    internal static (Guid Body, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Still, double? Distance) SizeOf(
        (Guid Body, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) Still, double? Distance)? known, Guid body, ((double X, double Y, double Z) Min, (double X, double Y, double Z) Max) rest, Func<Guid, double?> query)
        => known is { } kept && kept.Body == body && kept.Still == rest ? kept : (body, rest, query(body));

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

        // ADR-0083 D1 and D6 (TASK-0259): a new part with an import applies it first; else the first session of the
        // program shows the home screen.
        if (_document is { } files)
        {
            if (files.TakeImport() is { } import)
                Import(import);
            else if (files.Home)
                _ui.ShowHome(files.Recent(RecentCount));
        }
    }

    // The count of the recent files of the home screen.
    private const int RecentCount = 5;

    // ADR-0083 D1: the import of the new part document. The engine reads the bytes of the store again, so its refusal is
    // the message of the open.
    private void Import(StlImport import)
    {
        var (body, error) = StlOpening.Import(_host.Session, import).GetAwaiter().GetResult();
        if (body is { } imported)
        {
            _actions?.Select(imported);
            _ui?.ShowDimensionsOn();
            _documentMessage = $"Opened {import.Name}";
        }
        else
            _ui?.ShowMessage((error?.Code ?? string.Empty, $"{import.Name}: {error?.Message}"));
    }

    // ADR-0083 D1 and D3: the file that the dialog gave, read through SDL and checked with the reader of the engine. A
    // fault gives the message with "Choose another file" and no new document; a good file asks for a new part document
    // with its import.
    private void TakeOpenedFile()
    {
        var (done, path, failure) = Window.TakeOpenedFile();
        if (!done || _ui is null)
            return;
        // Finding V131 of the codebase review of 2026-11-07 (b) (TASK-0271): a dialog that fails gives its reason; a dialog
        // that the person closes with no file gives nothing.
        if (path is null)
        {
            if (failure is not null)
                _ui.ShowMessage((string.Empty, $"The file dialog did not open: {failure}"));
            return;
        }

        var name = StlOpening.FileName(path);
        if (StlOpening.TooLarge(Window.FileSize(path)) is { } tooLarge)
        {
            _ui.ShowMessage((tooLarge.Code, $"{name}: {tooLarge.Message}"));
            return;
        }

        // E183 (TASK-0281): the read stops after the limit, also when SDL does not know the size and FileSize gives -1.
        var (content, readError) = Window.LoadFile(path, StlOpening.MaximumBytes);
        if (content is null)
        {
            _ui.ShowMessage((string.Empty, $"The file {name} could not be read: {readError}"));
            return;
        }

        if (_assets is null || _document is null)
        {
            _ui.ShowMessage((string.Empty, "This program has no store of the assets, so it opens no STL file."));
            return;
        }

        var (import, fault) = StlOpening.Prepare(path, content, _assets).GetAwaiter().GetResult();
        if (import is null)
        {
            _ui.ShowMessage((fault?.Code ?? string.Empty, $"{name}: {fault?.Message}"));
            return;
        }

        // V125 (TASK-0270): a request that an open of another file refuses gives its message, and the home screen stays.
        if (_document.RequestNew(import) is { } refused)
        {
            _ui.ShowMessage((refused.Code, $"{name}: {refused.Message}"));
            return;
        }

        _ui.ShowMessage(null);
        _document.Home = false;
    }

    // The frame of the camera on the live bodies, as the first body gives it (ADR-0029 item 8).
    private void FrameBodies()
    {
        var window = _window.Extent;
        if (_renderer?.Bounds is { } box && window.height > 0)
            Camera.Frame(box.Min, box.Max, (float)window.width / window.height);
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

            // ADR-0046 item 4: during a calibration a tap of the scene is a point of the image, in each mode.
            if (_imageActions is not null && _images is not null
                && _imageActions.Tap(intent, Camera, window.width, window.height, _images.Live))
                continue;

            // ADR-0053 item 6: an armed tap of the mode Scene starts a sketch on the face under it.
            if (_ui is not null && _ui.FaceArm.Take(intent, out var face))
            {
                _documentMessage = SketchOnFace(face.X, face.Y);
                continue;
            }

            // A select, a hide and a show all go to the selection and to the session (ADR-0029). ADR-0077 item 3: near an
            // edge of the selected face, a select gives the edge.
            if (_actions is not null && _renderer is not null && _actions.Apply(intent, Pick, _renderer.LiveBodies, EdgeAt))
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
        // Finding V130 of the codebase review of 2026-11-07 (b) (TASK-0271): no gizmo behind the home screen, and none on a
        // mesh body, which no move takes (ADR-0083 item 4).
        if (_ui?.HomeShown == true || _renderer.LiveBodies.FirstOrDefault(live => live.Id == body)?.Kind == Engine.Core.Commands.ImportStlCommandHandler.MeshKind)
            return null;
        return _gizmo.Layout(Camera, window.width, window.height, _renderer.Pivot(body));
    }

    // ADR-0053 item 6: the pick under the tap, then the face, the datum and the sketch. It gives the message of a tap
    // that hits no body or of a refusal, or null.
    private string? SketchOnFace(double x, double y)
    {
        var window = _window.Extent;
        if (_renderer is null || _sketchActions is null || window.width == 0 || window.height == 0)
            return null;
        if (Picker.Nearest(Picker.RayThrough(Camera, x, y, window.width, window.height), _renderer.PickBodies) is not { } hit)
            return "No body is under the tap, so no sketch started.";
        if (_sketchActions.CreateOnFace(hit.Id, hit.Point, hit.Normal) is not { } created)
            return _sketchActions.Message;
        Face(created.Frame);
        return null;
    }

    // ADR-0075 item 3: a sketch on the selected face, from the point and the normal of its entry of GetFaces, as the armed
    // tap of ADR-0053 item 6 makes it. A curved face has no normal and no button.
    private string? SketchOnSelectedFace()
    {
        if (_sketchActions is null || _renderer is null || Selection is not { } body || _actions?.SelectedFace(_renderer.KeyOf(body)) is not { Normal: { } normal } face)
            return null;
        if (_sketchActions.CreateOnFace(body, (face.Point.X, face.Point.Y, face.Point.Z), (normal.X, normal.Y, normal.Z)) is not { } created)
            return _sketchActions.Message;
        Face(created.Frame);
        return null;
    }

    // ADR-0077 item 3 (phase R12b, TASK-0245): the edge of the selected face nearest the point, at most 12 pixels away.
    private (DatumReference First, DatumReference Second)? EdgeAt(double x, double y)
    {
        var window = _window.Extent;
        if (_actions is null || _renderer is null || Selection is not { } body || window.width == 0 || window.height == 0)
            return null;
        return FaceOverlay.Nearest(_actions.EdgesOfFace(_renderer.KeyOf(body)), Camera, window.width, window.height, x, y) is { } edge
            ? (edge.First, edge.Second)
            : null;
    }

    // Finding V118 (TASK-0252): the insets of the safe area of the window, for the panel and the fixed controls.
    private static Vector4 Inset((float Left, float Top, float Right, float Bottom) inset)
        => new(inset.Left, inset.Top, inset.Right, inset.Bottom);

    // The nearest visible body under a point of the window, or null (ADR-0029 item 1), with the point and the normal of
    // the hit, so that a second tap names the face (ADR-0075 item 2).
    private PickHit? Pick(double x, double y)
    {
        var window = _window.Extent;
        if (_renderer is null || window.width == 0 || window.height == 0)
            return null;

        return Picker.Nearest(Picker.RayThrough(Camera, x, y, window.width, window.height), _renderer.PickBodies);
    }

    // The actions of the controls of the panel, as commands or as a change of the selection.
    private void ApplyPanel(IReadOnlyList<LiveBody> live)
    {
        if (_ui is null || _actions is null)
            return;

        // ADR-0053 item 6 and findings V77 and V84: the panel applies each action to the arm of the button Face.
        TakeOpenedFile();
        var actions = _ui.TakeActions();
        _documentMessage = _ui.ArmMessage ?? _documentMessage;
        foreach (var action in actions)
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
                    _documentMessage = OpenDocument(open.Name, out var openRefused);
                    // Finding V127 of the codebase review of 2026-11-07 (b) (TASK-0271): the home screen hides the line of the
                    // document, so a refusal of an open from a recent row shows in the message there.
                    if (openRefused is not null && _ui.HomeShown)
                        _ui.ShowMessage(openRefused);
                    break;
                // ADR-0083 D1 and D6 (TASK-0259): the dialog of the system, a new part, the home screen and the frame.
                case OpenFileAction:
                    if (!_window.OpenFile("STL file", "stl"))
                    {
                        _documentMessage = "A file dialog is open already.";
                        if (_ui.HomeShown)
                            _ui.ShowMessage((string.Empty, _documentMessage));
                    }

                    break;
                case NewPartAction when _document is not null:
                    if (_document.RequestNew(null) is { } refused)
                        _ui.ShowMessage((refused.Code, refused.Message));
                    else
                        _document.Home = false;
                    break;
                case HomeAction when _document is not null:
                    _document.Home = true;
                    _ui.ShowHome(_document.Recent(RecentCount));
                    break;
                case FrameAction:
                    FrameBodies();
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
                case CreateCylinderAction cylinder:
                    _ui.EndPad(_actions.CreateCylinder(cylinder.Radius, cylinder.Height));
                    break;
                case MoveBodyAction move:
                    _ui.EndPad(_actions.Move(move.Body, move.Dx, move.Dy, move.Dz));
                    break;
                case CutBodyAction cut:
                    _documentMessage = _actions.Cut(cut.Body, cut.Tool);
                    break;
                case SketchOnSelectedFaceAction:
                    _documentMessage = SketchOnSelectedFace();
                    break;
                case EditDistanceAction editDistance when _actions.Distance(editDistance.Body) is { } distance:
                    _ui.OpenDistancePad(editDistance.Body, distance);
                    break;
                case SetDistanceAction setDistance:
                    _ui.EndPad(_actions.SetDistance(setDistance.Body, setDistance.Distance));
                    break;
                case SetBodyValueAction bodyValue when _sketchActions is not null:
                    _ui.EndPad(_sketchActions.SetValueOf(bodyValue.Sketch, bodyValue.Constraint, bodyValue.Value));
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
            case SketchOnFaceAction:
                // The panel applied the action to the arm (DesktopUi.TakeActions).
                break;
            case NewSketchAction create:
                if (sketch.Create(create.Datum) is not null)
                    Face(create.Datum);
                break;
            case EditSketchAction edit:
                sketch.Edit(edit.Sketch);
                // ADR-0085 items 1 and 2 (TASK-0273): a sketch that makes a live extrude moves under its body, and the camera
                // does not move; another sketch turns the camera to its plane, as before (ADR-0053 item 6).
                _lifted = BodyOf(edit.Sketch) is { } made ? (edit.Sketch, made) : null;
                if (_lifted is null && _sketches?.Live.FirstOrDefault(live => live.SketchId == edit.Sketch) is { } info && SketchOverlay.PlaneOf(info) is { } plane)
                    Face(plane);
                break;
            case FinishSketchAction:
                sketch.Finish();
                _lifted = null;
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

    // ADR-0034 item 6 (Q4 A): the camera looks along the normal of the datum of a new active sketch. An origin plane
    // goes through the origin.
    private void Face(Guid datum)
    {
        if (Datums.Find(datum) is { } plane)
            SketchCamera.Face(Camera, (plane.Normal.X, plane.Normal.Y, plane.Normal.Z), (0, 0, 0));
    }

    // ADR-0085 item 1 (TASK-0273): the sketch and the body of the edit of a sketch that makes a live extrude, or null.
    private (Guid Sketch, Guid Body)? _lifted;

    // ADR-0085 item 1: the live extrude that the sketch makes, or null: the feature of the kind Extrude that reads the
    // sketch, in effect, not consumed, and with a body that the renderer holds.
    private Guid? BodyOf(Guid sketch)
    {
        var features = _host.Session.Query<Engine.Core.Queries.FeaturesInfo>(new Engine.Core.Queries.GetFeaturesQuery()).GetAwaiter().GetResult().Result;
        var extrude = features?.Features.FirstOrDefault(feature => feature.Kind == "Extrude" && feature.Inputs.Contains(sketch)
            && feature.ConsumedBy is null && feature.State == "correct");
        return extrude is not null && _renderer?.BoxOf(extrude.FeatureId) is not null ? extrude.FeatureId : null;
    }

    // ADR-0053 item 6: the camera looks along the normal of a frame of the Document too, from the side of its plane
    // where the eye is (finding V76).
    private void Face(DatumFrame frame)
        => SketchCamera.Face(Camera, (frame.Normal.X, frame.Normal.Y, frame.Normal.Z), (frame.Origin.X, frame.Origin.Y, frame.Origin.Z));

    // A save in the serial section of the session, on the render loop (ADR-0030 item 3).
    private string SaveDocument(string name)
    {
        if (_document is null)
            return "The desktop has no document folder.";
        var path = _document.Resolve(name, out var refusal);
        if (path is null)
            return refusal!;
        var warnings = new List<Engine.Contracts.Diagnostic>();
        var error = _host.Session.SaveAs(path, warnings).GetAwaiter().GetResult();
        if (error is not null)
            return $"{error.Code}: {error.Message}";
        // Finding E164 (TASK-0251): the status line names an old file that stays.
        var saved = $"Saved as {Path.GetFileName(path)}";
        return warnings.Count == 0 ? saved : $"{saved}. {string.Join(" ", warnings.Select(w => $"{w.Code}: {w.Message}"))}";
    }

    // ADR-0048 item 6: the live bodies as one STL file in the document folder, with the name of the field and the
    // extension .stl; the rules of a name are the rules of a document file (ADR-0030 item 2).
    private string ExportStl(string name)
    {
        if (_document is null)
            return "The desktop has no document folder.";
        var path = StlPath(_document, name, out var refusal);
        if (path is null)
            return refusal!;
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

    // ADR-0041 item 6: the mark shows where a tap goes. Finding V74 of the codebase review of 2026-11-03 (TASK-0170):
    // during a calibration a tap goes to the image as the raw pixel under it, and not to the snap of the sketch tool,
    // so the mark does not show.
    internal static (double X, double Y)? MarkPoint((double X, double Y)? hover, ImageActions? images)
        => images?.Calibrating is null ? hover : null;

    // The line of the document in the panel. Finding V99 of the codebase review of 2026-11-06 (c) (TASK-0232): with no
    // IDocumentFiles the panel had no line, so the rejection of a cut, of an Undo or of a sketch on a face showed nothing
    // on the phone, although ADR-0073 item 2 sends it to the line of the document.
    internal static DocumentLine LineOf(IDocumentFiles? files, string? fileName, string? message)
        => new(fileName, message, Files: files is { HasFolder: true });

    // The path of the STL file of a name. Finding V72 of the codebase review of 2026-11-01 (TASK-0162):
    // Resolve adds .3de, so until TASK-0162 the name "part.stl" gave the file "part.stl.stl".
    internal static string? StlPath(IDocumentFiles files, string name, out string? refusal)
    {
        if (name.EndsWith(".stl", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        var path = files.Resolve(name, out refusal);
        return path is null ? null : Path.ChangeExtension(path, ".stl");
    }

    // The check of the file; when it opens, the window loop ends and the program starts
    // again with it (ADR-0030 items 4 and 5).
    private string OpenDocument(string name, out (string Code, string Text)? refused)
    {
        refused = null;
        if (_document is null)
            return "The desktop has no document folder.";
        var path = _document.Resolve(name, out var refusal);
        if (path is null)
        {
            refused = (string.Empty, refusal!);
            return refusal!;
        }

        var error = _document.Open(path, CancellationToken.None).GetAwaiter().GetResult();
        if (error is not null)
            refused = (error.Code, $"{Path.GetFileName(path)}: {error.Message}");
        return error is null ? $"Opening {Path.GetFileName(path)}" : $"{error.Code}: {error.Message}";
    }

    // V106 (TASK-0243): true when the last Render submitted a frame.
    public bool Drew => _graphicsDevice?.Presenter?.Drew ?? false;

    public void Render()
    {
        if (_graphicsDevice?.Presenter is not { } presenter || _renderer is null || _ui is null || _uiRenderer is null || CurrentScene is null)
        {
            throw new InvalidOperationException("Graphics device is not initialized.");
        }

        ApplyIntents();
        var extent = presenter.Swapchain.Extent;
        var scene = CurrentScene;
        var renderer = _renderer;
        _images?.Apply((asset, picture) => renderer.Upload(asset, (uint)picture.Width, (uint)picture.Height, picture.Rgba));
        var images = _images?.Quads();

        // Finding V86 of the codebase review of 2026-11-05 (b) (TASK-0191): the far plane holds the live images too.
        _renderer.Update(scene, Camera, (float)extent.width / extent.height, images);

        // The samples are in pixels of the window; the swapchain can have more pixels.
        var window = _window.Extent;
        var display = new Vector2(window.width, window.height);
        var scale = window.width > 0 && window.height > 0
            ? new Vector2((float)extent.width / window.width, (float)extent.height / window.height)
            : Vector2.One;
        _actions?.Forget(_renderer.LiveBodies);
        _sketches?.Apply();
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
            // ADR-0085 items 2 to 4 (TASK-0273): the move of the active sketch from the box of its body now, the sketches
            // with the moved plane, and a line from each moved point to its place on the plane of the record.
            var record = _lifted is { } lifted && sketchActions.Active == lifted.Sketch
                ? _sketches.Live.FirstOrDefault(live => live.SketchId == lifted.Sketch)
                : null;
            sketchActions.Lift = record is not null && SketchOverlay.PlaneOf(record) is { } recordPlane && _renderer.BoxOf(_lifted!.Value.Body) is { } liftBox
                ? SketchLift.Under(record.SketchId, recordPlane, liftBox.Min, liftBox.Max)
                : null;
            var lift = sketchActions.Lift;
            sketches = SketchOverlay.Of(lift is null ? _sketches.Live : [.. _sketches.Live.Select(lift.Apply)], Camera, window.width, window.height, focus);
            if (lift is not null && record is not null)
                sketches = sketches with { Lines = [.. sketches.Lines, .. lift.Links(record, Camera, window.width, window.height)] };
            if (MarkPoint(_hover, _imageActions) is { } hover && sketchActions.Mark(Camera, hover.X, hover.Y, window.width, window.height) is { } mark)
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

        // ADR-0056 items 3 to 6 (phase R13): the dimension overlay while its toggle in the panel is on.
        DimensionOverlay.Strokes? dimensions = null;
        string? size = null;
        if (_ui.ShowDimensions)
        {
            var unit = _host.Session.LengthUnit;
            var selectedBox = Selection is { } body ? _renderer.BoxOf(body) : null;
            // ADR-0083 D4 (TASK-0259): STL has no unit, so the size of a mesh body says that millimetres are assumed.
            var sizeUnit = Selection is { } meshBody && _renderer.LiveBodies.FirstOrDefault(live => live.Id == meshBody)?.Kind == Engine.Core.Commands.ImportStlCommandHandler.MeshKind
                ? $"{unit} (assumed)"
                : unit;
            dimensions = DimensionOverlay.Of(_renderer.Union(_ui.HiddenToo), selectedBox, Camera, window.width, window.height, unit);
            if (Selection is { } chosen && selectedBox is { } box && _renderer.RestBoxOf(chosen) is { } rest && _actions is { } actions)
            {
                _sizeOf = SizeOf(_sizeOf, chosen, rest, actions.Distance);
                size = DimensionOverlay.SizeText(box, _sizeOf.Value.Distance, sizeUnit);
            }
        }

        // ADR-0075 item 3 (phase R12a, TASK-0236): the outline and the line of the selected face.
        // V109 (TASK-0243): the entry comes again only for a new geometry key of the body; V107: the outline follows a drag.
        FaceLine? faceLine = null;
        if (Selection is { } faceBody && _actions?.SelectedFace(_renderer.KeyOf(faceBody)) is { } selectedFace)
        {
            var drag = _gizmo.Active is null ? default : _gizmo.Offset;
            var selectedEdge = _actions.SelectedEdge(_renderer.KeyOf(faceBody));
            faceLine = new FaceLine(FaceOverlay.Text(selectedFace, _host.Session.LengthUnit), selectedFace.Normal is not null,
                FaceOverlay.Of(selectedFace.Outline, Camera, window.width, window.height, drag),
                selectedEdge is null ? null : FaceOverlay.EdgeText(selectedEdge, _host.Session.LengthUnit),
                selectedEdge is null ? null : FaceOverlay.Of(selectedEdge.Segments, Camera, window.width, window.height, drag));
        }

        // ADR-0081 (TASK-0253): the centre of the box of the selected body on the screen, for the tool ring. ADR-0086 item 1
        // (TASK-0275): while a sketch is active, the centre of its points as the person sees them, or the middle of the
        // window for a sketch with no point.
        Vector2? ringCentre = null;
        var ringSize = 0f;
        if (_sketchActions?.Active is { } ringSketch && _sketches?.Live.FirstOrDefault(live => live.SketchId == ringSketch) is { } ringRecord)
        {
            var seen = _sketchActions.Lift?.Apply(ringRecord) ?? ringRecord;
            var pixels = seen.Points.Select(point => SketchOverlay.Pixel(seen, Camera, window.width, window.height, (point.X, point.Y))).OfType<Vector2>().ToList();
            if (pixels.Count == 0)
                ringCentre = new Vector2(window.width / 2f, window.height / 2f);
            else
            {
                var (low, high) = (pixels.Aggregate(Vector2.Min), pixels.Aggregate(Vector2.Max));
                ringCentre = (low + high) / 2f;
                ringSize = Vector2.Distance(low, high) / 2f;
            }
        }
        else if (Selection is { } ringBody && _renderer.BoxOf(ringBody) is { } ringBox)
        {
            var middle = ((ringBox.Min.X + ringBox.Max.X) / 2, (ringBox.Min.Y + ringBox.Max.Y) / 2, (ringBox.Min.Z + ringBox.Max.Z) / 2);
            ringCentre = SketchOverlay.Segment(Camera, window.width, window.height, middle, middle)?.A;
            // The half of the diagonal of the box on the screen, so the ring stays outside the body.
            if (SketchOverlay.Segment(Camera, window.width, window.height, ringBox.Min, ringBox.Max) is { } diagonal)
                ringSize = Vector2.Distance(diagonal.A, diagonal.B) / 2f;
        }

        // ADR-0082 (TASK-0254): the values of the selected extrude on its edges, with no drag of the gizmo and no fault.
        IReadOnlyList<EdgeValues.Value>? values = null;
        if (_actions is not null && _gizmo.Active is null && Selection is { } valueBody
            && _renderer.LiveBodies.FirstOrDefault(body => body.Id == valueBody) is { Kind: "Extrude", Fault: null })
        {
            var key = _renderer.KeyOf(valueBody);
            values = EdgeValues.Of(valueBody, _actions.DistanceOf(valueBody, key), _actions.Edges(key), _sketches?.Live ?? [],
                Camera, window.width, window.height, _host.Session.LengthUnit);
        }

        var panel = _ui.Frame((float)_delta.TotalSeconds, display, scale, _renderer.LiveBodies, Selection, LineOf(_document, _host.FileName, _documentMessage), sketches, sketchPanel, imagePanel, dimensions, size, faceLine, ringCentre, ringSize, Inset(_window.SafeInsets), values);
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
