using System.Numerics;
using ImGuiNET;
using ThreeDEngine.Core.Input;
using ThreeDEngine.Core.Models;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0138 (phase R21b, TASK-0370): the controls of the six flat views of the camera and of the return to the 3D view: a net of
// a cube, with Top over Left, Front, Right and Back, Bottom under Front, and "3D" at the right of Top. The button "Views" opens
// and closes it: on a landscape display after Redo on the row of the anchors, with the net above the anchors; on a portrait
// display at the upper right, where a portrait display has no command line, with the net under it and under the path line. A tap on a view closes
// the net. The cell of the view of the camera has the colour of a tool in use. The keys 1, 3 and 7 give Front, Right and
// Top, with Control Back, Left and Bottom, and 0 gives the 3D view.
internal sealed unsafe partial class DesktopUi
{
    public const string ViewsLabel = "Views";

    // The item of the cell of a view, or of "3D" with no view.
    public static string ViewItem(FlatView? view) => $"View:{view?.ToString() ?? "3D"}";

    // ADR-0166 item 3 (phase R30a, TASK-0406): the switch "Lines" in the cell at the lower right of the net, and its item.
    public const string LinesLabel = "Lines";
    public const string LinesItem = "View:Lines";
    internal const int LinesColumn = 3;
    internal const int LinesRow = 2;

    // ADR-0166 item 3: true while the switch "Lines" is on. It stays across the six flat views and the 3D view; the lines show
    // in a flat view only.
    private bool _lines;

    public bool LinesOn => _lines;

    // ADR-0166 item 2: the lines of the flat view in pixels of the window, and true while a read of new lines runs; null when the
    // window draws no lines in this frame (the switch is off, or the view has perspective). The host gives them before each
    // frame.
    private IReadOnlyList<ViewLines.Stroke>? _viewLines;
    private bool _linesPending;

    public void ShowLines(IReadOnlyList<ViewLines.Stroke>? strokes, bool pending = false)
        => (_viewLines, _linesPending) = (strokes, strokes is not null && pending);

    // The paper of the lines in this frame, in pixels (left, top, right, bottom), for a test.
    internal Vector4? PaperShown { get; private set; }

    // The text over the paper while a read of new lines runs.
    internal const string LinesReading = "Lines…";

    // The width of a cell: the widest word and the padding of a button, at most a quarter of the safe width, so the net fits a
    // phone; its height is that of a target of ADR-0027 item 6.
    private float ViewCellWidth(Vector2 displaySize)
        => Math.Min(ViewNet.Select(cell => cell.Label).Append(LinesLabel).Max(label => ImGui.CalcTextSize(label).X) + (2f * ImGui.GetStyle().FramePadding.X) + 8f,
            (displaySize.X - 32f - _inset.X - _inset.Z - (3f * ViewGap)) / 4f);

    private const float ViewGap = 4f;

    // The cells of the net: the column, the row, the view and its word.
    internal static readonly IReadOnlyList<(int Column, int Row, FlatView? View, string Label)> ViewNet =
    [
        (1, 0, FlatView.Top, "Top"),
        (3, 0, null, "3D"),
        (0, 1, FlatView.Left, "Left"),
        (1, 1, FlatView.Front, "Front"),
        (2, 1, FlatView.Right, "Right"),
        (3, 1, FlatView.Back, "Back"),
        (1, 2, FlatView.Bottom, "Bottom"),
    ];

    // The view of the camera of this frame, from the host.
    private FlatView? _view;

    // True while the net of a portrait display is open.
    private bool _viewsOpen;

    // The count of frames in which the keys of the views rest after Control and K (see Frame).
    private int _quietViewKeys;

    // The host gives the view of the camera before each frame.
    public void ShowView(FlatView? view) => _view = view;

    internal bool ViewsOpen => _viewsOpen;

    // Opens the net, as a tap on "Views" does, for a picture of a test.
    internal void OpenViews() => _viewsOpen = true;

    // The width of the button "Views": its word and the padding of a button.
    private static float ViewsButtonWidth => ImGui.CalcTextSize(ViewsLabel).X + (2f * ImGui.GetStyle().FramePadding.X);

    // The button "Views", as high as an anchor, with its outline 1 pixel inside, where the clip of the window does not cut it.
    private void ViewsButton()
    {
        if (ImGui.Button(ViewsLabel, new Vector2(0f, AnchorSize)))
            _viewsOpen = !_viewsOpen;
        Item(ViewsLabel);
        if (_items.TryGetValue(ViewsLabel, out var button))
            ImGui.GetWindowDrawList().AddRect(new Vector2(button.X + 1f, button.Y + 1f), new Vector2(button.Z - 1f, button.W - 1f), ImGui.GetColorU32(_viewsOpen ? ImGuiCol.ButtonActive : ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, Theme.OutlineWidth);
    }

    // On a portrait display the button "Views" at the upper right, in a window of its own.
    private void PortraitViewsButton(Vector2 displaySize)
    {
        ImGui.SetNextWindowPos(new Vector2(displaySize.X - 16f - _inset.Z, 16f + _inset.Y), ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##ViewsButton", FixedFlags))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
            ViewsButton();
            ImGui.PopStyleVar();
            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    private void Views(Vector2 displaySize)
    {
        var portrait = displaySize.X < displaySize.Y;
        if (portrait)
            PortraitViewsButton(displaySize);
        if (!_viewsOpen || !_items.TryGetValue(ViewsLabel, out var opener))
            return;
        var cell = new Vector2(ViewCellWidth(displaySize), MinimumControlHeight);
        var size = new Vector2((4f * cell.X) + (3f * ViewGap), (3f * cell.Y) + (2f * ViewGap));
        var at = portrait
            ? new Vector2(displaySize.X - 16f - _inset.Z - size.X, Math.Max(opener.W, _items.TryGetValue(PathItem, out var path) ? path.W : 0f) + 8f)
            : new Vector2(16f + _inset.X, opener.Y - 8f - size.Y);
        ImGui.SetNextWindowPos(at, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("##Views", FixedFlags))
        {
            // The outline of each cell is drawn 1 pixel inside it, where the clip of the window does not cut it.
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
            var accent = ImGui.ColorConvertU32ToFloat4(Theme.Accent);
            foreach (var (column, row, view, label) in ViewNet)
            {
                ImGui.SetCursorPos(new Vector2(column * (cell.X + ViewGap), row * (cell.Y + ViewGap)));
                var current = view == _view;
                if (current)
                    ImGui.PushStyleColor(ImGuiCol.Button, accent with { W = 0.3f });
                if (ImGui.Button($"{label}##View", cell))
                {
                    _actions.Add(new FlatViewAction(view));
                    _viewsOpen = false;
                }

                if (current)
                    ImGui.PopStyleColor();
                Item(ViewItem(view));
                if (_items.TryGetValue(ViewItem(view), out var box))
                    ImGui.GetWindowDrawList().AddRect(new Vector2(box.X + 1f, box.Y + 1f), new Vector2(box.Z - 1f, box.W - 1f), ImGui.GetColorU32(current ? ImGuiCol.ButtonActive : ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, Theme.OutlineWidth);
            }

            // ADR-0166 item 3: the switch "Lines" has the colour of a tool in use while it is on. A tap changes it and closes the
            // net, as a tap on a view does.
            ImGui.SetCursorPos(new Vector2(LinesColumn * (cell.X + ViewGap), LinesRow * (cell.Y + ViewGap)));
            var on = _lines;
            if (on)
                ImGui.PushStyleColor(ImGuiCol.Button, accent with { W = 0.3f });
            if (ImGui.Button($"{LinesLabel}##View", cell))
            {
                _lines = !_lines;
                _viewsOpen = false;
            }

            if (on)
                ImGui.PopStyleColor();
            Item(LinesItem);
            if (_items.TryGetValue(LinesItem, out var switchBox))
                ImGui.GetWindowDrawList().AddRect(new Vector2(switchBox.X + 1f, switchBox.Y + 1f), new Vector2(switchBox.Z - 1f, switchBox.W - 1f), ImGui.GetColorU32(on ? ImGuiCol.ButtonActive : ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, Theme.OutlineWidth);

            ImGui.PopStyleVar();

            AddWindowRectangle();
        }

        ImGui.End();
        ImGui.PopStyleVar();
    }

    // ADR-0140 item 2 (phase R21c, TASK-0372): the display of the last frame, for the free part of the scene.
    private Vector2 _frameDisplay;

    // ADR-0140 item 2: the free part of the scene in the windows of the last frame, in normalized coordinates (left, bottom,
    // right, top): inside the safe area less 16 pixels; right of the panel or the rows at the left of a landscape display, 16
    // pixels from them; under the windows of the upper band (the top bar, the path line and the command line) and above the
    // windows of the lower band (the anchors), 8 pixels from them; on a portrait display above the rows under the part. A
    // part of less than 100 pixels on a side gives the whole picture.
    internal (double X0, double Y0, double X1, double Y1) FreePart()
    {
        var (width, height) = (_frameDisplay.X, _frameDisplay.Y);
        if (FreePixels(_panels.Concat(_fixed)) is not { } free)
            return (-1.0, -1.0, 1.0, 1.0);
        return ((2.0 * free.X / width) - 1.0, 1.0 - (2.0 * free.W / height), (2.0 * free.Z / width) - 1.0, 1.0 - (2.0 * free.Y / height));
    }

    // ADR-0140 item 3: the free part in pixels (left, top, right, bottom) among the windows, or null for the whole picture.
    // ADR-0166 item 3 (TASK-0406): the paper of the lines takes it from the windows of the last frame.
    private Vector4? FreePixels(IEnumerable<Vector4> windows)
    {
        var (width, height) = (_frameDisplay.X, _frameDisplay.Y);
        if (!(width > 0 && height > 0))
            return null;
        var portrait = width < height;
        var (left, top, right, bottom) = (16f + _inset.X, 16f + _inset.Y, width - 16f - _inset.Z, height - 16f - _inset.W);
        foreach (var box in windows.Concat(_rowsArea is { } rows ? [rows] : Array.Empty<Vector4>()))
        {
            if (!portrait && box.X < width * 0.25f && box.Z < width * 0.6f && box.Y < height * 0.6f && box.W > height * 0.2f)
                left = Math.Max(left, box.Z + 16f);

            // Finding V191 of the codebase review of 2026-11-14 (e) (TASK-0379): a tall window at the right edge, as the
            // window of the live sketches, cuts the part as the panel at the left does. Until TASK-0379 Frame put the part
            // under it.
            if (!portrait && box.Z > width * 0.75f && box.X > width * 0.4f && box.Y < height * 0.6f && box.W > height * 0.2f)
                right = Math.Min(right, box.X - 16f);
            if (box.W < height * 0.2f)
                top = Math.Max(top, box.W + 8f);
            if (box.Y > height * 0.8f)
                bottom = Math.Min(bottom, box.Y - 8f);
        }

        if (portrait && _rowsUnder && _rowsArea is { } under)
            bottom = Math.Min(bottom, under.Y - 8f);
        if (right - left < 100f || bottom - top < 100f)
            return null;
        return new Vector4(left, top, right, bottom);
    }

    // ADR-0166 items 2 and 3: the paper and the lines of a flat view with the switch "Lines" on, first on the background list,
    // under each other mark of the scene: a light paper on the free part of the scene of the last frame, the visible lines of 2
    // pixels and the hidden lines of 1 pixel with dashes, in ink, cut at the paper, and "Lines…" while a read runs. The paper
    // covers the shaded bodies, their edges and the images, so the view has no fill.
    private void Paper()
    {
        PaperShown = null;
        if (_viewLines is not { } strokes || _home is not null)
            return;
        var display = _frameDisplay;
        var free = FreePixels(_lastPanels.Concat(_lastFixed)) ?? new Vector4(0f, 0f, display.X, display.Y);
        PaperShown = free;
        var list = ImGui.GetBackgroundDrawList();
        var (low, high) = (new Vector2(free.X, free.Y), new Vector2(free.Z, free.W));
        list.AddRectFilled(low, high, Theme.Paper, PaperRounding);
        list.PushClipRect(low, high, true);
        foreach (var stroke in strokes)
        {
            if (stroke.Visible)
            {
                var points = stroke.Points.ToArray();
                list.AddPolyline(ref points[0], points.Length, Theme.Ink, ImDrawFlags.None, VisibleLineWidth);
            }
            else
            {
                foreach (var (a, b) in ViewLines.Dashes(stroke.Points, HiddenDashOn, HiddenDashOff))
                    list.AddLine(a, b, Theme.Ink, HiddenLineWidth);
            }
        }

        if (_linesPending)
            list.AddText(low + new Vector2(12f, 8f), Theme.Ink, LinesReading);
        list.PopClipRect();
    }

    // ADR-0166 item 3: the widths in pixels of a visible and of a hidden line, the dash of a hidden line, and the corner of the
    // paper.
    internal const float VisibleLineWidth = 2f;
    internal const float HiddenLineWidth = 1f;
    internal const float HiddenDashOn = 6f;
    internal const float HiddenDashOff = 4f;
    private const float PaperRounding = 6f;

    // The keys of the views: 1, 3 and 7 give Front, Right and Top, and with Control Back, Left and Bottom; 0 gives the 3D view.
    private void WatchViewKeys(IReadOnlyList<InputSample> samples)
    {
        foreach (var sample in samples)
        {
            if (sample is not KeySample { Down: true } key)
                continue;
            var control = (key.Modifiers & KeyModifiers.Control) != 0;
            // ADR-0166 item 3 (TASK-0406): L, with no Control, changes the switch "Lines", under the guard of the keys of the views.
            if (key.Key == InputKey.L && !control)
            {
                _lines = !_lines;
                continue;
            }

            (bool Known, FlatView? View) chosen = key.Key switch
            {
                InputKey.Digit1 => (true, control ? FlatView.Back : FlatView.Front),
                InputKey.Digit3 => (true, control ? FlatView.Left : FlatView.Right),
                InputKey.Digit7 => (true, control ? FlatView.Bottom : FlatView.Top),
                InputKey.Digit0 => (true, null),
                _ => (false, null),
            };
            if (chosen.Known)
                _actions.Add(new FlatViewAction(chosen.View));
        }
    }
}
