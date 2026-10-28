using System.Numerics;
using System.Runtime.InteropServices;
using System.Globalization;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Numeric;
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

    // ADR-0032 item 5: the window of the pad and its buttons, row by row.
    public const string PadName = "Pad";
    public static readonly string[][] PadRows =
    [
        ["7", "8", "9", "(", ")"],
        ["4", "5", "6", "*", "pi"],
        ["1", "2", "3", "-", "sqrt("],
        ["0", ".", "+", "/", "Delete"],
        ["Clear", "Next", "Apply", "Cancel"],
    ];

    // ADR-0015 item 7: the millimetre is the one length unit of a document.
    private const string Unit = "mm";

    private readonly IntPtr _context;
    private readonly string _address;
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

    // ADR-0032 items 3 and 4: the open pad, and the body of a move, or null for a new box.
    private NumericPad? _pad;
    private Guid? _padBody;

    public DesktopUi(string address)
    {
        if (!UiDrawDataConverter.VertexLayoutAgrees)
            throw new InvalidOperationException("The vertex of Dear ImGui does not have the layout of UiVertex.");

        _address = address;
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
        if (rejection is null)
            (_pad, _padBody) = (null, null);
        else
            _pad?.Fail(rejection);
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

    // The text of a row: the name of the body, or its kind and a short identifier, and the word
    // "hidden" for a hidden body (ADR-0029 item 7).
    public static string RowText(LiveBody body)
        => (body.Name ?? $"{body.Kind}  {body.Id.ToString()[..8]}") + (body.Visible ? string.Empty : "  (hidden)");

    // ADR-0033 item 13: the colour of the lines and the points of a sketch, as ImGui packs
    // it (alpha, blue, green, red), and their sizes in pixels of the window.
    internal const uint SketchColour = 0xFF30C8F0;
    private const float SketchLineWidth = 2f;
    private const float SketchPointRadius = 4f;

    public UiDrawData Frame(float deltaSeconds, Vector2 displaySize, Vector2 framebufferScale, IReadOnlyList<LiveBody> bodies, Guid? selection = null, DocumentLine? document = null, SketchOverlay.Strokes? sketches = null)
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

        ImGui.NewFrame();
        if (_dropFocus)
        {
            ImGui.SetWindowFocus(null);
            _dropFocus = false;
        }

        if (sketches is not null)
            Sketches(sketches);
        Panel(bodies, selection, displaySize, document);
        if (_pad is { } pad)
            PadWindow(pad);
        ImGui.Render();

        _textActive = io.WantTextInput;
        return UiDrawDataConverter.Convert(ImGui.GetDrawData());
    }

    // ADR-0033 item 13: the sketches, read only, on the background list of ImGui, so they
    // come over the scene and under each window of the panel.
    private static void Sketches(SketchOverlay.Strokes sketches)
    {
        var list = ImGui.GetBackgroundDrawList();
        foreach (var (a, b) in sketches.Lines)
            list.AddLine(a, b, SketchColour, SketchLineWidth);
        foreach (var point in sketches.Points)
            list.AddCircleFilled(point, SketchPointRadius, SketchColour);
    }

    private void Panel(IReadOnlyList<LiveBody> bodies, Guid? selection, Vector2 displaySize, DocumentLine? document)
    {
        ImGui.SetNextWindowPos(new Vector2(16f, 16f), ImGuiCond.FirstUseEver);
        // The first size fits inside a small display too. Since the line of Undo and Redo (TASK-0093)
        // the panel is 640 pixels high at most, so that two bodies and Show all fit with no scroll bar.
        ImGui.SetNextWindowSize(new Vector2(460f, Math.Clamp(displaySize.Y - 32f, 200f, 640f)), ImGuiCond.FirstUseEver);
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
                (_pad, _padBody) = (new NumericPad(NewBoxLabel, Unit, new("x", "10"), new("y", "10"), new("z", "10")), null);
            Item(NewBoxLabel);

            ImGui.TextUnformatted(bodies.Count == 1 ? "1 live body" : $"{bodies.Count} live bodies");

            ImGui.SetNextItemWidth(220f);
            ImGui.InputText(FilterLabel, ref _filter, 64);
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

        ImGui.SetNextItemWidth(220f);
        ImGui.InputText(NameLabel, ref _name, 128);
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
            (_pad, _padBody) = (new NumericPad(MoveLabel, Unit, new("dx", "0"), new("dy", "0"), new("dz", "0")), body.Id);
        Item(MoveLabel);
        ImGui.SameLine();
    }

    // ADR-0032 item 5: the window of the pad. A row selects its field; the line under the fields
    // gives the value of the active field, or its error, and the message of the last apply.
    private void PadWindow(NumericPad pad)
    {
        ImGui.SetNextWindowPos(new Vector2(492f, 16f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(620f, 600f), ImGuiCond.FirstUseEver);
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
            case "Cancel": (_pad, _padBody) = (null, null); break;
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

    // The numbers of an apply go to the host, and the pad stays open until EndPad.
    private void ApplyPad(NumericPad pad)
    {
        if (pad.Apply() is not { } values)
            return;
        _actions.Add(_padBody is { } body
            ? new MoveBodyAction(body, values[0], values[1], values[2])
            : new CreateBoxAction(values[0], values[1], values[2]));
    }

    // ADR-0030 item 3: the name of the file, a name field, Save as and Open.
    private void File(DocumentLine document)
    {
        ImGui.TextUnformatted(document.FileName is { } name ? $"File: {name}" : "File: unsaved");
        ImGui.SetNextItemWidth(200f);
        ImGui.InputText(FileNameLabel, ref _fileName, 128);
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
