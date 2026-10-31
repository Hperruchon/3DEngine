using System.Numerics;
using System.Runtime.InteropServices;
using System.Globalization;
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
internal sealed unsafe class DesktopUi : IDisposable
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
    public const string UndoLabel = "Undo";
    public const string RedoLabel = "Redo";
    public const string NewBoxLabel = "New box";
    public const string MoveLabel = "Move";
    public const string FaultLabel = "Fault";

    // ADR-0034 items 6 and 8: the buttons of a new sketch in the panel, and the window of the
    // sketch with its controls. A row of the window has the label of its item (SketchText).
    public const string SketchLineLabel = "Sketch on";
    public static readonly (string Label, Guid Datum)[] NewSketchButtons =
        [("Sketch XY", Datums.XY.Id), ("Sketch XZ", Datums.XZ.Id), ("Sketch YZ", Datums.YZ.Id)];

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
    private readonly Dictionary<string, Vector4> _items = new(StringComparer.Ordinal);
    private readonly List<string> _shown = new();
    private readonly (uint Width, uint Height, byte[] Rgba) _atlas;
    private string _filter = string.Empty;
    private readonly List<PanelAction> _actions = new();

    // The text of the name field, and the body that it was filled for.
    private string _name = string.Empty;

    private string _fileName = string.Empty;
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

        ImFontConfigPtr font = ImGuiNative.ImFontConfig_ImFontConfig();
        font.SizePixels = FontSize;
        io.Fonts.AddFontDefault(font);
        font.Destroy();

        var style = ImGui.GetStyle();
        style.FramePadding = new Vector2(12f, FramePaddingY);
        style.ItemSpacing = new Vector2(12f, 10f);
        style.WindowPadding = new Vector2(14f, 14f);
        style.ScrollbarSize = 24f;
        style.GrabMinSize = 24f;
        style.WindowRounding = 6f;
        style.FrameRounding = 4f;

        // The atlas is built here: NewFrame refuses a font atlas that is not built, and the
        // native library stops the process on a failed check (TASK-0084).
        _atlas = BuildAtlas(io);
    }

    // The filter of the panel, as the person typed it.
    public string Filter => _filter;

    // The open pad, or null.
    public NumericPad? Pad => _pad;

    // The key of the rectangle of a control of the pad, for ItemRectangle.
    public static string PadItem(string label) => $"{PadName} {label}";

    // The host gives the result of the command of an apply: null closes the pad, and a
    // rejection stays in the pad (ADR-0032 item 8).
    public void EndPad(string? rejection)
    {
        _padApplied = false;
        if (rejection is null)
            ClosePad();
        else
            _pad?.Fail(rejection);
    }

    // ADR-0034 item 7: the pad of the next point of a chain, an offset from the last point while a
    // chain is open, else a position. The host opens it again after each apply, so a person types
    // each point of a chain in one pad.
    public void OpenNextPointPad(bool chainOpen)
    {
        var (a, b) = chainOpen ? ("dx", "dy") : ("x", "y");
        // The key comes from the state that names the fields, not from the last frame: the host
        // opens the pad again in the frame of an apply, before the panel sees the new chain.
        OpenPad(new NumericPad(NextPointLabel, _unit, new(a, "0"), new(b, "0")), values => new NextPointAction(values[0], values[1]), bySketch: true,
            scope: PadScope.Chain, key: ChainKey(_activeKey, SketchToolKind.Line, chainOpen));
    }

    private void OpenPad(NumericPad pad, Func<double[], PanelAction> apply, bool bySketch = false, PadScope scope = PadScope.None, string? key = null)
    {
        (_pad, _padApply, _padBySketch, _padApplied) = (pad, apply, bySketch, false);
        (_padScope, _padKey) = (scope, key ?? Key(scope));
    }

    private string? Key(PadScope scope) => scope switch
    {
        PadScope.Body => _bodyKey,
        PadScope.Sketch => _sketchKey,
        PadScope.Chain => _chainKey,
        PadScope.Active => _activeKey,
        _ => null,
    };

    private static string ChainKey(string active, SketchToolKind tool, bool chainOpen) => $"{active}:{tool}:{chainOpen}";

    private void ClosePad() => (_pad, _padApply, _padScope, _padKey, _padApplied) = (null, null, PadScope.None, null, false);

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

    // True when a point of the display is over a panel of the last frame.
    public bool OverPanel(double x, double y)
        => _panels.Any(panel => x >= panel.X && x < panel.Z && y >= panel.Y && y < panel.W);

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
        return actions;
    }

    // The text of a row: the name of the body, or its kind and a short identifier, the word
    // "hidden" for a hidden body (ADR-0029 item 7), and the word "fault" for a body whose last
    // rebuild failed (ADR-0037 item 6, K0c).
    public static string RowText(LiveBody body)
        => (body.Name ?? $"{body.Kind}  {body.Id.ToString()[..8]}") + (body.Visible ? string.Empty : "  (hidden)")
        + (body.Fault is null ? string.Empty : "  (fault)");

    // ADR-0033 item 13: the colour of the lines and the points of a sketch, as ImGui packs
    // it (alpha, blue, green, red), and their sizes in pixels of the window.
    internal const uint SketchColour = 0xFF30C8F0;
    private const float SketchLineWidth = 2f;
    private const float SketchPointRadius = 4f;

    // ADR-0034 item 9: a selected item in white and wider, and each sketch that is not active in grey.
    internal const uint SelectedColour = 0xFFFFFFFF;

    // The mark of a snap: a square of 12 pixels in cyan (ABGR).
    internal const uint SnapColour = 0xFFFFE000;
    private const float SnapHalfSize = 6f;
    internal const uint OtherSketchColour = 0xFF909090;
    private const float SelectedLineWidth = 4f;
    private const float SelectedPointRadius = 7f;
    private const float RingRadius = 12f;

    public UiDrawData Frame(float deltaSeconds, Vector2 displaySize, Vector2 framebufferScale, IReadOnlyList<LiveBody> bodies, Guid? selection = null, DocumentLine? document = null, SketchOverlay.Strokes? sketches = null, SketchPanel? sketchPanel = null)
    {
        ArgumentNullException.ThrowIfNull(bodies);
        ImGui.SetCurrentContext(_context);
        var io = ImGui.GetIO();
        io.DisplaySize = displaySize;
        io.DisplayFramebufferScale = framebufferScale;
        io.DeltaTime = deltaSeconds > 0 ? deltaSeconds : 1f / 60f;

        _panels.Clear();
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
        Panel(bodies, selection, displaySize, document);
        if (sketchPanel is { } shown && (shown.Active is not null || shown.Live.Count > 0))
            SketchWindow(shown, displaySize);
        if (_pad is { } pad)
            PadWindow(pad, displaySize);
        ImGui.Render();

        _textActive = io.WantTextInput;
        return UiDrawDataConverter.Convert(ImGui.GetDrawData());
    }

    // ADR-0033 item 13: the sketches, read only, on the background list of ImGui, so they
    // come over the scene and under each window of the panel.
    private static void Sketches(SketchOverlay.Strokes sketches)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var line in sketches.Lines)
            list.AddLine(line.A, line.B, Colour(line.Mark), line.Mark == SketchOverlay.Mark.Selected ? SelectedLineWidth : SketchLineWidth);
        foreach (var point in sketches.Points)
            list.AddCircleFilled(point.At, point.Mark == SketchOverlay.Mark.Selected ? SelectedPointRadius : SketchPointRadius, Colour(point.Mark));
        foreach (var ring in sketches.Rings)
            list.AddCircle(ring, RingRadius, SketchColour, 0, SketchLineWidth);
        // ADR-0041 item 6 (phase R11b): a square at the point that a tap under the mouse would take.
        if (sketches.Snap is { } snap)
            list.AddRect(snap - new Vector2(SnapHalfSize), snap + new Vector2(SnapHalfSize), SnapColour, 0, ImDrawFlags.None, SketchLineWidth);
        foreach (var label in sketches.Labels)
            list.AddText(label.At, SketchColour, label.Text);
    }

    private static uint Colour(SketchOverlay.Mark mark) => mark switch
    {
        SketchOverlay.Mark.Selected => SelectedColour,
        SketchOverlay.Mark.Other => OtherSketchColour,
        _ => SketchColour,
    };

    private void Panel(IReadOnlyList<LiveBody> bodies, Guid? selection, Vector2 displaySize, DocumentLine? document)
    {
        ImGui.SetNextWindowPos(new Vector2(16f, 16f), ImGuiCond.FirstUseEver);
        // The first size fits inside a small display too. Since the line of Undo and Redo (TASK-0093)
        // and the line of a new sketch (TASK-0101) the panel is 700 pixels high at most, so that two
        // bodies and Show all fit with no scroll bar.
        ImGui.SetNextWindowSize(new Vector2(460f, Math.Clamp(displaySize.Y - 32f, 200f, 700f)), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(PanelName))
        {
            ImGui.TextUnformatted(_address);
            if (document is not null)
                File(document);

            // ADR-0031 item 11: the history of the document. A rejection shows in the line of the file.
            if (ImGui.Button(UndoLabel))
                _actions.Add(new UndoAction());
            Item(UndoLabel);
            ImGui.SameLine();
            if (ImGui.Button(RedoLabel))
                _actions.Add(new RedoAction());
            Item(RedoLabel);
            ImGui.SameLine();
            if (ImGui.Button(NewBoxLabel))
                OpenPad(new NumericPad(NewBoxLabel, _unit, new("x", "10"), new("y", "10"), new("z", "10")), values => new CreateBoxAction(values[0], values[1], values[2]));
            Item(NewBoxLabel);

            // ADR-0034 item 6: a new sketch on each origin plane.
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(SketchLineLabel);
            foreach (var (label, datum) in NewSketchButtons)
            {
                ImGui.SameLine();
                if (ImGui.Button($"{label[^2..]}##{label}"))
                    _actions.Add(new NewSketchAction(datum));
                Item(label);
            }

            ImGui.TextUnformatted(bodies.Count == 1 ? "1 live body" : $"{bodies.Count} live bodies");

            ImGui.SetNextItemWidth(220f);
            TextField(FilterLabel, ref _filter, 64);
            Item(FilterLabel);
            ImGui.SameLine();
            if (ImGui.Button(ClearLabel))
                _filter = string.Empty;
            Item(ClearLabel);

            ImGui.Separator();
            foreach (var body in bodies)
            {
                var row = RowText(body);
                if (_filter.Length > 0 && !row.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                // The identifier after ## keeps two rows with one text apart.
                if (ImGui.Selectable($"{row}##{body.Id}", selection == body.Id, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    _actions.Add(new SelectBodyAction(body.Id));
                Item(row);
                _shown.Add(row);
            }

            ImGui.Separator();
            if (bodies.FirstOrDefault(body => body.Id == selection) is { } selected)
                Selected(selected);

            if (ImGui.Button(ShowAllLabel))
                _actions.Add(new ShowAllAction());
            Item(ShowAllLabel);
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    // The controls of the selected body. The name field starts with the name of the body each
    // time another body is selected.
    private void Selected(LiveBody body)
    {
        if (_nameFor != body.Id)
            (_name, _nameFor) = (body.Name ?? string.Empty, body.Id);

        // ADR-0037 item 6 (Q2 A): the body keeps its last solid, and the panel gives the reason.
        // Finding V50 of the codebase review of 2026-10-30 (TASK-0118): the full reason with the
        // identifiers took 208 pixels and pushed Show all out of the panel. One line gives the code,
        // and a hover or a press on it gives the reason.
        if (body.Fault is { } fault)
        {
            ImGui.TextUnformatted($"{FaultLabel}: {fault.Code}");
            Item(FaultLabel);
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
                ImGui.SetTooltip(fault.Message);
        }

        ImGui.SetNextItemWidth(220f);
        TextField(NameLabel, ref _name, 128);
        Item(NameLabel);
        ImGui.SameLine();
        if (ImGui.Button(RenameLabel))
            _actions.Add(new RenameBodyAction(body.Id, _name));
        Item(RenameLabel);

        var visibility = body.Visible ? HideLabel : ShowLabel;
        if (ImGui.Button(visibility))
            _actions.Add(new SetVisibleAction(body.Id, !body.Visible));
        Item(visibility);
        ImGui.SameLine();

        // ADR-0032 item 4: a move of the selected body by the offsets of the pad.
        if (ImGui.Button(MoveLabel))
            OpenPad(new NumericPad(MoveLabel, _unit, new("dx", "0"), new("dy", "0"), new("dz", "0")), values => new MoveBodyAction(body.Id, values[0], values[1], values[2]), scope: PadScope.Body);
        Item(MoveLabel);
        ImGui.SameLine();
    }

    // ADR-0034 item 8: the window of the sketch. With no active sketch it lists each live sketch
    // with Edit. With an active sketch it gives the tools, the buttons of the selection and a row
    // for each point, line and constraint; a row selects as a tap of the tool Select does.
    private void SketchWindow(SketchPanel panel, Vector2 displaySize)
    {
        ImGui.SetNextWindowPos(new Vector2(Math.Max(16f, displaySize.X - SketchWidth - 16f), 16f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(SketchWidth, Math.Clamp(displaySize.Y - 32f, 200f, 700f)), ImGuiCond.FirstUseEver);
        if (ImGui.Begin(SketchName))
        {
            if (panel.Active is { } sketch)
                ActiveSketch(panel, sketch);
            else
                LiveSketches(panel.Live);
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    private void LiveSketches(IReadOnlyCollection<SketchInfo> live)
    {
        ImGui.TextUnformatted(live.Count == 1 ? "1 live sketch" : $"{live.Count} live sketches");
        foreach (var sketch in live)
        {
            var label = $"{EditLabel} {SketchTitle(sketch)}";
            if (ImGui.Button($"{label}##{sketch.SketchId}"))
                _actions.Add(new EditSketchAction(sketch.SketchId));
            Item(SketchItem(label));
        }
    }

    // The name of a sketch, or its short identifier, and its datum.
    public static string SketchTitle(SketchInfo sketch)
        => $"{sketch.Name ?? sketch.SketchId.ToString()[..8]} on {Datums.Find(sketch.DatumId)?.Name ?? "?"}";

    // ADR-0035 item 8: if the solver satisfies each constraint, and the number of free degrees.
    public static string SketchStatus(SketchInfo sketch)
        => sketch.Solved ? $"Solved, {sketch.Freedom} free" : "Not solved";

    private void ActiveSketch(SketchPanel panel, SketchInfo sketch)
    {
        ImGui.TextUnformatted(SketchTitle(sketch));
        ImGui.TextUnformatted(SketchStatus(sketch));
        ToolButton(LineToolLabel, SketchToolKind.Line, panel.Tool);
        ImGui.SameLine();
        ToolButton(SelectToolLabel, SketchToolKind.Select, panel.Tool);
        ImGui.SameLine();
        ToolButton(CircleToolLabel, SketchToolKind.Circle, panel.Tool);
        ImGui.SameLine();
        ToolButton(ArcToolLabel, SketchToolKind.Arc, panel.Tool);

        // The row of Next point stays the second row, so the pad below it stays inside a display of
        // 1024 x 768 (finding V42); Finish and Extrude follow it (TASK-0122).
        if (panel.Tool == SketchToolKind.Line)
        {
            if (ImGui.Button(NextPointLabel))
                OpenNextPointPad(panel.ChainOpen);
            Item(SketchItem(NextPointLabel));
            if (panel.ChainOpen)
            {
                ImGui.SameLine();
                SketchButton(EndLabel, new EndChainAction());
                ImGui.SameLine();
                SketchButton(CloseLabel, new CloseChainAction());
            }
        }

        SketchButton(FinishLabel, new FinishSketchAction());
        ImGui.SameLine();
        // ADR-0036 item 9 (Q4 A): the pad of one signed distance along the normal of the datum.
        if (ImGui.Button(ExtrudeLabel))
            OpenPad(new NumericPad(ExtrudeLabel, _unit, new PadField("distance", "10")), values => new ExtrudeAction(values[0]), bySketch: true, scope: PadScope.Active);
        Item(SketchItem(ExtrudeLabel));

        SelectionButtons(sketch, panel.Selection);
        if (panel.Message is { } message)
            ImGui.TextWrapped(message);

        ImGui.Separator();
        foreach (var (id, text) in SketchText.Rows(sketch))
        {
            if (ImGui.Selectable($"{text}##{id}", panel.Selection.Contains(id), ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                _actions.Add(new ToggleSketchItemAction(id));
            Item(SketchItem(text.Split(' ')[0]));
        }
    }

    // The active tool has the colour of a pressed button.
    private void ToolButton(string label, SketchToolKind kind, SketchToolKind active)
    {
        var pushed = kind == active;
        if (pushed)
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
        SketchButton(label, new SketchToolAction(kind));
        if (pushed)
            ImGui.PopStyleColor();
    }

    private void SketchButton(string label, PanelAction action)
    {
        if (ImGui.Button(label))
            _actions.Add(action);
        Item(SketchItem(label));
    }

    // ADR-0034 item 8: the buttons of the kinds that fit the selection, Move point, Value and
    // Delete. A kind with a value opens the pad with the current value. The items of a constraint
    // go in the order of its kind: the points first.
    private void SelectionButtons(SketchInfo sketch, IReadOnlyList<Guid> selection)
    {
        if (selection.Count == 0)
            return;

        var points = sketch.Points.ToDictionary(point => point.Id);
        var lines = sketch.Lines.Select(line => line.Id).ToHashSet();
        var curves = sketch.Circles.Select(circle => circle.Id).Concat(sketch.Arcs.Select(arc => arc.Id)).ToHashSet();
        var constraint = selection.Count == 1 ? sketch.Constraints.FirstOrDefault(item => item.Id == selection[0]) : null;
        // The points first, then the lines, then the curves (A), as the kinds take their items.
        int Order(Guid item) => points.ContainsKey(item) ? 0 : lines.Contains(item) ? 1 : curves.Contains(item) ? 2 : 3;
        Guid[] items = [.. selection.OrderBy(Order)];
        var shape = string.Concat(items.Select(item => Order(item) switch { 0 => "P", 1 => "L", 2 => "A", _ => "C" }));

        // Finding V43 of the codebase review of 2026-10-29: the origin O of the frame has the
        // identifier of its sketch, and the engine refuses to move it or to delete it (ADR-0033
        // item 2). A selection with O gives no Fixed, Move point or Delete.
        var origin = selection.Contains(sketch.SketchId);

        // The right edge of the content, left of the scroll bar.
        var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
        var first = true;
        void Flow(string label, Action press)
        {
            // A button goes on the line of the last one when it fits in the window.
            var width = ImGui.CalcTextSize(label).X + 2 * ImGui.GetStyle().FramePadding.X;
            if (!first && ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemSpacing.X + width <= right)
                ImGui.SameLine();
            first = false;
            if (ImGui.Button(label))
                press();
            Item(SketchItem(label));
        }

        foreach (var (label, kind) in ConstraintButtons)
        {
            var fits = (kind, shape) switch
            {
                ("coincident", "PP") => true,
                ("horizontal" or "vertical", "L") => true,
                ("distance", "PP" or "PL" or "L") => true,
                ("fixed", "P") => !origin,
                // Finding V59 of the codebase review of 2026-10-31 (TASK-0133): the engine refuses a point
                // that defines the curve and a line with an end at the centre (SketchConstraints.Refusal).
                ("pointOn", "PA") => label == OnCurveLabel && !CurvePoints(sketch, items[1]).Contains(items[0]),
                ("tangent", "LA") => label != InsideTangentLabel && !IsEnd(sketch, CurvePoints(sketch, items[1])[0], items[0]),
                ("tangent", "AA") => true,
                ("equal", "AA" or "LL") => true,
                ("radius", "A") => true,
                // The kinds of K2c on lines: a point of pointOn and of midpoint is not an end of its
                // line, as the engine requires (SketchConstraints.Refusal). Symmetric takes two points
                // that are not ends of the line, since the selection holds three items (K2g, TASK-0127).
                ("pointOn", "PL") => label == OnLineLabel && !IsEnd(sketch, items[0], items[1]),
                ("midpoint", "PL") => !IsEnd(sketch, items[0], items[1]),
                ("parallel" or "perpendicular" or "angle", "LL") => true,
                ("symmetric", "PPL") => !IsEnd(sketch, items[0], items[2]) && !IsEnd(sketch, items[1], items[2]),
                _ => false,
            };
            if (!fits)
                continue;

            Flow(label, () =>
            {
                switch (kind)
                {
                    case "distance":
                        var length = SketchText.Distance(sketch, items) ?? 0;
                        OpenPad(new NumericPad(label, _unit, new PadField("length", SketchText.Number(length))), values => new AddConstraintAction(kind, items, values), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "fixed":
                        var at = points[items[0]];
                        OpenPad(new NumericPad(label, _unit, new("x", SketchText.Number(at.X)), new("y", SketchText.Number(at.Y))), values => new AddConstraintAction(kind, items, values), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "radius":
                        var radius = SketchText.Radius(sketch, items[0]) ?? 0;
                        OpenPad(new NumericPad(label, _unit, new PadField("radius", SketchText.Number(radius))), values => new AddConstraintAction(kind, items, values), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "angle":
                        var degrees = SketchText.Angle(sketch, items[0], items[1]) ?? 0;
                        OpenPad(new NumericPad(label, AngleUnit, new PadField("angle", SketchText.Number(degrees))), values => new AddConstraintAction(kind, items, CosineAndSine(values[0])), bySketch: true, scope: PadScope.Sketch);
                        break;
                    case "tangent" when shape == "AA":
                        _actions.Add(new AddConstraintAction(kind, items, [label == InsideTangentLabel ? -1 : 1]));
                        break;
                    default:
                        _actions.Add(new AddConstraintAction(kind, items, []));
                        break;
                }
            });
        }

        if (shape == "P" && !origin)
        {
            var point = points[items[0]];
            Flow(MovePointLabel, () => OpenPad(
                new NumericPad(MovePointLabel, _unit, new("x", SketchText.Number(point.X)), new("y", SketchText.Number(point.Y))),
                values => new MoveSketchPointAction(point.Id, values[0], values[1]),
                bySketch: true,
                scope: PadScope.Sketch));
        }

        // Finding V56 of the codebase review of 2026-10-31 (TASK-0133): the Value pad of an angle showed
        // its cosine and its sine in millimetres. It takes degrees, as the pad of Angle does. A tangent of
        // two curves has the value 1 or −1, which Tangent and Inside tangent give, so it has no Value.
        if (constraint is { Kind: "angle", Values.Count: 2 })
        {
            var degrees = Math.Atan2(constraint.Values[1], constraint.Values[0]) * 180 / Math.PI;
            Flow(ValueLabel, () => OpenPad(
                new NumericPad(ValueLabel, AngleUnit, new PadField("angle", SketchText.Number(degrees))),
                values => new SetConstraintValueAction(constraint.Id, CosineAndSine(values[0])),
                bySketch: true,
                scope: PadScope.Sketch));
        }
        else if (constraint is { Values.Count: > 0 } && constraint.Kind != "tangent")
        {
            string[] names = constraint.Values.Count == 1 ? ["length"] : ["x", "y"];
            Flow(ValueLabel, () => OpenPad(
                new NumericPad(ValueLabel, _unit, [.. constraint.Values.Select((value, index) => new PadField(names[index], SketchText.Number(value)))]),
                values => new SetConstraintValueAction(constraint.Id, values),
                bySketch: true,
                scope: PadScope.Sketch));
        }

        if (!origin)
            Flow(DeleteLabel, () => _actions.Add(new DeleteSketchItemsAction([.. selection])));
    }

    // The centre of a curve first, then the start and the end of an arc.
    private static Guid[] CurvePoints(SketchInfo sketch, Guid curve)
        => sketch.Circles.FirstOrDefault(circle => circle.Id == curve) is { } circle
            ? [circle.Center]
            : sketch.Arcs.FirstOrDefault(arc => arc.Id == curve) is { } arc ? [arc.Center, arc.Start, arc.End] : [Guid.Empty];

    private static bool IsEnd(SketchInfo sketch, Guid point, Guid line)
        => sketch.Lines.FirstOrDefault(item => item.Id == line) is { } found && (found.Start == point || found.End == point);

    // The cosine and the sine of an angle in degrees. A multiple of 90 degrees gives exact numbers, so
    // an angle of 90 records (0, 1) and not (6.1e-17, 1).
    internal static double[] CosineAndSine(double degrees)
    {
        var turn = degrees % 360;
        if (turn < 0)
            turn += 360;
        return turn switch
        {
            0 => [1, 0],
            90 => [0, 1],
            180 => [-1, 0],
            270 => [0, -1],
            // Finding V60 (TASK-0133): the turn, not the degrees, so 1e20 gives the angle of its turn; a
            // value that is not finite gives NaN, which the engine refuses.
            _ => [Math.Cos(turn * Math.PI / 180), Math.Sin(turn * Math.PI / 180)],
        };
    }

    // ADR-0032 item 5: the window of the pad. A row selects its field; the line under the fields
    // gives the value of the active field, or its error, and the message of the last apply.
    private void PadWindow(NumericPad pad, Vector2 displaySize)
    {
        // A pad of the sketch opens to the left of the window of the sketch, so that End and Close
        // stay visible (ADR-0034 item 8). The place and the size come from the display, as for the
        // panel, so the pad stays inside a tablet display. Until TASK-0099 Apply was outside the
        // display at 768 x 1024 and at 1024 x 768 (finding V35 of the second codebase review of
        // 2026-10-28).
        var width = Math.Min(PadWidth, displaySize.X - 32f);
        var preferred = _padBySketch ? displaySize.X - SketchWidth - 32f - width : 492f;
        var x = Math.Clamp(preferred, 16f, Math.Max(16f, displaySize.X - width - 16f));

        // Finding V42 of the codebase review of 2026-10-29: on a display with no room to the left
        // of the window of the sketch, as 768 x 1024, the pad covered End and Close. It now opens
        // below the row of Next point, End and Close.
        var y = 16f;
        if (_padBySketch && preferred < 16f && ItemRectangle(SketchItem(NextPointLabel)) is { } nextPoint)
            y = nextPoint.W + 8f;
        ImGui.SetNextWindowPos(new Vector2(x, y), ImGuiCond.Appearing);
        ImGui.SetNextWindowSize(new Vector2(width, Math.Clamp(displaySize.Y - y - 16f, 200f, PadHeight)), ImGuiCond.Appearing);
        if (ImGui.Begin(PadName))
        {
            ImGui.TextUnformatted(pad.Title);
            var fields = pad.Fields;
            for (var index = 0; index < fields.Count; index++)
            {
                if (ImGui.Selectable($"{fields[index].Name} = {fields[index].Text}  {pad.Unit}##field{index}", pad.Active == index, ImGuiSelectableFlags.None, new Vector2(0f, MinimumControlHeight)))
                    pad.Select(index);
                Item(PadItem(fields[index].Name));
            }

            var preview = pad.Preview;
            ImGui.TextUnformatted(preview.IsValue ? $"= {preview.Value.ToString("G15", CultureInfo.InvariantCulture)} {pad.Unit}" : preview.Error!);
            if (pad.Message is { } message)
                ImGui.TextUnformatted(message);
            ImGui.Separator();

            foreach (var row in PadRows)
            {
                for (var column = 0; column < row.Length; column++)
                {
                    if (column > 0)
                        ImGui.SameLine();
                    if (ImGui.Button(row[column], new Vector2(108f, 0f)))
                        Press(pad, row[column]);
                    Item(PadItem(row[column]));
                }
            }
        }

        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        _panels.Add(new Vector4(position.X, position.Y, position.X + size.X, position.Y + size.Y));
        ImGui.End();
    }

    // A button of the pad: an edit, an apply, a cancel, or the text of its label.
    private void Press(NumericPad pad, string label)
    {
        switch (label)
        {
            case "Delete": pad.Delete(); break;
            case "Clear": pad.Clear(); break;
            case "Next": pad.Next(); break;
            case "Apply": ApplyPad(pad); break;
            case "Cancel": ClosePad(); break;
            default: pad.Append(label); break;
        }
    }

    // ADR-0032 item 6: a key or a text of the frame while the pad is open.
    private void PadKey(NumericPad pad, InputSample sample)
    {
        switch (sample)
        {
            case TextSample text: pad.Append(text.Text); break;
            case KeySample { Down: true, Key: InputKey.Backspace }: Press(pad, "Delete"); break;
            case KeySample { Down: true, Key: InputKey.Tab }: Press(pad, "Next"); break;
            case KeySample { Down: true, Key: InputKey.Enter }: Press(pad, "Apply"); break;
            case KeySample { Down: true, Key: InputKey.Escape }: Press(pad, "Cancel"); break;
        }
    }

    // The numbers of an apply go to the host, and the pad stays open until EndPad. A second
    // apply before EndPad does nothing (V34): two presses of Enter in one frame gave two boxes.
    private void ApplyPad(NumericPad pad)
    {
        if (_padApplied)
            return;
        if (pad.Apply() is { } values && _padApply is { } apply)
        {
            _padAction = apply(values);
            _actions.Add(_padAction);
            _padApplied = true;
        }
    }

    // ADR-0030 item 3: the name of the file, a name field, Save as and Open.
    private void File(DocumentLine document)
    {
        ImGui.TextUnformatted(document.FileName is { } name ? $"File: {name}" : "File: unsaved");
        ImGui.SetNextItemWidth(200f);
        TextField(FileNameLabel, ref _fileName, 128);
        Item(FileNameLabel);
        // The buttons have their own line, so that they stay inside the panel.
        if (ImGui.Button(SaveAsLabel))
            _actions.Add(new SaveDocumentAction(_fileName));
        Item(SaveAsLabel);
        ImGui.SameLine();
        if (ImGui.Button(OpenLabel))
            _actions.Add(new OpenDocumentAction(_fileName));
        Item(OpenLabel);
        if (document.Message is { } message)
            ImGui.TextUnformatted(message);
        ImGui.Separator();
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

    public void Dispose() => ImGui.DestroyContext(_context);
}
