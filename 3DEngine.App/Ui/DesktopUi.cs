using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Contracts.Sketch;
using Engine.Core.Queries;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
using ThreeDEngine.Core.Sketching;
using ThreeDEngine.Vulkan;

namespace ThreeDEngine.Desktop.Ui;

// The user interface of the desktop (ADR-0027, TASK-0084): one context of Dear ImGui, its
// font, its sizes, and the first panel. It sees no SDL, no Vulkan and no window (ADR-0027
// item 2): the host gives it the samples of a frame and the size of the display, and it gives
// back the samples for the state machine of intents and the neutral draw data of the frame.
//
// The panel shows the address of the document and the live bodies that the desktop holds from
// the events of its session (ADR-0024), with a text field that filters them. The filter is a
// state of the client (ADR-0007). The panel reads no state of the engine. A row selects its
// body, and the selected body has a name field and the buttons Rename and Hide or Show, beside
// Show all (ADR-0029 item 7). The panel changes nothing itself: each press of a control gives
// a PanelAction, which the host applies as a command or as a change of the selection.
//
// Dear ImGui keeps one current context for the whole process, not one for each thread, so each
// public method makes this context current first, and only one thread may use the library at a
// time. The desktop has one user interface on its render loop; the tests of the library run in
// one collection, one after the other (TASK-0084).
//
// Phase R15 (TASK-0234): this file holds the labels, the state, the frame and the routing. Each region has a partial file
// of its own: DesktopUi.Panel.cs, DesktopUi.Overlay.cs, DesktopUi.Sketch.cs, DesktopUi.Image.cs and DesktopUi.Pad.cs.
internal sealed unsafe partial class DesktopUi : IDisposable
{
    // A control is at least 44 pixels high at a display scale of 1, so that a finger can hit
    // it (ADR-0027 item 6): the font size plus two times the vertical frame padding.
    public const float FontSize = 26f;
    public const float FramePaddingY = 9f;
    public const float MinimumControlHeight = 44f;

    public const string PanelName = "Document";
    public const string FilterLabel = "Filter";
    public const string ClearLabel = "Clear";
    public const string NameLabel = "Name";
    public const string RenameLabel = "Rename";
    public const string HideLabel = "Hide";
    public const string ShowLabel = "Show";
    public const string ShowAllLabel = "Show all";
    public const string FileNameLabel = "File name";
    public const string SaveAsLabel = "Save as";
    public const string OpenLabel = "Open";
    public const string ExportStlLabel = "Export STL";
    public const string DocumentMessageLabel = "Document message";
    public const string UndoLabel = "Undo";
    public const string RedoLabel = "Redo";
    // Phase K5b (ADR-0076 item 7, TASK-0244): the list "New" holds one entry for each primitive. The key of an entry is
    // the title of its pad.
    public const string NewLabel = "New";
    public const string NewBoxLabel = "New box";
    public const string NewCylinderLabel = "New cylinder";
    // Phase K5e (ADR-0089 item 5, TASK-0298): the cone, the sphere and the torus.
    public const string NewConeLabel = "New cone";
    public const string NewSphereLabel = "New sphere";
    public const string NewTorusLabel = "New torus";
    public const string MoveLabel = "Move";

    // ADR-0073 item 1 (phase R14e3, TASK-0227): the list of the tools of a cut of the selected body.
    public const string CutLabel = "Cut with";

    // The item of one tool in the list of a cut.
    public static string CutItem(Guid tool) => $"{CutLabel}:{tool}";
    public const string SetDistanceLabel = "Set distance";
    public const string FaultLabel = "Fault";

    // ADR-0056 item 2 (phase R13, TASK-0178): the toggles of the dimension overlay, and the line of the size of the
    // selected body.
    public const string DimensionsLabel = "Dimensions";
    public const string HiddenTooLabel = "Hidden too";
    public const string SizeLabel = "Size";
    public const string FaceLabel = "Face";
    public const string SketchOnFaceButtonLabel = "Sketch on face";
    public const string EdgeLabel = "Edge";

    // ADR-0034 items 6 and 8: the buttons of a new sketch in the panel, and the window of the
    // sketch with its controls. A row of the window has the label of its item (SketchText).
    public const string SketchLineLabel = "Sketch on";
    public static readonly (string Label, Guid Datum)[] NewSketchButtons =
        [("Sketch XY", Datums.XY.Id), ("Sketch XZ", Datums.XZ.Id), ("Sketch YZ", Datums.YZ.Id)];

    // ADR-0053 item 6 (phase K0i, TASK-0171): the button that arms a tap on a face for a new sketch.
    public const string SketchOnFaceLabel = "Sketch Face";

    public const string SketchName = "Sketch";
    public const string LineToolLabel = "Line";
    public const string SelectToolLabel = "Select";
    public const string CircleToolLabel = "Circle";
    public const string ArcToolLabel = "Arc";
    public const string FinishLabel = "Finish";
    public const string NextPointLabel = "Next point";
    public const string EndLabel = "End";
    public const string CloseLabel = "Close";
    public const string DeleteLabel = "Delete";
    public const string MovePointLabel = "Move point";
    public const string ValueLabel = "Value";
    public const string EditLabel = "Edit";
    public const string ExtrudeLabel = "Extrude";

    // The five kinds of K2a (ADR-0033 item 3) and their buttons.
    public static readonly (string Label, string Kind)[] ConstraintButtons =
        [("Coincident", "coincident"), ("Horizontal", "horizontal"), ("Vertical", "vertical"), ("Distance", "distance"), ("Fixed", "fixed"),
         ("On curve", "pointOn"), ("Tangent", "tangent"), ("Inside tangent", "tangent"), ("Equal", "equal"), ("Radius", "radius"),
         ("On line", "pointOn"), ("Midpoint", "midpoint"), ("Parallel", "parallel"), ("Perpendicular", "perpendicular"), ("Angle", "angle"), ("Symmetric", "symmetric")];

    // Phase K2f (TASK-0125): On curve takes a point and a curve, and On line a point and a line.
    public const string OnCurveLabel = "On curve";
    public const string OnLineLabel = "On line";

    // ADR-0033 item 3, kind 14: the pad of an angle takes degrees, and the client gives the cosine and
    // the sine to the command, so the log holds no angle (determinism rule 2).
    public const string AngleUnit = "deg";

    // Phase K2e (TASK-0122): Tangent of two curves gives 1 (outside), and Inside tangent gives −1
    // (ADR-0033 item 3, kind 11).
    public const string InsideTangentLabel = "Inside tangent";

    // ADR-0046 items 3 and 4: the window of the images, with the file field, a place on each origin
    // plane, a calibration of each live image, and the distance of a calibration.
    public const string ImagesName = "Images";
    public const string ImageFileLabel = "PNG file";
    public static readonly (string Label, Guid Datum)[] PlaceImageButtons =
        [("Place on XY", Datums.XY.Id), ("Place on XZ", Datums.XZ.Id), ("Place on YZ", Datums.YZ.Id)];
    public const string CalibrateLabel = "Calibrate";
    public const string DistanceLabel = "Distance";
    public const string ApplyCalibrationLabel = "Apply";
    public const string CancelCalibrationLabel = "Cancel calibration";
    public const string ProposeLabel = "Propose contour";
    public const string AcceptLabel = "Accept";
    public const string RejectLabel = "Reject";
    public const string PlaceLineLabel = "Place on";
    private const float ImagesWidth = 280f;
    private const float ImagesHeight = 300f;

    // The key of the rectangle of a control of the window of the sketch, for ItemRectangle.
    public static string SketchItem(string label) => $"{SketchName} {label}";

    // Wide enough for the row of a constraint with its values and its residual.
    private const float SketchWidth = 480f;

    // ADR-0032 item 5: the window of the pad and its buttons, row by row.
    public const string PadName = "Pad";
    private const float PadWidth = 620f;
    private const float PadHeight = 600f;
    public static readonly string[][] PadRows =
    [
        ["7", "8", "9", "(", ")"],
        ["4", "5", "6", "*", "pi"],
        ["1", "2", "3", "-", "sqrt("],
        ["0", ".", "+", "/", "Delete"],
        ["Clear", "Next", "Apply", "Cancel"],
    ];

    private readonly IntPtr _context;
    private readonly string _address;

    // ADR-0015 item 7 and ADR-0032 item 7: the length unit of the document, which the pad shows beside
    // each field. The host gives the unit of its session, which the header of the file gave (P8e,
    // TASK-0119). Until TASK-0119 the pad showed the constant mm.
    private readonly string _unit;
    private readonly UiInputRouter _router = new();
    private readonly List<Vector4> _panels = new();

    // The review of Codex of v0.265 (TASK-0284): the windows of the last frame, so a check in this frame sees the windows that
    // come after it in the frame, as the Images window and the message after the ring.
    private readonly List<Vector4> _lastPanels = new();

    // ADR-0080 (TASK-0248): the rectangles of the fixed controls, which a tap does not pass either.
    private readonly List<Vector4> _fixed = new();
    private readonly Dictionary<string, Vector4> _items = new(StringComparer.Ordinal);
    private readonly List<string> _shown = new();
    private readonly (uint Width, uint Height, byte[] Rgba) _atlas;
    private string _filter = string.Empty;
    private readonly List<PanelAction> _actions = new();

    // The text of the name field, and the body that it was filled for.
    private string _name = string.Empty;

    private string _fileName = string.Empty;

    // ADR-0046: the text of the file field and of the distance field of the window of the images.
    private string _imagePath = string.Empty;
    private string _distance = "100";
    private Guid? _nameFor;
    private bool _textActive;

    // Finding V26 of the codebase review of 2026-10-28 (TASK-0090): a press that starts
    // outside the panel goes to the intents only, so Dear ImGui does not see the click away
    // and keeps its text field. The panel then drops the focus in the next frame, and the
    // keys after the press go to the scene at once. Until TASK-0090 the field kept its
    // buffer, and Rename could rename the body that the click selected with the text of
    // the field.
    private bool _dropFocus;

    // ADR-0032 items 3 and 4, ADR-0034 items 7 and 8: the open pad, the action that its numbers
    // give, and where it opens: beside the panel, or to the left of the window of the sketch.
    private NumericPad? _pad;
    private Func<double[], PanelAction>? _padApply;
    private bool _padBySketch;

    // ADR-0032 item 4: the selection that the open pad names, as a key, or null for a pad that
    // names none. The pad closes when the key of the frame differs. Until TASK-0099 the Move pad
    // moved the body that was selected when it opened (finding V32 of the second codebase review
    // of 2026-10-28).
    // Findings V40 and V41 of the codebase review of 2026-10-29 (TASK-0106): Chain is the active
    // sketch, the tool and the state of the chain, which decide whether the numbers of Next point
    // are a position or an offset; Active is the active sketch, which Extrude acts on.
    private enum PadScope { None, Body, Sketch, Chain, Active }
    private PadScope _padScope;
    private string? _padKey;
    private string _bodyKey = string.Empty;
    private string _sketchKey = string.Empty;
    private string _chainKey = string.Empty;
    private string _activeKey = string.Empty;

    // ADR-0032 item 4: true after an apply, until the host ends the pad, so a second Enter in one
    // frame gives no second command (V34).
    private bool _padApplied;

    // V47: the action that the pad added in this frame, until the scope check of the frame.
    private PanelAction? _padAction;

    public DesktopUi(string address, string lengthUnit)
    {
        ArgumentNullException.ThrowIfNull(lengthUnit);
        if (!UiDrawDataConverter.VertexLayoutAgrees)
            throw new InvalidOperationException("The vertex of Dear ImGui does not have the layout of UiVertex.");

        _address = address;
        _unit = lengthUnit;
        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);

        var io = ImGui.GetIO();

        // No file: the layout of a panel is not a setting that the desktop saves.
        io.NativePtr->IniFilename = null;
        io.NativePtr->LogFilename = null;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        // ADR-0091 (the redesign, slice i, TASK-0292): Instrument Sans is the font of the interface, and JetBrains Mono the
        // font of a value on an edge. The atlas does not own the data, which the interface frees after its context. Until
        // TASK-0292 the font was the default bitmap font of Dear ImGui at a larger size (ADR-0027 item 6).
        _fontData = [FontData("InstrumentSans.ttf"), FontData("JetBrainsMono.ttf")];
        ImFontConfigPtr font = ImGuiNative.ImFontConfig_ImFontConfig();
        font.FontDataOwnedByAtlas = false;
        io.Fonts.AddFontFromMemoryTTF(_fontData[0].Pointer, _fontData[0].Length, FontSize, font);
        _monoFont = io.Fonts.AddFontFromMemoryTTF(_fontData[1].Pointer, _fontData[1].Length, FontSize, font);
        font.Destroy();

        var style = ImGui.GetStyle();
        style.FramePadding = new Vector2(12f, FramePaddingY);
        style.ItemSpacing = new Vector2(12f, 10f);
        style.WindowPadding = new Vector2(14f, 14f);
        style.ScrollbarSize = 24f;
        style.GrabMinSize = 24f;
        style.WindowRounding = 6f;
        style.FrameRounding = 4f;

        // ADR-0078 item 1 (TASK-0246): the colours of the theme of the redesign; the sizes above stay.
        Theme.Apply(style);

        // The atlas is built here: NewFrame refuses a font atlas that is not built, and the
        // native library stops the process on a failed check (TASK-0084).
        _atlas = BuildAtlas(io);
    }

    // ADR-0078 (TASK-0246): a colour of the style of this panel, and its sizes, for a test that may not name the library.
    internal Vector4 StyleColour(ImGuiCol slot)
    {
        ImGui.SetCurrentContext(_context);
        return ImGui.GetStyle().Colors[(int)slot];
    }

    internal (Vector2 FramePadding, float WindowRounding, float FrameRounding) StyleSizes()
    {
        ImGui.SetCurrentContext(_context);
        var style = ImGui.GetStyle();
        return (style.FramePadding, style.WindowRounding, style.FrameRounding);
    }

    // The filter of the panel, as the person typed it.
    public string Filter => _filter;

    // ADR-0056 items 2 and 3: the toggles of the dimension overlay, a state of the client, off at each start.
    private bool _dimensions;
    private bool _hiddenToo;

    public bool ShowDimensions => _dimensions;

    // ADR-0083 (TASK-0259): an import turns the dimensions on, so the name and the size of the part show at once.
    internal void ShowDimensionsOn() => _dimensions = true;

    public bool HiddenToo => _hiddenToo;

    // The text of the size of the selected body for this frame, or null.
    private string? _size;

    // V118 (TASK-0252): the insets of the safe area of the display, as left, top, right and bottom.
    private Vector4 _inset;

    // ADR-0075 item 3 (phase R12a, TASK-0236): the selected face for this frame, or null.
    private FaceLine? _face;

    private string? Key(PadScope scope) => scope switch
    {
        PadScope.Body => _bodyKey,
        PadScope.Sketch => _sketchKey,
        PadScope.Chain => _chainKey,
        PadScope.Active => _activeKey,
        _ => null,
    };

    private static string ChainKey(string active, SketchToolKind tool, bool chainOpen) => $"{active}:{tool}:{chainOpen}";

    // V32: the keys of the selection of this frame, and the end of a pad whose selection changed.
    // A selected body that left the list has the key of no selection.
    private void FollowSelection(IReadOnlyList<LiveBody> bodies, Guid? selection, SketchPanel? sketchPanel)
    {
        _bodyKey = selection is { } id && bodies.Any(body => body.Id == id) ? id.ToString() : string.Empty;
        // Finding V46 of the codebase review of 2026-10-30 (TASK-0118): the key also holds the
        // positions of the selected points, of the ends of a selected line, and the values of a
        // selected constraint, so a pad closes when a value that it shows changes, and its apply
        // never moves a point back after an edit of another client or an undo.
        _sketchKey = sketchPanel?.Active is { } active
            ? active.SketchId + ":" + string.Join(",", sketchPanel.Selection.Order()) + ":" + ShownValues(active, sketchPanel.Selection)
            : string.Empty;
        _activeKey = sketchPanel?.Active?.SketchId.ToString() ?? string.Empty;
        _chainKey = sketchPanel?.Active is null ? string.Empty : ChainKey(_activeKey, sketchPanel.Tool, sketchPanel.ChainOpen);

        var current = Key(_padScope);
        if (_pad is not null && _padKey is not null && current != _padKey)
        {
            // Finding V47 (TASK-0118): an Enter routed in this frame applied the pad before the
            // panel saw the new scope, so its action read old numbers in a new state.
            if (_padAction is { } stale)
                _actions.Remove(stale);
            ClosePad();
        }
        _padAction = null;
    }

    // The positions and the values that a pad of the selection shows, in the order of the selection.
    private static string ShownValues(SketchInfo sketch, IReadOnlyCollection<Guid> selection)
    {
        var values = new List<string>();
        foreach (var id in selection.Order())
        {
            if (sketch.Points.FirstOrDefault(point => point.Id == id) is { } point)
                values.Add(Exact(point.X) + " " + Exact(point.Y));
            else if (sketch.Lines.FirstOrDefault(line => line.Id == id) is { } line)
                values.AddRange(sketch.Points.Where(p => p.Id == line.Start || p.Id == line.End).Select(p => Exact(p.X) + " " + Exact(p.Y)));
            else if (sketch.Constraints.FirstOrDefault(constraint => constraint.Id == id) is { } constraint)
                values.Add(string.Join(" ", constraint.Values.Select(Exact)));
        }
        return string.Join(";", values);

        static string Exact(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    // The rows of bodies that the last frame showed, in their order.
    public IReadOnlyList<string> ShownBodies => _shown;

    // The height of one control: the font size plus two times the frame padding.
    public float ControlHeight
    {
        get
        {
            ImGui.SetCurrentContext(_context);
            return ImGui.GetFontSize() + 2 * ImGui.GetStyle().FramePadding.Y;
        }
    }

    // The rectangle (left, top, right, bottom) of a control of the last frame, or null.
    public Vector4? ItemRectangle(string label) => _items.TryGetValue(label, out var rectangle) ? rectangle : null;

    // The rectangles (left, top, right, bottom) of the panels of the last frame.
    public IReadOnlyList<Vector4> PanelRectangles => _panels;

    public IReadOnlyList<Vector4> FixedRectangles => _fixed;

    // True when a point of the display is over a panel of the last frame.
    //
    // Finding V116 of the codebase review of 2026-11-06 (e) (TASK-0252): while a list of the panel is open, each point
    // belongs to the panel, so a press outside the list goes to the library, which closes the list, and the tap does not
    // reach the scene, as a list of a desktop program does. Until TASK-0252 the press went to the scene, and the list
    // stayed open.
    public bool OverPanel(double x, double y)
        => _cutList is not null || _newList is not null
            || _panels.Concat(_fixed).Any(panel => x >= panel.X && x < panel.Z && y >= panel.Y && y < panel.W) || OverRing(x, y)
            || OverValue(x, y) || OverRows(x, y);

    // The font atlas as RGBA pixels, for the renderer to upload. It is built once.
    public (uint Width, uint Height, byte[] Rgba) FontAtlas() => _atlas;

    private static (uint Width, uint Height, byte[] Rgba) BuildAtlas(ImGuiIOPtr io)
    {
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        var rgba = new byte[width * height * bytesPerPixel];
        Marshal.Copy(pixels, rgba, 0, rgba.Length);
        return ((uint)width, (uint)height, rgba);
    }

    // The identifier of the texture of the font atlas, as the renderer gave it.
    public void SetFontTexture(ulong texture)
    {
        ImGui.SetCurrentContext(_context);
        ImGui.GetIO().Fonts.SetTexID((IntPtr)texture);
    }

    // Gives the samples of the panel to the library, and returns the samples for the state
    // machine of intents (ADR-0027 item 4).
    public IReadOnlyList<InputSample> Route(IEnumerable<InputSample> frame)
    {
        ImGui.SetCurrentContext(_context);
        var samples = frame as IReadOnlyList<InputSample> ?? frame.ToList();
        if (_textActive && samples.Any(sample => sample is PointerSample { Phase: PointerPhase.Down } press && !OverPanel(press.X, press.Y)))
        {
            _textActive = false;
            _dropFocus = true;
        }

        // ADR-0032 item 6: while the pad is open, it takes each key and each text of the frame.
        var (panel, intents) = _router.Route(samples, OverPanel, _textActive || _pad is not null);
        // Finding V130 of the codebase review of 2026-11-07 (b) (TASK-0271): the home screen takes the place of the document,
        // so no sample of the frame reaches the scene or the keys of the document behind it.
        if (_home is not null)
            intents = [];
        var io = ImGui.GetIO();

        foreach (var sample in panel)
        {
            if (_pad is { } pad && sample is KeySample or TextSample)
            {
                PadKey(pad, sample);
                continue;
            }

            switch (sample)
            {
                case PointerSample pointer:
                    io.AddMouseSourceEvent(pointer.Kind switch
                    {
                        DeviceKind.Touch => ImGuiMouseSource.TouchScreen,
                        DeviceKind.Pen => ImGuiMouseSource.Pen,
                        _ => ImGuiMouseSource.Mouse,
                    });
                    io.AddMousePosEvent((float)pointer.X, (float)pointer.Y);
                    Buttons(io, pointer);
                    break;

                case WheelSample wheel:
                    io.AddMouseWheelEvent(0f, (float)wheel.Steps);
                    break;

                case KeySample key:
                    io.AddKeyEvent(ImGuiKey.ModShift, (key.Modifiers & KeyModifiers.Shift) != 0);
                    io.AddKeyEvent(ImGuiKey.ModCtrl, (key.Modifiers & KeyModifiers.Control) != 0);
                    if (Key(key.Key) is { } imguiKey)
                        io.AddKeyEvent(imguiKey, key.Down);
                    break;

                case TextSample text:
                    io.AddInputCharactersUTF8(text.Text);
                    break;

                case FocusLostSample:
                    io.AddFocusEvent(false);
                    break;
            }
        }

        return intents;
    }

    // One frame of the user interface for a display of this size in pixels of the window,
    // and the pixels of the target for one pixel of the display.
    // The actions of the controls of the last frames, in their order; the call empties the list.
    public IReadOnlyList<PanelAction> TakeActions()
    {
        var actions = _actions.ToArray();
        _actions.Clear();

        // Finding V84 of the codebase review of 2026-11-05 (TASK-0181): the host called the arm for each action, and no
        // test drove that call. The panel applies each action to the arm here, with the active sketch of its last frame,
        // and gives the message of the last change of the arm.
        ArmMessage = null;
        foreach (var action in actions)
            ArmMessage = FaceArm.Panel(action, _sketchActive) ?? ArmMessage;
        return actions;
    }

    // ADR-0053 item 6: the arm of the button Face, a state of the client, and the message of its last change.
    public FaceArm FaceArm { get; } = new();

    public string? ArmMessage { get; private set; }

    // True when the sketch panel of the last frame had an active sketch.
    private bool _sketchActive;

    // The text of a row: the name of the body, or its kind and a short identifier, the word
    // "hidden" for a hidden body (ADR-0029 item 7), and the word "fault" for a body whose last
    // rebuild failed (ADR-0037 item 6, K0c).
    public static string RowText(LiveBody body)
        => (body.Name ?? $"{body.Kind}  {body.Id.ToString()[..8]}") + (body.Visible ? string.Empty : "  (hidden)")
        + (body.Fault is null ? string.Empty : "  (fault)");

    public UiDrawData Frame(float deltaSeconds, Vector2 displaySize, Vector2 framebufferScale, IReadOnlyList<LiveBody> bodies, Guid? selection = null, DocumentLine? document = null, SketchOverlay.Strokes? sketches = null, SketchPanel? sketchPanel = null, ImagePanel? imagePanel = null, DimensionOverlay.Strokes? dimensions = null, string? size = null, FaceLine? face = null, Vector2? ringCentre = null, float ringBody = 0f, Vector4 safeInsets = default, IReadOnlyList<EdgeValues.Value>? values = null)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        _size = size;
        _inset = safeInsets;
        _face = face;
        _sketchActive = sketchPanel?.Active is not null;
        _sketchTool = sketchPanel?.Active is not null ? sketchPanel.Tool : null;
        _sketchPanel = sketchPanel;
        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        io.DisplaySize = displaySize;
        io.DisplayFramebufferScale = framebufferScale;
        io.DeltaTime = deltaSeconds > 0 ? deltaSeconds : 1f / 60f;

        _lastPanels.Clear();
        _lastPanels.AddRange(_panels);
        _panels.Clear();
        _fixed.Clear();
        _anchorsTop = float.MaxValue;
        _cutList = null;
        _newList = null;
        _items.Clear();
        _shown.Clear();
        FollowSelection(bodies, selection, sketchPanel);

        ImGui.NewFrame();
        if (_dropFocus)
        {
            ImGui.SetWindowFocus(null);
            _dropFocus = false;
        }

        if (sketches is not null)
            Sketches(sketches);
        if (dimensions is not null)
            Dimensions(dimensions, displaySize);
        if (face is not null)
            FaceOutline(face);
        // ADR-0083 D6 (TASK-0259): the home screen takes the place of the panel and of the controls of a document.
        if (_home is { } recent)
        {
            Home(displaySize, recent);
            _ring = null;
            _chips = [];
            (_rows, _rowsArea) = ([], null);
        }
        else
        {
            Panel(bodies, selection, displaySize, document);
            FixedControls(displaySize, bodies, document);
            // ADR-0084 (TASK-0267): the rows of the selected body come first, so that the ring and the chips read them.
            var selectedBody = bodies.FirstOrDefault(body => body.Id == selection);
            PlaceRows(displaySize, selectedBody, selection is null ? null : values);
            // ADR-0081 (TASK-0253): the tool ring of the selected body, around the centre of its box on the screen.
            Ring(displaySize, bodies.FirstOrDefault(body => body.Id == selection), ringCentre, ringBody);
            // ADR-0082 (TASK-0254): the values of the selected extrude on its edges.
            Values(displaySize, selection is null ? null : values);
            // ADR-0084 (TASK-0267): the rows of the selected body, with no panel, over the chips.
            Rows(selectedBody);
            // The review of Codex of v0.252 (TASK-0271): the window of the live sketches is at the upper right too, so while
            // the rows show, they take its place (ADR-0084 item 6); the window of an active sketch shows as before.
            if (sketchPanel is { } shown && (shown.Active is not null || (shown.Live.Count > 0 && _rowsArea is null)))
                SketchWindow(shown, displaySize);
            if (imagePanel is not null)
                ImageWindow(imagePanel, displaySize);
        }

        if (_message is { } message)
            MessageWindow(displaySize, message);
        if (_pad is { } pad)
            PadWindow(pad, displaySize);
        ImGui.Render();

        _textActive = io.WantTextInput;
        return UiDrawDataConverter.Convert(ImGui.GetDrawData());
    }

    // ADR-0032 item 6: a text field of the panel is disabled while the pad is open, so it does not
    // take the keys of the pad. Until TASK-0099 a press on the file name field gave the typed name
    // to the pad, and Save as had an empty name (finding V33 of the second codebase review of
    // 2026-10-28).
    private void TextField(string label, ref string text, uint length)
    {
        ImGui.BeginDisabled(_pad is not null);
        ImGui.InputText(label, ref text, length);
        ImGui.EndDisabled();
    }

    private void Item(string label)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        _items[label] = new Vector4(min.X, min.Y, max.X, max.Y);
    }

    // The button of a mouse sample, or the one button of a finger or a pen.
    private static void Buttons(ImGuiIOPtr io, PointerSample sample)
    {
        if (sample.Phase == PointerPhase.Cancel)
        {
            for (var button = 0; button < 3; button++)
                io.AddMouseButtonEvent(button, false);
            return;
        }

        if (sample.Phase is not (PointerPhase.Down or PointerPhase.Up))
            return;

        var down = sample.Phase == PointerPhase.Down;
        if (sample.Kind != DeviceKind.Mouse)
        {
            io.AddMouseButtonEvent(0, down);
            return;
        }

        if ((sample.Buttons & PointerButtons.Left) != 0)
            io.AddMouseButtonEvent(0, down);
        if ((sample.Buttons & PointerButtons.Right) != 0)
            io.AddMouseButtonEvent(1, down);
        if ((sample.Buttons & PointerButtons.Middle) != 0)
            io.AddMouseButtonEvent(2, down);
    }

    private static ImGuiKey? Key(InputKey key) => key switch
    {
        InputKey.ArrowLeft => ImGuiKey.LeftArrow,
        InputKey.ArrowRight => ImGuiKey.RightArrow,
        InputKey.ArrowUp => ImGuiKey.UpArrow,
        InputKey.ArrowDown => ImGuiKey.DownArrow,
        InputKey.Backspace => ImGuiKey.Backspace,
        InputKey.Delete => ImGuiKey.Delete,
        InputKey.Home => ImGuiKey.Home,
        InputKey.End => ImGuiKey.End,
        InputKey.Enter => ImGuiKey.Enter,
        InputKey.Escape => ImGuiKey.Escape,
        InputKey.Tab => ImGuiKey.Tab,
        _ => null,
    };

    // ADR-0091: the bytes of a font of the assembly, in memory that the interface frees.
    private static (nint Pointer, int Length) FontData(string name)
    {
        using var stream = typeof(DesktopUi).Assembly.GetManifestResourceStream("ThreeDEngine.Desktop.Fonts." + name)
            ?? throw new InvalidOperationException($"The assembly holds no font {name}.");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        var pointer = System.Runtime.InteropServices.Marshal.AllocHGlobal(bytes.Length);
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, pointer, bytes.Length);
        return (pointer, bytes.Length);
    }

    // The two fonts of ADR-0091, and the font of a value.
    private readonly (nint Pointer, int Length)[] _fontData;
    private readonly ImFontPtr _monoFont;

    // ADR-0091: the count of the fonts of the atlas, and the widths of a text in the font of the interface and in the font
    // of a value, for a test.
    internal int FontCount => ImGui.GetIO().Fonts.Fonts.Size;

    internal (float Sans, float Mono) TextWidths(string text)
        => (ImGui.GetIO().Fonts.Fonts[0].CalcTextSizeA(FontSize, float.MaxValue, 0f, text).X, _monoFont.CalcTextSizeA(FontSize, float.MaxValue, 0f, text).X);

    public void Dispose()
    {
        ImGui.DestroyContext(_context);
        foreach (var (pointer, _) in _fontData)
            System.Runtime.InteropServices.Marshal.FreeHGlobal(pointer);
    }
}
