using System.Numerics;
using ImGuiNET;

namespace ThreeDEngine.Desktop.Ui;

// ADR-0084 (the redesign, slice d, TASK-0267): the rows of the selected body, with no panel. The "mix" direction of the
// designer brief puts the data of the selected part in rows with no ground and no border: its name and its state, then
// each value that a command changes, with its label and its exact value. A tap on a value row opens the pad of that value,
// as its chip on the edge does (ADR-0082).
internal sealed unsafe partial class DesktopUi
{
    // The width of the column of the rows, and the height of one row: the 44 pixels of a target of ADR-0027 item 6.
    public const float RowsWidth = 360f;
    public const float RowHeight = ValueSize;

    // The item of the first row: the name and the state of the body.
    public const string RowsTitleItem = "Rows:title";

    // The colour of the word "failed" and its code (the brief: red always comes with the word "failed" and the code). The
    // theme of ADR-0078 has no colour of a fault, so this file holds it. Finding V133 (TASK-0271): until TASK-0271 this
    // comment said that it was a colour of the theme.
    internal const uint FailedColour = 0xFF5C5CE8;

    public static string RowItem(string key) => $"Row:{key}";

    // The value rows of this frame, and the rectangle of each row with the first one.
    private List<(EdgeValues.Value Value, Vector4 Rectangle)> _rows = [];
    private Vector4? _rowsArea;

    // True when the point is on the rows of this frame.
    private bool OverRows(double x, double y)
        => _rowsArea is { } area && x >= area.X && x < area.Z && y >= area.Y && y < area.W;

    private EdgeValues.Value? RowAt(Vector2 point)
        => _rows.FirstOrDefault(row => point.X >= row.Rectangle.X && point.X < row.Rectangle.Z && point.Y >= row.Rectangle.Y && point.Y < row.Rectangle.W).Value;

    // The state of the body after its kind: "correct", or "failed" and the code of its fault; and "hidden".
    internal static string RowState(LiveBody body)
        => $"{body.Kind} · {(body.Fault is { } fault ? $"failed {fault.Code}" : "correct")}{(body.Visible ? string.Empty : " · hidden")}";

    // Item 1: on a landscape display, while a body is selected and no sketch is active, the rows are at the upper right,
    // 16 pixels inside the safe area. A portrait display keeps the data in the panel in this slice. The frame places the
    // rows before the ring and the chips, so that both read the rows of this frame.
    private void PlaceRows(Vector2 displaySize, LiveBody? body, IReadOnlyList<EdgeValues.Value>? values)
    {
        (_rows, _rowsArea) = ([], null);
        if (body is null || _sketchActive || displaySize.X < displaySize.Y)
            return;

        var right = displaySize.X - 16f - _inset.Z;
        var left = right - RowsWidth;
        // ADR-0098 item 4 (the redesign, slice o, TASK-0312): the rows start 8 pixels under the command line.
        var top = Math.Max(16f + _inset.Y, _commandBottom > 0f ? _commandBottom + 8f : 0f);
        var y = top + RowHeight;
        // The review of Codex of v0.252 (TASK-0271): the rows end 16 pixels above the anchors, as the panel does; a value
        // that has no room keeps its chip only. Until TASK-0271 a long list went past the bottom of the display.
        var bottom = displaySize.Y - 32f - _inset.W - AnchorSize;
        foreach (var value in values ?? [])
        {
            if (value.Body != body.Id)
                continue;
            if (y + RowHeight > bottom)
                break;
            _rows.Add((value, new Vector4(left, y, right, y + RowHeight)));
            y += RowHeight;
        }

        _rowsArea = new Vector4(left, top, right, y);
    }

    // Items 2 and 3: the rows of this frame, and the tap on a value row.
    private void Rows(LiveBody? body)
    {
        if (body is null || _rowsArea is not { } area)
            return;

        // Item 3: a press and a release on one value row, with no window of the panels under the pointer and no pad open,
        // open its pad.
        var io = ImGui.GetIO();
        if (_pad is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !OverWindowAtRelease()
            && RowAt(io.MousePos) is { } released && RowAt(io.MouseClickedPos[0]) is { } pressed && pressed.Key == released.Key)
            Use(released);

        // Item 2: no ground and no border; a line under each row. Finding V129 of the codebase review of 2026-11-07 (b)
        // (TASK-0271): the name is on the first line of the first row, and the kind and the state on its second line, each
        // in the width of the column; only "failed" and the code are red. Until TASK-0271 the state of a hidden body pushed
        // the name out to "..." and crossed the left edge of the column.
        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        var text = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        var line = ImGui.GetColorU32(colours[(int)ImGuiCol.Separator]);
        var (left, top, right) = (area.X, area.Y, area.Z);
        var middle = (RowHeight - ImGui.GetTextLineHeight()) / 2f;
        var (name, parts) = RowTitle(body);
        var lineHeight = ImGui.GetTextLineHeight();
        var first = top + ((RowHeight - (2f * lineHeight)) / 2f);
        list.AddText(new Vector2(left, first), text, name);
        var x = left;
        foreach (var (part, red) in parts)
        {
            list.AddText(new Vector2(x, first + lineHeight), red ? FailedColour : quiet, part);
            x += ImGui.CalcTextSize(part).X;
        }
        list.AddLine(new Vector2(left, top + RowHeight), new Vector2(right, top + RowHeight), line, 1f);
        _items[RowsTitleItem] = new Vector4(left, top, right, top + RowHeight);

        foreach (var (value, rectangle) in _rows)
        {
            var (min, max) = (new Vector2(rectangle.X, rectangle.Y), new Vector2(rectangle.Z, rectangle.W));
            var pending = value.Key == _pendingValue;
            var hovered = !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) && RowAt(io.MousePos)?.Key == value.Key;
            if (hovered)
                list.AddRectFilled(min, max, ImGui.GetColorU32(colours[(int)ImGuiCol.ButtonHovered]), 6f);
            if (pending)
                Dashed(list, min, max, DimensionColour);
            list.AddText(new Vector2(min.X, min.Y + middle), quiet, value.Label);
            var width = ImGui.CalcTextSize(value.Text).X;
            list.AddText(new Vector2(max.X - width, min.Y + middle), pending ? DimensionColour : text, value.Text);
            list.AddLine(new Vector2(min.X, max.Y), max, line, 1f);
            _items[RowItem(value.Key)] = rectangle;
        }
    }

    // V129 (TASK-0271): the name of the first row, and the parts of its second line: the kind, "failed" and the code (red),
    // or "correct", and "hidden"; each line fits the width of the column. A state that is too wide loses the kind first,
    // then "hidden", so that "failed" and the code stay whole; the panel gives the kind and the visibility too.
    internal (string Name, IReadOnlyList<(string Text, bool Red)> State) RowTitle(LiveBody body)
    {
        var name = Fit(body.Name ?? body.Kind, RowsWidth);
        var state = body.Fault is { } fault ? ($"failed {fault.Code}", true) : ("correct", false);
        List<(string Text, bool Red)>[] forms =
        [
            [($"{body.Kind} · ", false), state, .. body.Visible ? Array.Empty<(string, bool)>() : [(" · hidden", false)]],
            [state, .. body.Visible ? Array.Empty<(string, bool)>() : [(" · hidden", false)]],
            [state],
        ];
        foreach (var form in forms)
        {
            if (ImGui.CalcTextSize(string.Concat(form.Select(part => part.Text))).X <= RowsWidth)
                return (name, form);
        }

        return (name, [(Fit(state.Item1, RowsWidth), state.Item2)]);
    }

    // The text, or its start and "...", in the width.
    internal static string Fit(string text, float width)
    {
        if (ImGui.CalcTextSize(text).X <= width)
            return text;
        for (var length = text.Length - 1; length > 0; length--)
        {
            var cut = text[..length] + "...";
            if (ImGui.CalcTextSize(cut).X <= width)
                return cut;
        }

        return "...";
    }

    // Item 4: a chip that the placement put on the rows goes under them, at the same x, so that a value shows once in
    // each place and a tap finds one of them.
    private Vector4 OutOfRows(Vector4 chip)
    {
        if (_rowsArea is not { } area || chip.Z <= area.X || chip.X >= area.Z || chip.W <= area.Y || chip.Y >= area.W)
            return chip;
        var down = area.W + 8f - chip.Y;
        return chip with { Y = chip.Y + down, W = chip.W + down };
    }
}
