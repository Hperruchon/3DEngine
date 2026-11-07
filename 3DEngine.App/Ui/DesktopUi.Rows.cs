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

    // The colour of the word "failed" and its code: the red of the theme for a fault (the brief: red always comes with
    // the word "failed" and the code).
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
        var top = 16f + _inset.Y;
        var y = top + RowHeight;
        foreach (var value in values ?? [])
        {
            if (value.Body != body.Id)
                continue;
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
        if (_pad is null && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)
            && RowAt(io.MousePos) is { } released && RowAt(io.MouseClickedPos[0]) is { } pressed && pressed.Key == released.Key)
            Use(released);

        // Item 2: no ground and no border; a line under each row. The name at the left, and the kind and the state at the
        // right in the colour of disabled text, or "failed" and the code in red; a long name ends with "...".
        var list = ImGui.GetBackgroundDrawList();
        var colours = ImGui.GetStyle().Colors;
        var text = ImGui.GetColorU32(colours[(int)ImGuiCol.Text]);
        var quiet = ImGui.GetColorU32(colours[(int)ImGuiCol.TextDisabled]);
        var line = ImGui.GetColorU32(colours[(int)ImGuiCol.Separator]);
        var (left, top, right) = (area.X, area.Y, area.Z);
        var middle = (RowHeight - ImGui.GetTextLineHeight()) / 2f;
        var state = RowState(body);
        var stateWidth = ImGui.CalcTextSize(state).X;
        list.AddText(new Vector2(right - stateWidth, top + middle), body.Fault is null ? quiet : FailedColour, state);
        list.AddText(new Vector2(left, top + middle), text, Fit(body.Name ?? body.Kind, RowsWidth - stateWidth - 12f));
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
